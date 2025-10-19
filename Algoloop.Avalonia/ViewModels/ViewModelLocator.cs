using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Algoloop.Wpf.ViewModels
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            var services = new ServiceCollection();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<MarketsViewModel>();
            services.AddSingleton<StrategiesViewModel>();
            services.AddSingleton<ResearchViewModel>();
            services.AddSingleton<LogViewModel>();
            services.AddSingleton<MainViewModel>();
            Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        }

        public static MainViewModel? MainViewModel =>
            Ioc.Default.GetService<MainViewModel>();
        public static MarketsViewModel? MarketsViewModel =>
            Ioc.Default.GetService<MarketsViewModel>();
        public static StrategiesViewModel? StrategiesViewModel =>
            Ioc.Default.GetService<StrategiesViewModel>();
        public static ResearchViewModel? ResearchViewModel =>
            Ioc.Default.GetService<ResearchViewModel>();
        public static LogViewModel? LogViewModel =>
            Ioc.Default.GetService<LogViewModel>();
        public static SettingsViewModel? SettingsViewModel =>
            Ioc.Default.GetService<SettingsViewModel>();
    }
}
