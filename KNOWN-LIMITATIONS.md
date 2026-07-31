# Known limitations

These constraints apply to Arduino Serial Monitor 1.0.0 and the supplied reference firmware.

## Windows application

1. The application is Windows-only and targets .NET Framework 4.7.2.
2. Serial communication is fixed at 9600 baud, 8 data bits, no parity, and one stop bit.
3. Telemetry is displayed as raw text. There are no charts, CSV export, alarms, remote access, or field-level validation.
4. Automatic port detection is heuristic. Multiple similar adapters require manual selection.
5. The application does not upload firmware or install USB drivers.
6. Opening a port asserts DTR and generally resets a classic Nano.
7. Automatic reconnection cannot recover a port held exclusively by another application.
8. The last COM-port preference is stored in the current Windows user profile and is not portable.
9. The display is intentionally bounded; sufficiently old telemetry is removed from memory.

## Sensor and control model

1. The reference firmware supports the Adafruit SHT31 library and assumes an SHT31/SHT35-compatible sensor at `0x44` unless edited.
2. The sensor measures ambient air, not the lens, dome, or optical-window surface.
3. Heater control is therefore based on ambient dew risk and is not closed-loop surface-temperature control.
4. `DEW_MARGIN_C` and `PWM_GAIN` are installation-specific tuning values. Defaults are not guaranteed to suit a particular heater, enclosure, climate, or optical system.
5. The Magnus formula is an approximation and inherits sensor accuracy, placement, self-heating, response-time, and condensation errors.
6. At very low or invalid humidity the dew-point calculation is rejected and the heater is switched off.
7. The two-second blocking sample interval is appropriate for this simple controller but prevents other time-sensitive firmware tasks.
8. The firmware does not store configuration in EEPROM and does not accept runtime commands.

## Hardware and safety

1. No heater power stage, fuse, power supply, PCB, enclosure, or certified wiring design is included.
2. D5 cannot drive a heater directly; it is only a MOSFET control signal.
3. SHT3x breakout voltage tolerance varies. Incorrect voltage can destroy a bare sensor or unsuitable module.
4. The fail-safe firmware switches D5 to zero for detected sensor/calculation errors, but software cannot protect against a shorted MOSFET, wiring fault, corrupt bootloader, unsuitable pull-down, or failed power component.
5. Hardware-in-the-loop operation, EMC behavior, unattended safety, and regulatory compliance have not been independently certified.
