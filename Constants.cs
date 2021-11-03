using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinMediaPie
{
    class Constants
    {
        public static string APP_ID = "WinMediaPie";
        public static string GITHUB_RELEASES_URL = "https://github.com/artus9033/WinMediaPie";

        /// <summary>
        /// Stores paths to icon assets
        /// </summary>
        public static class AppPaths
        {
            public static string appTmpPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), APP_ID);
            public static string iconPngPath = System.IO.Path.Combine(AppPaths.appTmpPath, "icon.png");
            public static string iconIcoPath = System.IO.Path.Combine(AppPaths.appTmpPath, "icon.ico");
        }
    }
}
