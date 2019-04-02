using System;
using System.IO;
using System.Windows;

using ReactiveUI;

using SpeedCalc.Frontend.ViewModels;
using SpeedCalc.Frontend.Views;

using Splat;

namespace SpeedCalc
{
    public class SpeedCalcApp : Application
    {
        public static string UserDataDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpeedCalcApp");

        static void InitializeIoC()
        {
            Locator.CurrentMutable.InitializeSplat();
            Locator.CurrentMutable.InitializeReactiveUI();

            Locator.CurrentMutable.Register(() => new ShellViewModel());
            Locator.CurrentMutable.Register(() => new ShellView(Locator.Current.GetService<ShellViewModel>()));
        }

        public SpeedCalcApp()
        {
            ShutdownMode = ShutdownMode.OnLastWindowClose;

            InitializeIoC();
        }
    }
}
