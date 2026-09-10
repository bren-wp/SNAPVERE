# SNAPVERE v0.0.5

This patch release fixes the user-visible startup behavior of the Windows executables.

## Fixed

- Manual launches now open the SNAPVERE Capture Center instead of starting silently in the system tray.
- Windows startup remains tray-first and hidden by using the explicit `--background` launch argument.
- Portable launches preserve the same manual-versus-background behavior after extracting the embedded application payload.
- Setup now has an automated UI probe so release validation verifies that the installer can actually display its window.

## Release validation

The x64 and x86 release packages are validated for:

- restore, build and unit tests;
- Setup and Portable package generation;
- install/uninstall lifecycle and startup-registration cleanup;
- visible main-window creation for normal installed and Portable launches;
- hidden tray-only behavior for `--background` launches;
- interactive Setup UI creation;
- SHA-256 release manifest integrity.

`v0.0.4` remains unchanged. This release supersedes it for normal downloads.
