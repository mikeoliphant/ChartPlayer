using System;

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