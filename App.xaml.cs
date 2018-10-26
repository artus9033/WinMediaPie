using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

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
}
