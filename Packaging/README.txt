ChartPlayer

Copyright (c) 2024 Mike Oliphant

Source code and license information can be found here:

https://github.com/mikeoliphant/ChartPlayer

This package has ChartPlayer as an application and as a plugin for DAWs (Digital Audio Workstations):

- ChartPlayer: the application. Run ChartPlayer (ChartPlayer.exe on Windows) in that folder. On Linux, you may have
  to make it executable first: chmod u+x ChartPlayer
- clap and vst3: the plugin, in CLAP and VST3 formats. For your DAW to see it, copy the ChartPlayerPlugin folder to
  your CLAP folder, or ChartPlayerPlugin.vst3 to your VST3 folder:
  - Windows: "C:\Program Files\Common Files\CLAP" and "C:\Program Files\Common Files\VST3"
  - Linux: ~/.clap and ~/.vst3

The plugin needs .NET 10 installed (the .NET Desktop Runtime on Windows, the .NET Runtime on Linux):

https://dotnet.microsoft.com/en-us/download/dotnet/10.0

On Linux, speed and pitch shifting need Rubber Band (librubberband2) installed.

If you are still having trouble getting the plugin to work, please check out troubleshooting instructions here:

https://github.com/mikeoliphant/ChartPlayer/blob/master/Troubleshooting.md
