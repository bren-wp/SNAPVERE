//! Lokalna SQLite baza za povijest screenshotova, tagove i pretragu.
//! Shema prati `docs/SPECIFICATION.md` §6.1.

use std::collections::HashMap;
use std::path::Path;
use std::time::Duration;

use rusqlite::{params, Connection};

use crate::models::Screenshot;

pub struct Db {
    conn: Connection,
}

impl Db {
    /// Otvori (ili kreiraj) bazu na zadanoj putanji i pokreni migracije.
    pub fn open(path: &Path) -> anyhow::Result<Self> {
        let conn = Connection::open(path)?;
        conn.busy_timeout(Duration::from_secs(5))?;
        conn.pragma_update(None, "journal_mode", "WAL")?;
        conn.pragma_update(None, "synchronous", "NORMAL")?;
        conn.pragma_update(None, "foreign_keys", "ON")?;
        let db = Self { conn };
        db.migrate()?;
        Ok(db)
    }

    /// Baza samo u memoriji — zadnja linija obrane kad se datoteka ne da otvoriti.
    /// Povijest se tada ne pamti između pokretanja, ali capture i snimanje rade.
    pub fn open_in_memory() -> anyhow::Result<Self> {
        let conn = Connection::open_in_memory()?;
        conn.pragma_update(None, "foreign_keys", "ON")?;
        let db = Self { conn };
        db.migrate()?;
        Ok(db)
    }

    fn migrate(&self) -> anyhow::Result<()> {
        self.conn.execute_batch(
            r#"
            CREATE TABLE IF NOT EXISTS screenshots (
                id            TEXT PRIMARY KEY,
                file_path     TEXT NOT NULL,
                thumb_path    TEXT,
                name          TEXT NOT NULL,
                format        TEXT NOT NULL,
                width         INTEGER NOT NULL,
                height        INTEGER NOT NULL,
                size_bytes    INTEGER NOT NULL,
                source        TEXT NOT NULL,
                created_at    INTEGER NOT NULL,
                remote_id     TEXT,
                remote_slug   TEXT,
                remote_url    TEXT,
                remote_expires_at INTEGER,
                is_uploaded   INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS idx_screenshots_created ON screenshots(created_at DESC);
            CREATE INDEX IF NOT EXISTS idx_screenshots_name ON screenshots(name);

            CREATE TABLE IF NOT EXISTS tags (
                id   INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE
            );
            CREATE TABLE IF NOT EXISTS screenshot_tags (
                screenshot_id TEXT NOT NULL REFERENCES screenshots(id) ON DELETE CASCADE,
                tag_id        INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
                PRIMARY KEY (screenshot_id, tag_id)
            );
            "#,
        )?;
        Ok(())
    }

    /// Ubaci novi zapis screenshota.
    pub fn insert(&self, s: &Screenshot) -> anyhow::Result<()> {
        self.conn.execute(
            r#"INSERT INTO screenshots
               (id, file_path, thumb_path, name, format, width, height, size_bytes, source, created_at, is_uploaded)
               VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11)"#,
            params![
                s.id,
                s.file_path,
                s.thumb_path,
                s.name,
                s.format,
                s.width,
                s.height,
                s.size_bytes as i64,
                s.source,
                s.created_at,
                s.is_uploaded as i32,
            ],
        )?;
        Ok(())
    }

    /// Dohvati zadnjih `limit` screenshotova (najnoviji prvi).
    pub fn list(&self, limit: u32, offset: u32) -> anyhow::Result<Vec<Screenshot>> {
        let mut stmt = self.conn.prepare(
            r#"SELECT id, file_path, thumb_path, name, format, width, height, size_bytes,
                      source, created_at, is_uploaded
               FROM screenshots
               ORDER BY created_at DESC, id DESC
               LIMIT ?1 OFFSET ?2"#,
        )?;
        let rows = stmt.query_map(params![limit.min(500), offset], Self::map_row)?;
        let mut out = Vec::new();
        for r in rows {
            out.push(r?);
        }
        self.attach_tags(&mut out)?;
        Ok(out)
    }

    /// Jedan zapis po id-u (preglednik u galeriji).
    pub fn get(&self, id: &str) -> anyhow::Result<Option<Screenshot>> {
        let mut stmt = self.conn.prepare(
            r#"SELECT id, file_path, thumb_path, name, format, width, height, size_bytes,
                      source, created_at, is_uploaded
               FROM screenshots
               WHERE id = ?1"#,
        )?;
        let mut rows = stmt.query_map(params![id], Self::map_row)?;
        match rows.next() {
            Some(r) => {
                let mut s = r?;
                s.tags = self.tags_for(&s.id)?;
                Ok(Some(s))
            }
            None => Ok(None),
        }
    }

