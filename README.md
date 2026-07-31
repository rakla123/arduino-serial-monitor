# Arduino Serial Monitor

Arduino Serial Monitor is a small Windows application for viewing telemetry from an Arduino Nano-based all-sky-camera dew-heater controller. The matching firmware reads an external SHT31/SHT35 humidity and temperature sensor, calculates dew point, drives an external heater through a PWM power stage, and reports its measurements over USB serial.

The application automatically reconnects when a supported USB serial adapter is unplugged and reattached. A manual COM-port selector is provided for computers with several serial devices or adapters that Windows identifies with an unusual name.

## Features

- Windows Forms serial monitor with no third-party desktop dependencies.
- Fixed 9600-baud connection matching the supplied Arduino firmware.
- Automatic detection of common Arduino, CH340/CH341/CH910, CP210x, FTDI, WCH, and USB-serial adapters.
- Manual COM-port selection and persistence of the last successful port.
- Automatic reconnect after a temporary disconnect.
- Bounded display buffer to prevent indefinite memory growth.
- Correct handling of serial line endings split between read operations.
- Matching fail-safe Arduino Nano firmware for an SHT3x sensor and PWM heater output.
- Reproducible Windows release packaging with a SHA-256 checksum.

## Quick start

1. Read [Hardware and wiring](docs/HARDWARE.md), especially the heater-driver safety warning.
2. Install the Arduino firmware as described in [Installation](docs/INSTALLATION.md).
3. Download and extract the latest Windows ZIP from [GitHub Releases](https://github.com/rakla123/arduino-serial-monitor/releases).
4. Connect the Arduino Nano by USB.
5. Start `ArduinoSerialMonitor.exe`.
6. If automatic detection cannot identify the Nano, select its COM port and click **Connect**.

Detailed operating instructions are in [Usage](docs/USAGE.md). The exact output format is documented in [Serial protocol](docs/SERIAL-PROTOCOL.md).

## Hardware summary

- Arduino Nano or compatible ATmega328P board.
- SHT31 or SHT35-compatible I²C temperature/humidity breakout at address `0x44`.
- External resistive dew heater.
- Logic-level N-channel MOSFET power stage appropriate for the heater current.
- Separately rated and fused heater power supply.
- USB connection to a Windows computer.

The Arduino pin is a control signal only. **Never connect or power a heater directly from D5 or any other Arduino I/O pin.**

## Repository layout

```text
firmware/DewControlAllSkyCam/     Arduino Nano firmware
src/ArduinoSerialMonitor/         Windows Forms source
docs/                             Installation, wiring, protocol, and usage
.github/workflows/                Automated build and release workflows
Build-Release.ps1                 Local release builder
```

## Building from source

Requirements:

- Windows 10 or Windows 11.
- Visual Studio 2022 or newer with **.NET desktop development**.
- .NET Framework 4.7.2 targeting pack.

Build in Visual Studio by opening `ArduinoSerialMonitor.sln`, selecting **Release**, and choosing **Build Solution**.

From a Developer PowerShell prompt:

```powershell
msbuild .\ArduinoSerialMonitor.sln /restore /p:Configuration=Release
```

Create the distributable ZIP and checksum:

```powershell
powershell -ExecutionPolicy Bypass -File .\Build-Release.ps1
```

## Safety and responsibility

This project controls external electrical hardware. Select the MOSFET, wiring, connector, fuse, power supply, and enclosure for the actual heater voltage and current. Verify the heater is off during reset, disconnection, invalid sensor readings, and firmware failure before unattended operation.

The software and firmware are provided **as is**, without warranty or condition. Fitness for a particular purpose, electrical safety, equipment protection, configuration, and use remain solely the user's responsibility.

## Known limitations

See [KNOWN-LIMITATIONS.md](KNOWN-LIMITATIONS.md). In particular, the supplied design measures ambient temperature rather than the optical-window or lens temperature. It is an ambient dew-risk controller, not a closed-loop surface-temperature controller.

## License

Copyright 2026 FlapAstro.

This project is source-available under the [PolyForm Noncommercial License 1.0.0](LICENSE.md). Personal and other permitted noncommercial use is allowed; commercial use is restricted.

Third-party software and trademarks remain the property of their respective owners. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
