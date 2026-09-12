import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const extensionsRoot = path.resolve(here, '..');
const storeRoot = path.join(extensionsRoot, 'store');
const listingPath = path.join(storeRoot, 'listing.json');
const privacyPath = path.join(extensionsRoot, 'PRIVACY.md');
const reviewerPath = path.join(storeRoot, 'reviewer-notes.md');
const browsers = ['chrome', 'edge', 'opera', 'firefox'];
const expectedPackages = {
  chrome: 'SNAPVERE-Chrome.zip',
  edge: 'SNAPVERE-Edge.zip',
  opera: 'SNAPVERE-Opera.zip',
  firefox: 'SNAPVERE-Firefox.zip'
};
const allowedPermissions = ['activeTab', 'downloads', 'scripting', 'storage'].sort();

function fail(message) {
  throw new Error(`Store readiness validation failed: ${message}`);
}

function readJson(file) {
  try { return JSON.parse(fs.readFileSync(file, 'utf8')); }
  catch (error) { fail(`${path.relative(extensionsRoot, file)} is not valid JSON: ${error.message}`); }
}

function pngDimensions(file) {
  const b = fs.readFileSync(file);
  if (b.length < 24 || b.toString('hex', 0, 8) !== '89504e470d0a1a0a') fail(`${path.relative(extensionsRoot, file)} is not a PNG`);
  return [b.readUInt32BE(16), b.readUInt32BE(20)];
}

function assertHttps(value, field) {
  if (typeof value !== 'string' || !value.startsWith('https://')) fail(`${field} must be an https URL`);
}

const listing = readJson(listingPath);
if (listing.schemaVersion !== 1) fail('unsupported listing schemaVersion');
if (listing.product !== 'SNAPVERE') fail('product must be SNAPVERE');
if (!/^\d+\.\d+\.\d+$/.test(listing.extensionVersion ?? '')) fail('extensionVersion must use x.y.z');
if (listing.developmentChannel !== 'post-v0.1.0-source-development') fail('developmentChannel must preserve the historical release boundary');
if (listing.category !== 'Productivity') fail('category must be Productivity');
if (typeof listing.singlePurpose !== 'string' || listing.singlePurpose.length < 80) fail('singlePurpose must clearly describe the extension');
assertHttps(listing.website, 'website');
assertHttps(listing.supportUrl, 'supportUrl');
assertHttps(listing.privacyPolicyUrl, 'privacyPolicyUrl');
if (!listing.privacyPolicyUrl.endsWith('/ekstenzije/PRIVACY.md')) fail('privacyPolicyUrl must reference the version-controlled browser privacy policy');

const permissionKeys = Object.keys(listing.permissions ?? {}).sort();
if (JSON.stringify(permissionKeys) !== JSON.stringify(allowedPermissions)) fail(`permission justifications must be exactly ${allowedPermissions.join(', ')}`);
for (const [key, justification] of Object.entries(listing.permissions)) {
  if (typeof justification !== 'string' || justification.length < 40) fail(`permission ${key} needs a substantive justification`);
}

const privacyFlags = ['collectsOffDevice','transmitsOffDevice','sellsData','usesAnalytics','usesTelemetry','usesAds','usesRemoteCode','requiresAccount','requiresPayment'];
for (const flag of privacyFlags) if (listing.dataPractices?.[flag] !== false) fail(`dataPractices.${flag} must be false for the current local-first implementation`);
if (!Array.isArray(listing.dataPractices?.localProcessing) || listing.dataPractices.localProcessing.length < 3) fail('localProcessing disclosures are incomplete');

for (const locale of ['en', 'hr']) {
  const data = listing.locales?.[locale];
  if (!data) fail(`missing ${locale} listing locale`);
  if (data.name !== 'SNAPVERE') fail(`${locale} listing name mismatch`);
  if (typeof data.shortDescription !== 'string' || data.shortDescription.length < 30 || data.shortDescription.length > 132) fail(`${locale} shortDescription must be 30..132 characters`);
  if (typeof data.longDescription !== 'string' || data.longDescription.length < 250) fail(`${locale} longDescription is too short`);
}

for (const browser of browsers) {
  const manifest = readJson(path.join(extensionsRoot, browser, 'manifest.json'));
  if (manifest.version !== listing.extensionVersion) fail(`${browser} manifest version does not match listing version`);
  const permissions = [...(manifest.permissions ?? [])].sort();
  if (JSON.stringify(permissions) !== JSON.stringify(allowedPermissions)) fail(`${browser} manifest permission set changed`);
  if ('host_permissions' in manifest) fail(`${browser} must not declare host_permissions`);
  const store = listing.stores?.[browser];
  if (!store) fail(`missing ${browser} store metadata`);
  if (store.package !== expectedPackages[browser]) fail(`${browser} package name mismatch`);
  if (!Array.isArray(store.screenshots) || store.screenshots.length < 2) fail(`${browser} requires at least two prepared screenshots in the submission kit`);
  for (const asset of store.screenshots) {
    const file = path.join(storeRoot, asset);
    if (!fs.existsSync(file)) fail(`${browser} screenshot missing: ${asset}`);
  }
}

const expectedDims = new Map([
  ['assets/screenshot-capture-1280x800.png', [1280, 800]],
  ['assets/screenshot-region-1280x800.png', [1280, 800]],
  ['assets/screenshot-settings-1280x800.png', [1280, 800]],
  ['assets/opera-screenshot-capture-612x408.png', [612, 408]],
  ['assets/opera-screenshot-region-612x408.png', [612, 408]],
  ['assets/promo-small-440x280.png', [440, 280]],
  ['assets/promo-marquee-1400x560.png', [1400, 560]]
]);
for (const [asset, expected] of expectedDims) {
  const file = path.join(storeRoot, asset);
  if (!fs.existsSync(file)) fail(`required store asset missing: ${asset}`);
  const actual = pngDimensions(file);
  if (actual[0] !== expected[0] || actual[1] !== expected[1]) fail(`${asset} must be ${expected[0]}x${expected[1]}, got ${actual[0]}x${actual[1]}`);
}

if (listing.stores.chrome.smallPromo !== 'assets/promo-small-440x280.png') fail('Chrome small promo asset mismatch');
if (listing.stores.chrome.marqueePromo !== 'assets/promo-marquee-1400x560.png') fail('Chrome marquee asset mismatch');
if (listing.stores.edge.smallPromo !== 'assets/promo-small-440x280.png') fail('Edge small promo asset mismatch');
if (listing.stores.edge.largePromo !== 'assets/promo-marquee-1400x560.png') fail('Edge large promo asset mismatch');
if (listing.stores.firefox.sourceCodePackageRequired !== false) fail('Firefox source-code-package declaration must match the current unminified/no-build package');

for (const [file, requiredPhrases] of [
  [privacyPath, ['does not automatically upload screenshots', 'activeTab', 'scripting', 'downloads', 'storage', 'does not request `<all_urls>`']],
  [reviewerPath, ['Suggested functional test', 'No account, login, payment', 'Firefox-specific note']]
]) {
  if (!fs.existsSync(file)) fail(`missing ${path.relative(extensionsRoot, file)}`);
  const content = fs.readFileSync(file, 'utf8');
  for (const phrase of requiredPhrases) if (!content.includes(phrase)) fail(`${path.relative(extensionsRoot, file)} missing required disclosure: ${phrase}`);
}

console.log('SNAPVERE browser store readiness validation passed.');