    /// Bounded substring search without multiplying results for matching tags.
    pub fn search(&self, query: &str, limit: u32, offset: u32) -> anyhow::Result<Vec<Screenshot>> {
        anyhow::ensure!(query.chars().count() <= 128, "Search query exceeds 128 characters");
        // A user's % and _ are literal search terms, not SQLite wildcards.
        let escaped = query
            .replace('\\', "\\\\")
            .replace('%', "\\%")
            .replace('_', "\\_");
        let like = format!("%{escaped}%");
        let mut stmt = self.conn.prepare(
            r#"SELECT s.id, s.file_path, s.thumb_path, s.name, s.format, s.width, s.height,
                      s.size_bytes, s.source, s.created_at, s.is_uploaded
               FROM screenshots s
               WHERE s.name LIKE ?1 ESCAPE '\'
                  OR EXISTS (
                      SELECT 1 FROM screenshot_tags st
                      JOIN tags t ON t.id = st.tag_id
                      WHERE st.screenshot_id = s.id AND t.name LIKE ?1 ESCAPE '\'
                  )
               ORDER BY s.created_at DESC, s.id DESC
               LIMIT ?2 OFFSET ?3"#,
        )?;
        let rows = stmt.query_map(params![like, limit.min(500), offset], Self::map_row)?;
        let mut out = Vec::new();
        for r in rows {
            out.push(r?);
        }
        self.attach_tags(&mut out)?;
        Ok(out)
    }

    /// Obriši zapis (datoteku briše pozivatelj).
    pub fn delete(&self, id: &str) -> anyhow::Result<()> {
        self.conn
            .execute("DELETE FROM screenshots WHERE id = ?1", params![id])?;
        Ok(())
    }

    /// Dodaj tag screenshotu (kreira tag ako ne postoji).
    pub fn add_tag(&self, screenshot_id: &str, tag: &str) -> anyhow::Result<()> {
        let tag = tag.trim();
        anyhow::ensure!(!tag.is_empty(), "Tag cannot be empty");
        anyhow::ensure!(tag.chars().count() <= 64, "Tag exceeds 64 characters");
        anyhow::ensure!(!tag.chars().any(char::is_control), "Tag contains control characters");
        let exists: bool = self.conn.query_row(
            "SELECT EXISTS(SELECT 1 FROM screenshots WHERE id = ?1)",
            params![screenshot_id],
            |row| row.get(0),
        )?;
        anyhow::ensure!(exists, "Screenshot does not exist");
        self.conn
            .execute("INSERT OR IGNORE INTO tags(name) VALUES (?1)", params![tag])?;
        self.conn.execute(
            r#"INSERT OR IGNORE INTO screenshot_tags(screenshot_id, tag_id)
               VALUES (?1, (SELECT id FROM tags WHERE name = ?2))"#,
            params![screenshot_id, tag],
        )?;
        Ok(())
    }

    /// Load tags for one page using one SQL query (avoids N+1 tag lookups).
    fn attach_tags(&self, items: &mut [Screenshot]) -> anyhow::Result<()> {
        if items.is_empty() {
            return Ok(());
        }
        let slots = vec!["?"; items.len()].join(", ");
        let sql = format!(
            "SELECT st.screenshot_id, t.name FROM screenshot_tags st
             JOIN tags t ON t.id = st.tag_id
             WHERE st.screenshot_id IN ({slots}) ORDER BY t.name"
        );
        let mut stmt = self.conn.prepare(&sql)?;
        let pairs = stmt.query_map(
            rusqlite::params_from_iter(items.iter().map(|s| s.id.as_str())),
            |row| Ok((row.get::<_, String>(0)?, row.get::<_, String>(1)?)),
        )?;
        let mut tags: HashMap<String, Vec<String>> = HashMap::new();
        for pair in pairs {
            let (id, name) = pair?;
            tags.entry(id).or_default().push(name);
        }
        for item in items {
            item.tags = tags.remove(&item.id).unwrap_or_default();
        }
        Ok(())
    }

    fn tags_for(&self, screenshot_id: &str) -> anyhow::Result<Vec<String>> {
        let mut stmt = self.conn.prepare(
            r#"SELECT t.name FROM tags t
               JOIN screenshot_tags st ON st.tag_id = t.id
               WHERE st.screenshot_id = ?1
               ORDER BY t.name"#,
        )?;
        let rows = stmt.query_map(params![screenshot_id], |row| row.get::<_, String>(0))?;
        Ok(rows.collect::<rusqlite::Result<Vec<_>>>()?)
    }

