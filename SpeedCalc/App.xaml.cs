using Autofac;

using ReactiveUI;

using SpeedCalc.ViewModels;
using SpeedCalc.Views;

using Splat;
using Splat.Autofac;

using System;
using System.Windows;

namespace SpeedCalc
{
    public partial class App : Application
    {
        void ConfigureIoC()
        {
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyTypes(GetType().Assembly)
                .PublicOnly()
                .AsClosedTypesOf(typeof(IViewFor<>))
                .AsImplementedInterfaces()
                .AsSelf();
            builder.RegisterAssemblyTypes(GetType().Assembly)
                .PublicOnly()
                .Where(type => type.Name.EndsWith("ViewModel", StringComparison.InvariantCultureIgnoreCase))
                .AsSelf();

            // Workaround for https://github.com/reactiveui/splat/issues/645
            var resolver = builder.UseAutofacDependencyResolver();
            resolver.InitializeSplat();
            resolver.InitializeReactiveUI();
            builder.RegisterInstance(resolver);

            var container = builder.Build();
            resolver.SetLifetimeScope(container);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ConfigureIoC();

            var mainView = (CalculatorView)Locator.Current.GetService<IViewFor<CalculatorViewModel>>();
            mainView.Show();
        }
    }
}
