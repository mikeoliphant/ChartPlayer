using System;
using AudioPlugSharpHost;

namespace ChartPlayer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            WindowsFormsHost<ChartPlayerPlugin> host = new WindowsFormsHost<ChartPlayerPlugin>(new ChartPlayerPlugin());

            host.Run();
        }
    }
}