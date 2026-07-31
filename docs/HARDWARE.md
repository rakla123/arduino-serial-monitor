# Hardware and wiring

## Supported reference hardware

The supplied firmware was prepared for a classic Arduino Nano-compatible ATmega328P board and an external SHT31/SHT35-compatible sensor breakout using I²C address `0x44`.

### Sensor wiring

| SHT3x breakout | Arduino Nano | Purpose |
|---|---|---|
| GND | GND | Common ground |
| VIN/VCC | Per breakout specification | Sensor power |
| SDA | A4 | I²C data |
| SCL | A5 | I²C clock |

Many breakout boards include a regulator and level shifting, but a bare SHT3x sensor is not a generic 5 V device. Follow the specific breakout manufacturer's voltage limits. Do not assume that every module marked SHT31 or SHT35 accepts the Nano's 5 V rail.

## Heater output

Firmware pin D5 produces a 0–5 V PWM control signal. It cannot supply heater current.

A typical power path is:

```text
Arduino D5 ── gate resistor ── logic-level N-MOSFET gate
Arduino GND ────────────────┬── MOSFET source
Heater supply negative ─────┘
Heater supply positive ── fuse ── heater ── MOSFET drain
```

Recommended supporting parts include a gate pull-down resistor so the MOSFET remains off while the Nano resets, suitable connectors and wire, strain relief, an enclosure, and a fuse selected for the heater circuit. A resistive heater does not normally require a flyback diode; add appropriate transient protection if the actual load is inductive.

## Electrical safety checklist

- Determine heater resistance and maximum current before selecting components.
- Use a logic-level MOSFET that is fully enhanced at the Nano's gate voltage.
- Check MOSFET dissipation and provide cooling when needed.
- Fuse the heater supply close to its source.
- Join controller and heater-supply grounds when using the illustrated low-side driver.
- Confirm D5 never carries heater current.
- Confirm the heater is off when the Nano is disconnected, resetting, or unprogrammed.
- Test sensor failures and USB disconnection before unattended operation.
- Keep electronics protected from condensation.

The repository does not provide a certified electrical design. The user is responsible for adapting and validating the circuit for the actual supply, heater, environment, and applicable regulations.
