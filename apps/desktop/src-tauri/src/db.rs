//! Lokalna SQLite baza za povijest screenshotova, tagove i pretragu.
//! Shema prati `docs/SPECIFICATION.md` §6.1.

use std::path::Path;

use rusqlite::{params, Connection};

use crate::models::Screenshot;

pub struct Db {
    conn: Connection,
}

impl Db {
    /// Otvori (ili kreiraj) bazu na zadanoj putanji i pokreni migracije.
    pub fn open(path: &Path) -> anyhow::Result<Self> {
        let conn = Connection::open(path)?;
        conn.pragma_update(None, "journal_mode", "WAL")?;
        conn.pragma_update(None, "foreign_keys", "ON")?;
        let db = Self { conn };
        db.migrate()?;
        Ok(db)
    }

    /// Baza samo u memoriji — zadnja linija obrane kad se datoteka ne da otvoriti.
    /// Povijest se tada ne pamti između pokretanja, ali capture i snimanje rade.
    pub fn open_in_memory() -> anyhow::Result<Self> {
        let conn = Connection::open_in_memory()?;
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
               ORDER BY created_at DESC
               LIMIT ?1 OFFSET ?2"#,
        )?;
        let rows = stmt.query_map(params![limit, offset], Self::map_row)?;
        let mut out = Vec::new();
        for r in rows {
            let mut s = r?;
            s.tags = self.tags_for(&s.id)?;
            out.push(s);
        }
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

    /// Pretraga po imenu ili tagu (LIKE; FTS5 dolazi u F2-14).
    pub fn search(&self, query: &str) -> anyhow::Result<Vec<Screenshot>> {
        let like = format!("%{}%", query);
        let mut stmt = self.conn.prepare(
            r#"SELECT DISTINCT s.id, s.file_path, s.thumb_path, s.name, s.format, s.width, s.height,
                      s.size_bytes, s.source, s.created_at, s.is_uploaded
               FROM screenshots s
               LEFT JOIN screenshot_tags st ON st.screenshot_id = s.id
               LEFT JOIN tags t ON t.id = st.tag_id
               WHERE s.name LIKE ?1 OR t.name LIKE ?1
               ORDER BY s.created_at DESC
               LIMIT 500"#,
        )?;
        let rows = stmt.query_map(params![like], Self::map_row)?;
        let mut out = Vec::new();
        for r in rows {
            let mut s = r?;
            s.tags = self.tags_for(&s.id)?;
            out.push(s);
        }
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
        self.conn
            .execute("INSERT OR IGNORE INTO tags(name) VALUES (?1)", params![tag])?;
        self.conn.execute(
            r#"INSERT OR IGNORE INTO screenshot_tags(screenshot_id, tag_id)
               VALUES (?1, (SELECT id FROM tags WHERE name = ?2))"#,
            params![screenshot_id, tag],
        )?;
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
        Ok(rows.filter_map(Result::ok).collect())
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
