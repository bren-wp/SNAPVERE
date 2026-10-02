SNAPVERE WEBSITE DOWNLOAD UPGRADE v2.2.0
=========================================

Product packages represented: SNAPVERE v0.1.22
Build date: 2026-10-02

CONTENTS
--------
1) main-site-patch/
   Replacement /download/ and /hr/download/ pages plus local CSS/logo assets.

2) download-subdomain/
   Production PHP download service for https://download.snapvere.com/

   Protected local packages:
   - SNAPVERE-Setup.exe
   - SNAPVERE-Portable.exe
   - SNAPVERE-Chrome.zip
   - SNAPVERE-Edge.zip
   - SNAPVERE-Firefox.zip
   - SNAPVERE-Opera.zip

DOWNLOAD FLOW
-------------
1. Visitor selects one of six packages.
2. Visitor must enter an email address.
3. Disposable/reserved/test addresses are rejected.
4. Server emails a short-lived verification link.
5. Only successful verification creates a short-lived package-specific download token.
6. File size and SHA-256 are checked before link issuance and again before byte delivery.
7. Package bytes are streamed from protected local storage.
8. On the first authorised GET, info@brendigo.com receives:
   - submitted email
   - selected package/file
   - SNAPVERE version
   - UTC time
   - privacy-safe client reference hash
   - request ID
   - aggregate download count
9. Failed administrator notification mail is queued privately and can be retried with:
   php tools/retry-admin-mail.php

The same email address can download again later. There is no permanent per-email lock.

DEPLOYMENT
----------
download.snapvere.com:
- Preserve existing storage/data files if retaining counter/history.
- Upload the CONTENTS of download-subdomain/ to the subdomain document root.
- Keep storage/data writable by PHP.
- Keep storage/packages blocked from direct HTTP access.
- Run: php tools/self-test.php
- Check /api/health.php and confirm 6 expected + 6 integrity-verified packages.

snapvere.com:
- Copy main-site-patch/assets/css/snapvere-download-page.css to /assets/css/
- Copy main-site-patch/assets/img/snapvere-logo.svg to /assets/img/
- Replace/add /download/index.html and /hr/download/index.html
- Keep all other site content unchanged.

MAIL
----
Default administrator notification: info@brendigo.com
Default visitor verification sender: info@snapvere.com

Real outbound mail delivery depends on the production hosting mail transport. Visitor verification fails closed if mail cannot be sent, so the email requirement cannot be bypassed.

SECURITY
--------
- Same-origin POST validation
- Session CSRF token
- Honeypot
- IP + email rate limiting
- Disposable/reserved/test email rejection
- Cryptographically random verification/download tokens
- Stored token hashes
- Protected package storage
- Exact size + SHA-256 validation
- HTTP Range/resume support
- No public third-party download redirect
- No distributed credentials/secrets
- Hashed client metadata in application logs/admin notification
- Security headers + restrictive CSP
