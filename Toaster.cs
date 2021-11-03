using System;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace WinMediaPie
{
    class Toaster
    {
        /// <summary>
        /// Shows a Windows 10 ModernUI toast notification
        /// </summary>
        /// <param name="text">The content of the toast</param>
        public static void Toast(string text = "")
        {
            try
            {
                XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText03);

                XmlNodeList stringElements = toastXml.GetElementsByTagName("text");

                stringElements[0].AppendChild(toastXml.CreateTextNode(text));

                XmlNodeList imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = Constants.AppPaths.iconPngPath;

                ToastNotification toast = new ToastNotification(toastXml);

                ToastNotificationManager.CreateToastNotifier(Constants.APP_ID).Show(toast);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Błąd podczas pokazywania powiadomienia: {e.ToString()}");
            }
        }
    }
}
