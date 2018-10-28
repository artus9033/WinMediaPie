using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace WinMediaPie
{
	public class AutorunManager
    {

		private const string id = "WinMediaPie Autorun";

		public static bool IsAlreadyAddedToCurrentUserStartup() {
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
			{
				return key.GetValue(id) != null ? true : false;
			}
		}

        public static bool IsAlreadyAddedToAllUsersStartup ()
		{
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
			{
				return key.GetValue(id) != null ? true : false;
			}
		}

		public static void AddToCurrentUserAutorun()
		{
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
			{
				key.SetValue(id, "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
			}
		}

		public static void AddToAllUsersAutorun()
		{
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
			{
				key.SetValue(id, "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
			}
		}

		public static void RemoveFromCurrentUserAutorun()
		{
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
			{
				key.DeleteValue(id, false);
			}
		}

		public static void RemoveFromAllUsersAutorun()
		{
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
			{
				key.DeleteValue(id, false);
			}
		}

		public static bool IsAdmin()
		{
			bool isAdmin;
			try
			{
				WindowsIdentity user = WindowsIdentity.GetCurrent();
				WindowsPrincipal principal = new WindowsPrincipal(user);
				isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
			}
			catch (UnauthorizedAccessException)
			{
				isAdmin = false;
			}
            catch (StackOverflowException)
            {
                isAdmin = false;
            }
			catch (Exception)
			{
				isAdmin = false;
			}
			return isAdmin;
		}
	}
}
