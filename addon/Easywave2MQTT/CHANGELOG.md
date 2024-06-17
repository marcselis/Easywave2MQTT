# Version History

|Version|Changes|
|-------|-------|
| 1.0.1 | Set initial EasyWave switch state to `Unknown`, to allow processing of HomeAssistant retained commands and communicating the new state to HomeAssistant.|
| 1.0.0 | Upgrade to .NET 8.<br/>Log declaration of Lights & Blinds<br/>Switch to managed MQTT client that automatically reconnects. |
| 0.7.1 | Fix blind states |
| 0.7.0 | First try to add support for blinds |
| 0.6 | Automatically reconnect to MQTT server when connection breaks<br/>Persist state of lights in MQTT|
| 0.5.3 beta | Fix bug in detection mechanism for button repeats |
| 0.5.2 beta | Fix crash when serialport is throwing IOException for a timeout, instead of a TimeoutException |
| 0.5 beta | Allow addon to start when no serial ports are found, or when a non-existing port was specified |
| 0.4 beta | Simplified addon building & startup: <br/>- addon binaries & config file are directly editable in app subfolder, making it easier for non-developers to alter the configuration and to deploy the addon in Home Assistant.<br/>- options are directly read from data subfolder (if available).<br/>Lowered logging level for a few `MessagingService` messages that were logged twice, but in a different way. |
| 0.3 beta | Automatically add all serial devices to addon container.<br/>Added configuration options for serial device, mqtt & log level.<br/>Switched to SeriLog for logging.<br/>Switched to .NET 7.0.<br/>Added test project to simulate a local Eldat RX09 device. This project requires on the [com0com Null-modem emulator](https://files.akeo.ie/blog/com0com.7z) and an MQTT server where the Easywave2Mqtt process is connected to.|
| 0.2 alpha | First public version.|
| 0.1 alpha | First version.|
