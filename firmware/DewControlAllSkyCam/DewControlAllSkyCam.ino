#include <Wire.h>
#include <Adafruit_SHT31.h>

/*
 * Arduino Nano SHT3x dew-heater controller
 * Copyright 2026 FlapAstro
 *
 * IMPORTANT: D5 only provides a PWM control signal. Never power a heater
 * directly from an Arduino pin. Use a suitable logic-level MOSFET driver,
 * an independently rated and fused heater supply, and a common ground.
 */

/* ---------------- User settings ---------------- */
const uint8_t HEATER_PWM_PIN = 5;
const uint8_t SHT3X_I2C_ADDRESS = 0x44;
const float DEW_MARGIN_C = 2.0F;
const float PWM_GAIN = 40.0F;
const unsigned long SAMPLE_INTERVAL_MS = 2000UL;
/* ------------------------------------------------ */

Adafruit_SHT31 sht3x = Adafruit_SHT31();

float calculateDewPointC(float temperatureC, float relativeHumidity)
{
  const float a = 17.62F;
  const float b = 243.12F;
  const float gamma = log(relativeHumidity / 100.0F) +
                      (a * temperatureC) / (b + temperatureC);
  return (b * gamma) / (a - gamma);
}

void forceHeaterOff()
{
  analogWrite(HEATER_PWM_PIN, 0);
}

void setup()
{
  // Establish a safe output state before initializing the sensor or serial I/O.
  pinMode(HEATER_PWM_PIN, OUTPUT);
  forceHeaterOff();

  Serial.begin(9600);
  Wire.begin();

  if (!sht3x.begin(SHT3X_I2C_ADDRESS))
  {
    Serial.print(F("ERROR: SHT3x sensor not found at I2C address 0x"));
    Serial.println(SHT3X_I2C_ADDRESS, HEX);

    // Fail safe: keep the heater off and report the problem periodically.
    while (true)
    {
      forceHeaterOff();
      delay(5000);
      Serial.println(F("ERROR: heater disabled; check SHT3x wiring"));
    }
  }

  Serial.print(F("SHT3x dew-heater controller ready at I2C address 0x"));
  Serial.println(SHT3X_I2C_ADDRESS, HEX);
}

void loop()
{
  delay(SAMPLE_INTERVAL_MS);

  const float temperatureC = sht3x.readTemperature();
  const float relativeHumidity = sht3x.readHumidity();

  if (isnan(temperatureC) || isnan(relativeHumidity) ||
      relativeHumidity <= 0.0F || relativeHumidity > 100.0F)
  {
    forceHeaterOff();
    Serial.println(F("ERROR: invalid SHT3x reading; heater disabled"));
    return;
  }

  const float dewPointC = calculateDewPointC(temperatureC, relativeHumidity);
  if (isnan(dewPointC) || isinf(dewPointC))
  {
    forceHeaterOff();
    Serial.println(F("ERROR: dew-point calculation failed; heater disabled"));
    return;
  }

  // Without a separate lens-temperature sensor, this is an ambient
  // dew-risk controller. It increases power as ambient temperature enters
  // the configured margin above the calculated dew point.
  const float errorC = (dewPointC + DEW_MARGIN_C) - temperatureC;
  int pwm = 0;
  if (errorC > 0.0F)
    pwm = constrain((int)(errorC * PWM_GAIN), 0, 255);

  analogWrite(HEATER_PWM_PIN, pwm);

  // Human-readable protocol consumed as plain text by the Windows monitor.
  Serial.print(F("T: "));
  Serial.print(temperatureC, 2);
  Serial.print(F(" C  RH: "));
  Serial.print(relativeHumidity, 2);
  Serial.print(F(" %  Td: "));
  Serial.print(dewPointC, 2);
  Serial.print(F(" C  PWM: "));
  Serial.println(pwm);
}
