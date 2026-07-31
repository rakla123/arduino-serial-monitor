# Changelog

## 1.0.0 - 2026-07-31

- First public source and binary release.
- Added matching Arduino Nano SHT3x dew-heater firmware.
- Forced the heater output off before sensor initialization and on all invalid-reading paths.
- Added humidity and dew-point validation.
- Added manual COM-port selection, refresh, and persistent preferred-port storage.
- Expanded automatic USB serial adapter detection.
- Added reconnect throttling and repeated-message suppression.
- Bounded the desktop log buffer and corrected split serial line endings.
- Added installation, wiring, protocol, usage, troubleshooting, safety, and limitations documentation.
- Added reproducible build and GitHub release workflows.