    fn map_row(row: &rusqlite::Row) -> rusqlite::Result<Screenshot> {
        Ok(Screenshot {
            id: row.get(0)?,
            file_path: row.get(1)?,
            thumb_path: row.get(2)?,
            name: row.get(3)?,
            format: row.get(4)?,
            width: row.get(5)?,
            height: row.get(6)?,
            size_bytes: row.get::<_, i64>(7)? as u64,
            source: row.get(8)?,
            created_at: row.get(9)?,
            is_uploaded: row.get::<_, i32>(10)? != 0,
            tags: Vec::new(),
        })
    }
}

#[cfg(test)]
mod gallery_tests {
    use super::*;

    fn shot(id: &str, time: i64) -> Screenshot {
        Screenshot {
            id: id.into(),
            file_path: format!("/tmp/{id}.png"),
            thumb_path: None,
            name: format!("Snapvere {id}"),
            format: "png".into(),
            width: 4,
            height: 4,
            size_bytes: 64,
            source: "region".into(),
            created_at: time,
            is_uploaded: false,
            tags: Vec::new(),
        }
    }
    #[test]
    fn list_search_pagination_and_tag_loading() -> anyhow::Result<()> {
        let db = Db::open_in_memory()?;
        db.insert(&shot("one", 100))?;
        db.insert(&shot("two", 200))?;
        db.add_tag("one", "design")?;
        db.add_tag("one", "team")?;
        db.add_tag("two", "team")?;
        let all = db.list(200, 0)?;
        assert_eq!(all.len(), 2);
        assert_eq!(all[0].id, "two");
        assert_eq!(all[0].tags, vec!["team"]);
        assert_eq!(all[1].tags, vec!["design", "team"]);
        assert_eq!(db.search("design", 200, 0)?[0].tags, vec!["design", "team"]);
        assert_eq!(db.list(1, 1)?[0].id, "one");
        assert!(db.list(1, 10)?.is_empty());
        assert!(db.list(0, 0)?.is_empty());
        Ok(())
    }

    #[test]
    fn search_treats_wildcards_as_literal_characters() -> anyhow::Result<()> {
        let db = Db::open_in_memory()?;
        db.insert(&shot("one%two", 100))?;
        db.insert(&shot("one_two", 200))?;
        db.insert(&shot("oneXtwo", 300))?;
        assert_eq!(db.search("%", 200, 0)?[0].id, "one%two");
        assert_eq!(db.search("%", 200, 0)?.len(), 1);
        assert_eq!(db.search("_", 200, 0)?[0].id, "one_two");
        assert_eq!(db.search("_", 200, 0)?.len(), 1);
        assert_eq!(db.search("one", 200, 0)?.len(), 3);
        Ok(())
    }

    #[test]
    fn stable_pagination_across_matching_timestamps() -> anyhow::Result<()> {
        let db = Db::open_in_memory()?;
        db.insert(&shot("same_a", 100))?;
        db.insert(&shot("same_b", 100))?;
        db.insert(&shot("older", 90))?;
        assert_eq!(db.list(1, 0)?[0].id, "same_b");
        assert_eq!(db.list(1, 1)?[0].id, "same_a");
        assert_eq!(db.list(1, 2)?[0].id, "older");
        assert_eq!(db.search("same", 1, 0)?[0].id, "same_b");
        assert_eq!(db.search("same", 1, 1)?[0].id, "same_a");
        assert!(db.search("same", 1, 2)?.is_empty());
        Ok(())
    }

    #[test]
    fn reject_invalid_tags_and_oversized_search() -> anyhow::Result<()> {
        let db = Db::open_in_memory()?;
        db.insert(&shot("one", 100))?;
        assert!(db.add_tag("one", "").is_err());
        assert!(db.add_tag("one", "a\nb").is_err());
        assert!(db.add_tag("one", &"x".repeat(65)).is_err());
        assert!(db.add_tag("missing", "valid").is_err());
        db.add_tag("one", "  useful  ")?;
        assert_eq!(db.get("one")?.expect("record").tags, vec!["useful"]);
        assert!(db.search(&"x".repeat(129), 10, 0).is_err());
        Ok(())
    }

    #[test]
    fn search_does_not_duplicate_matches_across_tags() -> anyhow::Result<()> {
        let db = Db::open_in_memory()?;
        db.insert(&shot("one", 100))?;
        db.add_tag("one", "meeting")?;
        db.add_tag("one", "meet-up")?;
        assert_eq!(db.search("meet", 10, 0)?.len(), 1);
        Ok(())
    }

    #[test]
    fn in_memory_delete_cascades_tag_relations() -> anyhow::Result<()> {
        let db = Db::open_in_memory()?;
        db.insert(&shot("one", 100))?;
        db.add_tag("one", "important")?;
        db.delete("one")?;
        let count: i64 = db
            .conn
            .query_row("SELECT COUNT(*) FROM screenshot_tags", [], |r| r.get(0))?;
        assert_eq!(count, 0);
        Ok(())
    }
}
