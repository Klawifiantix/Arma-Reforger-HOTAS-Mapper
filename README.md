# Arma Reforger HOTAS Mapper

A lightweight, standalone tool to easily bind Joystick and Throttle axes/buttons for Arma Reforger.

## Why use this?
Editing the `.conf` files in Arma Reforger manually can be a pain. This tool provides a simple GUI to detect your hardware and assign inputs directly to the configuration blocks.

## Features
- **Auto-Discovery:** Automatically lists all connected game controllers.
- **Smart Parsing:** Scans your `.conf` file and lists every available Action.
- **Preset Support:** Handles "next", "previous", "click", and "hold" presets separately.
- **Search:** Quickly find the action you want to bind.

## Setup & Required Files
To get started, you need a valid configuration file. This repository includes a template called `T16000 Bindings.conf` which you can use as a base.

1. **Locate your config folder:** Go to `C:\Users\YOURNAME\Documents\My Games\ArmaReforger\profile\.save\settings\`
2. **Create the subfolder:** If it doesn't exist, create a folder named `customInputConfigs`.
3. **Add the file:** Place the `T16000 Bindings.conf` from this repository into that folder.

## How to use the Tool
1. Download the latest `ArmaBindingTool.exe` from the **Releases** section.
2. Run the tool and click **"Load Arma .conf file"**.
3. Select the file you just placed in your `customInputConfigs` folder.
4. Search for an action (e.g., "Collective") in the tool.
5. Select the action in the list and move your joystick axis or press a button.
6. The tool saves the changes instantly to the file.
