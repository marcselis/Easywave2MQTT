# Easywave2MQTT: Easywave support for [Home Assistant](https://www.home-assistant.io/).
I started this project because I have [Niko](https://www.niko.eu/) Easywave devices in my house, but wanted to automate them with [Home Assistant](https://www.home-assistant.io/).  [Home Assistant](https://www.home-assistant.io/) has no built-in support for Easywave, but is very extensible and easy to interface with via its [MQTT Integration](https://www.home-assistant.io/integrations/mqtt/).

I chose for this approach over a native integration because you have to write those in Python and I am more a C# guy :smiley:. Since .NET 5 you can program in C# for Linux, which means also for [Home Assistant OS on Raspberry PI](https://www.home-assistant.io/installation/raspberrypi).

**This project is a work-in-progress that I work on in my free time.**

## What is Easywave?

Easywave is a proprietary wireless protocol developed by [Niko](https://www.niko.eu/) & [Eldat](https://www.eldat.de/), using the robust RF 868 Mhz technology that enables indoor wireless communication up to 30m.  It is a very simple unidirectional protocol. Easywave is a very simple protocol that allows unidirectional (one way) communication between a transmitter and one or more receivers.

Very little technical information can be found on the protocol itself, but what I have detected thus far is that an Easywave message that is sent by a transmitter has 2 parts:

- the transmitters address (unique 6-byte code).
- a single byte payload, indicating what button was pressed.

It is the receiver that decides what messages it processes.  Most of the time this decision process is done by pressing a link button
on the receiver and triggering the transmitter to send the message. For example, the 
[Niko Single-Pole RF Receiver](https://www.niko.eu/en/products/switching-material-and-socket-outlets/wireless-solutions/one-channel-flush-mounting-wireless-receiver-single-pole-potential-free-productmodel-niko-3f9e1469-93a4-5b9e-94aa-da26caa6a03a)
can be linked in 1- or 2-key mode:

- In 1-key mode, the receiver toggles its state when it receives a fixed button press message from the linked transmitter.
  Example:
  - 123456,A ==> ON
  - 123456,A ==> OFF
  - 123456,A ==> ON
- In 2-key mode, the receiver switches on when it receives the press message for the button that was pressed during linkin, and it switches off when it receives a press message for the next button.
  Example:
  - 123456,A ==> ON
  - 123456,A ==> nothing happens
  - 123456,B ==> OFF

### How to talk 'Easywave' using a computer?

Eldat produces the [RX09 USB Transceiver](https://www.eldat.de/produkte/schnittstellen/rx09e_en.html). This USB stick emulates a serial port that a computer program can use to listen to Easywave traffic. By sending commands to the serial port, you can instruct the USB stick to send Easywave messages, but only using a limited number of addresses (0 to 64/128, depending on the stick model), requiring you to (also) link the transceiver to the receivers you want to control.

### Issues with the Easywave protocol & devices.

In contrast to other protocols (like [Zigbee](https://en.wikipedia.org/wiki/Zigbee), an Easywave receiver does not give feedback, leaving no way for the program to check whether the message was actually processed and what the result was.  This leads to all kinds of problems:

- When the receiver is a dimmer, there is no way of knowing on what level the light connected to the dimmer is burning or even if it is still burning at all, after sending a repeated stream of dim down messages.
  This makes makes it very difficult for this project to reflect the state of lights in the house in Home Assistant
- A typical Easywave wireless switch, like the [Niko 41-00001](https://www.niko.eu/en/products/wireless-controls/wireless-switch-with-two-buttons-productmodel-niko-fbacd5f6-94fc-5ce9-af7c-7394469b12c0) will send 2 to 4 repeated messages when a user presses a button.  This makes it difficult to predict in what state a receiver that is linked in 1-button mode will be after processing these messages, when looking at the messages being sent.
- These repeated messages makes it also hard to detect a button being held and double- & tripple pressed.

## Architecture

![Easywave2MQTT Architecture](https://github.com/marcselis/Easywave2MQTT/blob/main/Architecture.png)

This program has 4 main parts:

- A **Easyweave service** that takes care of the Easywave communication using the [RX09 USB Transceiver](https://www.eldat.de/produkte/schnittstellen/rx09e_en.html).
- An **MQTT service** that takes care of communication over MQTT.
- Some **Worker Logic** that uses the other 2 services to detect what is happening in the Easywave world and 
  communicating it with [Home Assistant](https://www.home-assistant.io/).
- Communication between the different components is done through a custom-built and very rudimentary in-memory **bus**.

## Getting started

### Building & Debugging

- Check that you have the following tools available:

  - [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
  - A code editor, like [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio](https://visualstudio.microsoft.com/), if you want to make changes.
  - Update the contents of the `appsettings.json` file.  See the [Configuration Section](#configuration) for more information.
- You should be able to test and debug this project under both Windows & Linux without any code changes.

## Configuration

For now, the configuration is done through the `appsettings.json` file.

- `SerialPort`: the device where your [RX09 USB Transceiver](https://www.eldat.de/produkte/schnittstellen/rx09e_en.html) can be found.  (e.g. `COM1` in Windows, `/dev/ttyUSB0` in Linux)
- `EasywaveActionTimeout` determines how many milliseconds to wait before concluding that an Easywave button action is finished.
- `EasywaveRepeatTimeout` determines within how many milliseconds a new message is considered as a repeat.
- `MQTTServer`: IP Address or name of the MQTT broker.
- `MQTTPort`: The port that the MQTT broker listens on. { get; set; }
- `MQTTUser`: Username to connect to the MQTT broker
- `MQTTPassword`: Password to connect to the MQTT broker
- Alter the `Devices` section to match your own setup.
  - Start with declaring your Easywave transmitters and their buttons.  These will be synchronized to Home Assistant, allowing you to link them in automations.  Easywave2MQTT does its best to detect 5 different button actions:
    - **press**: a button was pressed for a short time
    - **double_press**: a button was pressed 2 times within the configured `ActionTimeoutInMilliseconds`.
    - **triple_press**: a button was pressed 3 times within the configured `ActionTimeoutInMilliseconds`.
    - **hold**: the button was held longer than the configured `ActionTimeoutInMilliseconds`.
    - **released**: the button was released after being held.
  - **Optional**: Declare the receivers and the transmitter buttons you have them linked to.  Doing so will allow these receivers and their current state to be synchronized to Home Assistant.  
  If you want to make it possible to control your receivers from Home Assistant, you'll need to manually link them to an RX09 message and add that message as a subscription with `CanSend: "true"`.
