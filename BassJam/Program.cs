using System;
using AudioPlugSharpHost;

namespace BassJam
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            WindowsFormsHost<BassJamPlugin> host = new WindowsFormsHost<BassJamPlugin>(new BassJamPlugin());

            host.Run();
        }
    }
}