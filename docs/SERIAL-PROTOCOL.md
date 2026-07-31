# Serial protocol

## Connection settings

| Parameter | Value |
|---|---|
| Baud rate | 9600 |
| Data bits | 8 |
| Parity | None |
| Stop bits | 1 |
| Handshake | None |
| Encoding | ASCII |
| Line ending | LF (`\n`) |

Opening the port asserts DTR, which normally resets a classic Arduino Nano. The startup banner can therefore reappear after each connection.

## Telemetry line

The firmware writes one human-readable line approximately every two seconds:

```text
T: 12.34 C  RH: 78.90 %  Td: 8.72 C  PWM: 0
```

Fields:

- `T`: ambient SHT3x temperature in degrees Celsius.
- `RH`: relative humidity in percent.
- `Td`: calculated dew-point temperature in degrees Celsius.
- `PWM`: heater command from 0 to 255.

The Windows application displays incoming data as plain text. Version 1.0.0 does not parse fields into charts, validate telemetry values, or send commands to the Arduino.

## Status and error lines

Examples include:

```text
SHT3x dew-heater controller ready at I2C address 0x44
ERROR: SHT3x sensor not found at I2C address 0x44
ERROR: heater disabled; check SHT3x wiring
ERROR: invalid SHT3x reading; heater disabled
ERROR: dew-point calculation failed; heater disabled
```

Error paths force PWM to zero. Messages are informational text rather than a versioned machine protocol; future firmware can add fields without breaking the raw monitor.
