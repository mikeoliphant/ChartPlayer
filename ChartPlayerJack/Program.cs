using System;
using System.Threading;
using AudioPlugSharp;
using AudioPlugSharpJack;

namespace ChartPlayer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            JackHost<ChartPlayerPlugin> host = new JackHost<ChartPlayerPlugin>(new ChartPlayerPlugin());

            host.Run();
        }
    }
}