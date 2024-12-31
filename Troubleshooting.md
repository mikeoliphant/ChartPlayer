# Troubleshooting

## Installing

Make sure that the ChartPlayer folder is installed where your DAW looks for VST3 plugins. Normally this is "**C:\Program Files\Common Files\VST3**".

Also ensure that you have .NET 8.0 installed on your system. You can get it here:

https://dotnet.microsoft.com/en-us/download/dotnet/8.0

## ChartPlayer Plugin Doesn't Scan in DAW

If your DAW isn't loading the plugin, first make sure that you had it re-scan plugins after installing .net 8.0

If you are still having trouble, please check for log files here:

"**%appdata%\AudioPlugSharp**"

Error messages here can help track down the problem.

## DAW Configuration

You will probably want two tracks in your DAW - one for ChartPlayer, and one for whatever plugin you want to use for your instrument effects.

ChartPlayer needs to be sent input from your instrument audio channel (or midi channel for drums/keys) if you want it to do detection.

**NOTE** Make sure that the ChartPlayer track is armed for input even if you aren't doing note detection, as some DAWs will not run it properly in real-time sync if you do not.

## Reporting Problems

If you are having trouble, please get in touch. The best way is through opening an issue or a discussion here on GitHub.
