# Changelog

All notable changes to this project will be documented in this file.

This format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project follows [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-04-07

First public release of CursorCage.

### Added
- Cursor locking to the screen under the pointer (multi-monitor mode).
- Configurable global hotkey to quickly lock/unlock the cursor.
- Full WPF interface with Home and Settings pages.
- Notification area tray icon (open app, settings, check updates, quit).
- English/French multilingual support with dynamic language switching.
- In-app display of the current application version.
- Update checking via GitHub releases.
- Windows installer configuration and build script (`CursorCage.iss`, `Build-Installer.ps1`).

### Improved
- Better readability and visual hierarchy on the Settings page.
- Consistent dark theme for combo boxes and menu items.
- Improved tray menu behavior when switching languages.

### Fixed
- Application exit from tray icon (prevents background ghost process cases).
- Multiple in-game cursor lock edge cases (better stability after `Esc`/menus).
- Consistent localized labels in notifications and system messages.

