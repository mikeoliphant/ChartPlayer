# ChartPlayer

ChartPlayer is a cross-platform application for playing along to charts in [OpenSongChart](https://github.com/mikeoliphant/OpenSongChart) format.

It runs as a VST plugin inside your DAW (or as a [Jack Audio](https://github.com/jackaudio) client) so that you can use any other plugin you like (ie: guitar amp simulation) for your instrument sounds.

![ChartPlayerSultans](https://github.com/user-attachments/assets/17c38536-040f-4a24-b97a-97cbb9548b4d)

# Features

* Support for guitar, bass, drums, keys and vocals
* Song browser
* Very fast startup and song loading
* 3D, "note highway" display
* Note detection
* Pitch shifting of songs that are slightly off of standard tuning
* Adjustable song playback speed

# Platforms Supported

ChartPlayer has been tested on Windows, Linux (x64 and arm64) and macOS.

# Downloading

You can download ChartPlayer from releases section [here](https://github.com/mikeoliphant/ChartPlayer/releases/latest).

# Running as a VST in Windows

To install, unpack the .zip file and copy the extracted folder to "C:\Program Files\Common Files\VST3".

# Running under Jack in Linux or MacOS

Unpack the .zip file and run "ChartPlayerJack" from the resulting folder (you will likely have to make it executable:

```
chmod u+x ChartPlayerJack
```

You need to have a running [Jack](https://jackaudio.org/) instance for it to work. It will attempt to connect to your jack input/output ports.

For speed/pitch shifting to work, you need to have Rubber Band (librubberband2) installed.

For file browser input to work, you need to have [Zenity](https://help.gnome.org/users/zenity/stable/index.html.en) installed.

## MacOS Note

If you are running on an Arm Mac, and the Arm64 build does not work - try the x64 build with [Rosetta](https://en.wikipedia.org/wiki/Rosetta_(software)).

# Where to get songs

ChartPlayer plays songs in tne [OpenSongChart](https://github.com/mikeoliphant/OpenSongChart) format. There is a [ChartConverter tool for converting songs from other formats](https://github.com/mikeoliphant/ChartConverter).

# Building From Source

Make sure you clone this github repo recursively:

```bash
git clone --recurse-submodules https://github.com/mikeoliphant/ChartPlayer
```

Building should be straightforward using Visual Studio.

**NOTE:** Build and run the "ImageProcessor" project first - it creates texture assets that are required for the main build.

