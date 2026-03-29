# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Does

Easywave2MQTT bridges Niko Easywave RF wireless devices (buttons, switches, blinds) to Home Assistant via MQTT. It reads telegrams from a USB Eldat RX09 transceiver over serial, maps them to device events, and publishes state via MQTT using Home Assistant's discovery protocol.

## Build & Run Commands

```bash
# Build
dotnet build src/Easywave2MQTT.sln

# Run main app
dotnet run --project src/Easywave2Mqtt/Easywave2Mqtt.csproj

# Run emulator (simulates USB transceiver, for development without hardware)
dotnet run --project src/EldatEmulator/EldatEmulator.csproj

# Run serial port diagnostics tool
dotnet run --project src/SerialTester/SerialTester.csproj

# Publish (for Home Assistant add-on deployment)
dotnet publish -c Release src/Easywave2Mqtt/Easywave2Mqtt.csproj -o addon/Easywave2MQTT/app
```

There are no automated tests in this project.

## Architecture

The application uses three concurrently-running `BackgroundService` implementations that communicate through a custom in-memory pub/sub bus (`IBus`):

1. **`EldatRx09Transceiver`** — reads the USB serial port (57600 baud), parses Easywave telegrams (6-byte address + 1-byte keycode), and publishes `EasywaveTelegram` to the bus. Also listens for `SendEasywaveCommand` to transmit commands back.

2. **`Worker`** — central orchestrator. On startup, reads device config and instantiates all device objects. Routes bus messages:
   - `EasywaveTelegram` → the appropriate device's handler
   - `MqttCommand` → to the target device
   - `MqttMessage` → broadcast to all devices

3. **`MessagingService`** — MQTT client (MQTTnet managed client, auto-reconnects every 5s). Translates internal domain events into MQTT messages and publishes Home Assistant MQTT Discovery configs. Subscribes to `easywave2mqtt/#` and `mqtt2easywave/#`.

### Device Model

```
EasywaveTransmitter
  └── EasywaveButton (one per keycode: A, B, C, D)
        Detects: press, double-press, triple-press, hold, release
        Uses Stopwatch + Timer for pattern disambiguation

IEasywaveEventListener (receivers)
  ├── EasywaveSwitch (Light) — states: On, Off, Unknown
  └── EasywaveBlind (Cover)  — states: Opening, Open, Closing, Closed, Stopped, Unknown
```

Receivers subscribe to specific transmitter addresses+keycodes via `Subscriptions` config. A subscription with `CanSend: true` means the app can control that device (its address belongs to the RX09 transceiver).

### Message Flow (Button Press → MQTT)

```
Serial → EasywaveTelegram → EasywaveButton.HandlePress()
  → SendButtonPress (bus) → MessagingService
  → MQTT publish: easywave2mqtt/{address}/{keycode}/press
```

### Configuration

Runtime config is in `src/Easywave2Mqtt/appsettings.json`. When running as a Home Assistant add-on, `/data/options.json` overrides it.

Key settings: `SerialPort`, `MQTTServer/Port/User/Password`, `EasywaveActionTimeout` (ms until button action finalizes), `EasywaveRepeatTimeout` (ms to debounce repeated RF signals), and a `Devices` array.

Device types: `Transmitter`, `Light`, `Blind`. Transmitters have `Buttons` (list of keycodes). Receivers have `Subscriptions` linking transmitter addresses+keycodes, with `CanSend` for devices the RX09 can control.

## Target Framework

The solution targets **.NET 10** with `Nullable` and `ImplicitUsings` enabled. The Dockerfile uses the .NET 10 runtime image.

## Logging

Uses Serilog with `LoggerMessage` source-generated attributes (compile-time). Log level is runtime-configurable via `LogLevel` setting. Format: `[HH:mm:ss.fff LVL SourceContext] Message`.
