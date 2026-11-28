using System;
using AudioPlugSharpStandalone;

namespace ChartPlayer
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            StandaloneHost.ForPlugin<ChartPlayerPlugin>().Run();
        }
    }
}
