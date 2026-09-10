# SNAPVERE Capture Engine

Capture engine je lokalni sloj za dohvat zaslona i prozora. Ne šalje pixel podatke na mrežu i ne ovisi o cloudu.

## Monitor capture

Preferirani backend je Windows.Graphics.Capture uz Direct3D 11. Kada očekivani WGC/platform/native problem onemogući monitor capture, `ResilientScreenCaptureService` može prijeći na GDI monitor fallback. Cancellation se ne pretvara u skriveni fallback posao.

## Window capture

Window Capture koristi enumeraciju top-level prozora, DWM extended frame bounds i geometric hit testing nad unaprijed snimljenim popisom prozora. Finalni capture koristi WGC `CreateForWindow`; SNAPVERE ne glumi Window Capture cropanjem dijela ekrana.

## Region capture

Region Capture prvo zamrzava frame odabranog monitora. Korisnik zatim odabire fizički pixel rectangle nad tim istim frameom. Finalni crop i anotacije nastaju iz istog izvora koji je prikazan u editoru.

## Resursi i performanse

Capture resursi se stvaraju lazy. Aplikacija ne drži aktivan D3D capture session tijekom običnog idle rada u trayu. Frameovi i privremene grafičke strukture ostaju vezani uz trajanje capture workflowa.

## Greške

Očekivane platform/native greške mapiraju se na jasne korisničke poruke. Ne očekuje se da unsupported WGC feature sruši tray host. Fatalni initialization problemi zapisuju se u lokalni startup log bez spremanja pixel sadržaja.
