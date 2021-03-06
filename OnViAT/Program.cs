using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using OnViAT.LibVLC;
using OnViAT.Views;
namespace OnViAT
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp().Start(AppMain, args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                //.UseDirect2D1()
                .UseReactiveUI()
                //.With(new AvaloniaNativePlatformOptions() { UseDeferredRendering = false })
                //.With(new Win32PlatformOptions() { UseDeferredRendering = false }) //with defered rendering false look like it's working slightly better
                .UseVLCSharp() //by default vlc rendering
                //.UseVLCSharp(renderingOptions: LibVLCAvaloniaRenderingOptions.AvaloniaCustomDrawingOperation)
                .LogToDebug();

        // Your application's entry point. Here you can initialize your MVVM framework, DI
        // container, etc.
        private static void AppMain(Application app, string[] args)
        {
            var window = new MainWindow
                {
                    //DataContext = new MainViewModel(),
                };
                app.Run(window);
        }
    }
}