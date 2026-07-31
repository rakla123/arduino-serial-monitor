# Installation

## 1. Prepare the hardware

Assemble and verify the circuit in [HARDWARE.md](HARDWARE.md) before connecting a heater. Initially test with the heater power supply disconnected.

## 2. Install the Arduino IDE dependencies

Install a current Arduino IDE and add these libraries through **Tools → Manage Libraries**:

- `Adafruit SHT31 Library`
- `Adafruit BusIO` when requested as a dependency

The built-in `Wire` library provides I²C communication.

## 3. Upload the firmware

1. Open `firmware/DewControlAllSkyCam/DewControlAllSkyCam.ino` in Arduino IDE.
2. Select **Arduino Nano** under **Tools → Board**.
3. Select the processor appropriate for the board. Some older or clone Nanos require **ATmega328P (Old Bootloader)**.
4. Select the Nano's COM port.
5. Confirm the SHT3x I²C address. The firmware defaults to `0x44`; change `SHT3X_I2C_ADDRESS` if the module is configured as `0x45`.
6. Upload the sketch.
7. Open Arduino IDE Serial Monitor at **9600 baud** and confirm that plausible readings appear every two seconds.

Expected output resembles:

```text
SHT3x dew-heater controller ready at I2C address 0x44
T: 12.34 C  RH: 78.90 %  Td: 8.72 C  PWM: 0
```

Disconnect Arduino IDE Serial Monitor before starting the Windows application. Only one program can normally open a COM port at a time.

## 4. Install the Windows application

1. Download `Arduino-Serial-Monitor-1.0.0.zip` from the repository's Releases page.
2. Optionally verify the adjacent `.sha256` checksum:

   ```powershell
   Get-FileHash .\Arduino-Serial-Monitor-1.0.0.zip -Algorithm SHA256
   ```

3. Extract the ZIP to a writable folder.
4. Run `ArduinoSerialMonitor.exe`.

No formal installer or administrator access is required. Windows 10 and Windows 11 normally include a compatible .NET Framework runtime. If startup reports a missing framework, install .NET Framework 4.7.2 or newer from Microsoft.

## 5. USB serial drivers

Windows Update usually installs drivers automatically. Depending on the Nano or clone, the USB interface may appear as Arduino, FTDI, CH340/CH341/CH910, CP210x, WCH, or a generic USB serial device. Obtain drivers only from the board/vendor or chip manufacturer's official source.
