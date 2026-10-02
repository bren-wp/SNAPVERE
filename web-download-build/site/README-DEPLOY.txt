SNAPVERE protected local downloads
==================================

Deployment target
-----------------
Recommended document root: https://download.snapvere.com/

This package is intentionally self-contained. It serves all six downloadable
packages locally. Production requests do not redirect to GitHub and direct
HTTP access to storage/ is denied.

Included packages
-----------------
- SNAPVERE-Setup.exe
- SNAPVERE-Portable.exe
- SNAPVERE-Chrome.zip
- SNAPVERE-Edge.zip
- SNAPVERE-Firefox.zip
- SNAPVERE-Opera.zip

Download flow
-------------
1. Visitor chooses a package.
2. Visitor must enter a valid permanent email address.
3. Disposable-email domains are rejected.
4. The server stores the lead and issues a signed 15-minute access token.
5. The first real authorized GET for that package is recorded once.
6. The aggregate counter is incremented once.
7. An administrative notification is sent to info@brendigo.com.
8. The local file is streamed with HTTP Range support.

HEAD, prefetch/prerender traffic and repeated Range requests do not create
duplicate notification messages or duplicate counter increments.

Notification data
-----------------
The admin email contains the entered email address, selected package, file,
release version, UTC time, language, anonymized IP hash, user-agent, referer,
origin, lead ID and event ID.

The application intentionally does not store a raw IP address in its JSONL
lead/event records.

Mail transport
--------------
The default mail transport is PHP mail(). Configure the hosting account so
outbound mail from no-reply@snapvere.com is permitted. The From domain remains
SNAPVERE-controlled and the user's address is used only as Reply-To.

If PHP reports that mail submission failed, the event is still recorded and a
protected fallback record is appended to storage/data/mail-spool.jsonl. Review
that file if server mail delivery is not configured.

Persistent runtime data
-----------------------
Preserve storage/data/ during upgrades if the site is already live.

Runtime-created files include:
- .secrets.json
- download-leads.jsonl
- download-events.jsonl
- download-mail-status.jsonl
- download-count.json
- rate-limit.json
- mail-spool.jsonl (only when mail transport reports failure)

Do not expose or publish storage/data/. Direct HTTP access is blocked by
.htaccess, but server configuration must also honor Apache access rules.

Permissions
-----------
PHP must be able to write storage/data/. Package files should be readable by
PHP but not directly web-accessible.

Recommended:
- directories: 0750 or hosting-provider equivalent
- package files: 0640 or equivalent
- storage/data writable only by the site account/PHP process

Origin/CORS
-----------
POST authorization is accepted only from the configured SNAPVERE origins:
- https://snapvere.com
- https://www.snapvere.com
- https://download.snapvere.com

If the canonical domains change, update storage/config.php.

Validation
----------
Run from the deployment document root:
    php tools/self-test.php

The self-test verifies package presence, exact byte size, SHA-256, runtime
secret creation, storage writability, email denylist behavior and package
contract.

Privacy/retention
-----------------
Operational download records contain the user-entered email address because it
is required for the requested download and administrative notification.
The intended retention period is 90 days. Apply the same retention/backup
policy used by the existing SNAPVERE deployment.

Do not use these addresses for marketing unless separate valid consent and
legal basis have been obtained.
