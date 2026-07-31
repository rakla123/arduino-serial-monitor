# Usage

## Connect

1. Connect the programmed Arduino Nano by USB.
2. Close Arduino IDE Serial Monitor or any other program using the COM port.
3. Start `ArduinoSerialMonitor.exe`.
4. The application lists available ports and attempts to identify a supported Arduino/USB serial adapter.
5. When connected, the status displays the COM port and `9600 baud`.

If automatic detection does not choose the correct device:

1. Click **Refresh**.
2. Select the Arduino's COM port.
3. Click **Connect**.

The selected port is saved for the next run. It contains no password or other credential.

## Disconnect and reconnect

- Click **Disconnect** to close the port and disable automatic reconnection.
- Click **Connect** to resume connection and automatic reconnection.
- If a connected Nano is unplugged unexpectedly, the application closes the failed connection and retries every two seconds.

The log suppresses repeated detection notices for 15 seconds so an unplugged device does not fill the display with identical messages.

## Read telemetry

The Arduino normally produces a line every two seconds containing ambient temperature, relative humidity, dew point, and PWM command. See [SERIAL-PROTOCOL.md](SERIAL-PROTOCOL.md) for definitions.

Click **Clear** to clear the visible log. To prevent unbounded memory use, the application automatically removes the oldest text after the display grows beyond approximately 250,000 characters.

## Troubleshooting

### Arduino not detected

- Check Windows Device Manager under **Ports (COM & LPT)**.
- Click **Refresh**, select the port manually, and click **Connect**.
- Try a known data-capable USB cable; some cables provide power only.
- Install the correct USB serial driver from the board or chip vendor.

### Access denied or port in use

Close Arduino IDE Serial Monitor, another terminal, ASCOM utility, or any application that has the same COM port open. Disconnect/reconnect the Nano if the owning program has crashed.

### No telemetry after connection

- Confirm firmware upload and 9600 baud.
- Allow several seconds after connection because DTR normally resets the Nano.
- Test the firmware with Arduino IDE Serial Monitor, then close it before reopening this application.

### Sensor error

Check sensor voltage limits, common ground, A4/SDA, A5/SCL, and I²C address. The heater remains disabled while the firmware reports invalid or missing sensor data.

### Wrong COM port selected automatically

Use the manual selector. On systems with several matching USB serial adapters, automatic detection cannot know which physical device controls the heater.
