using Squirrel;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace Common.Helpers
{
    public static class WindowHelpers
    {
        public static Screen CurrentScreen(this Window window)
        {
            return Screen.FromPoint(new System.Drawing.Point((int)window.Left, (int)window.Top));
        }
    }
}

namespace WinMediaPie
{
    /// <summary>
    /// Logika interakcji dla klasy App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var assembly = Assembly.GetEntryAssembly();

            var updateDotExe = Path.Combine(Path.GetDirectoryName(assembly.Location), "..", "Update.exe");

            var isSquirrelInstallation = File.Exists(updateDotExe);

            if (isSquirrelInstallation)
            {
                var updateTask = new Task(async () =>
                {
                    var mgr = await UpdateManager.GitHubUpdateManager(Constants.GITHUB_RELEASES_URL);
                    Console.Out.WriteLine($"Checking for updates at {Constants.GITHUB_RELEASES_URL}...");

                    var result = await mgr.CheckForUpdate();

                    if (result.ReleasesToApply.Count > 0)
                    {
                        Console.Out.WriteLine($"There are {result.ReleasesToApply.Count} update(s) to be downloaded. WinMediaPie will be upgraded to version {result.FutureReleaseEntry.Version}.");

                        Toaster.Toast($"Updating WinMediaPie to version {result.FutureReleaseEntry.Version} in background");
                    }
                    else
                    {
                        Console.Out.WriteLine($"There are no new updates. Version {result.CurrentlyInstalledVersion.Version} is the latest available");
                    }

                    await mgr.UpdateApp();
                });

                updateTask.ContinueWith(t =>
                {
                    AggregateException exception = t.Exception;

                    Console.Out.WriteLine("Error checking for updates: ", exception);
                }, TaskContinuationOptions.OnlyOnFaulted);

                updateTask.Start();
            }
            else
            {
                Console.Out.WriteLine("Not a Squirrel setup based installation, automatic updates are unavailable.");
            }
        }
    }

    public class MouseWheelBehavior
    {
        public static double GetValue(Slider slider)
        {
            return (double)slider.GetValue(ValueProperty);
        }

        public static void SetValue(Slider slider, double value)
        {
            slider.SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.RegisterAttached(
            "Value",
            typeof(double),
            typeof(MouseWheelBehavior),
            new UIPropertyMetadata(0.0, OnValueChanged));

        public static Slider GetSlider(UIElement parentElement)
        {
            return (Slider)parentElement.GetValue(SliderProperty);
        }

        public static void SetSlider(UIElement parentElement, Slider value)
        {
            parentElement.SetValue(SliderProperty, value);
        }

        public static readonly DependencyProperty SliderProperty =
            DependencyProperty.RegisterAttached(
            "Slider",
            typeof(Slider),
            typeof(MouseWheelBehavior));


        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Slider slider = d as Slider;
            slider.Loaded += (ss, ee) =>
            {
                Window window = Window.GetWindow(slider);
                if (window != null)
                {
                    SetSlider(window, slider);
                    window.PreviewMouseWheel += Window_PreviewMouseWheel;
                }
            };
            slider.Unloaded += (ss, ee) =>
            {
                Window window = Window.GetWindow(slider);
                if (window != null)
                {
                    SetSlider(window, null);
                    window.PreviewMouseWheel -= Window_PreviewMouseWheel;
                }
            };
        }

        private static void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Window window = sender as Window;
            Slider slider = GetSlider(window);
            double value = GetValue(slider);
            if (slider != null && value != 0)
            {
                slider.Value += slider.SmallChange * e.Delta / value;
            }
        }
    }
}
