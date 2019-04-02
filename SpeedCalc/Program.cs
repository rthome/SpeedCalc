using System;
using System.IO;
using System.Runtime;

using SpeedCalc.Frontend.Views;

using Splat;

namespace SpeedCalc
{
    public class Program
    {
        static void InitializePGO()
        {
            try
            {
                Directory.CreateDirectory(SpeedCalcApp.UserDataDirectory);
                ProfileOptimization.SetProfileRoot(SpeedCalcApp.UserDataDirectory);
                ProfileOptimization.StartProfile("startup_profile");
            }
            catch (Exception) { }
        }

        [STAThread]
        public static int Main()
        {
            InitializePGO();

            var app = new SpeedCalcApp();

            var shellView = Locator.Current.GetService<ShellView>();
            return app.Run(shellView);
        }
    }
}
