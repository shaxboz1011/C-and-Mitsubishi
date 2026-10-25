# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- Bilingual README (English and O'zbekcha) with a screenshot, an I/O map, serial settings,
  build/run steps and a troubleshooting table.
- Open-source project files: `LICENSE` (MIT), `CONTRIBUTING.md`, `SECURITY.md`, this changelog,
  `.editorconfig`, GitHub issue/PR templates, Dependabot config and a GitHub Actions build (CI).
- Status indicators now show a neutral grey state while disconnected, instead of a misleading red.
- **Connect**/**Disconnect** and the COM-port list enable and disable to match the connection
  state, so you cannot start a second connection by accident.
- Clear message boxes when no COM port is selected, when a connection fails, or when a control is
  used before connecting.

### Fixed
- **Reconnect is no longer broken.** The polling flag was never reset after a disconnect, so
  pressing **Connect** again started a thread that exited immediately.
- **No more serial/CPU flooding.** The read loop had no delay and re-read several inputs; it now
  polls on an interval and reuses the values it already read.
- Selecting **Connect** with no COM port no longer throws a `NullReferenceException`.
- Connection failures are now reported instead of claiming "Connected" regardless of the result.
- Errors from the background polling thread are shown on the UI thread instead of being raised
  from the wrong thread.
- Removed the use of `Thread.Abort`; the background thread now stops cleanly.
