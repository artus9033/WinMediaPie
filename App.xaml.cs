using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
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
