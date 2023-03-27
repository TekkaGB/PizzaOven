using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using xdelta3.net;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;

namespace PizzaOven
{
    public static class Setup
    {
        public static string GetMD5Checksum(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }
        public static bool GameSetup()
        {
            string defaultPath = String.Empty;
            try
            {
                var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2231450");
                if (key != null)
                    if (!String.IsNullOrEmpty(key.GetValue("InstallLocation") as string))
                        defaultPath = $"{key.GetValue("InstallLocation") as string}{Global.s}PizzaTower.exe";
            }
            catch (Exception e)
            {
            }
            if (!File.Exists(defaultPath))
            {
                Global.logger.WriteLine($"Couldn't find install path in registry, select path to exe instead", LoggerType.Warning);
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.DefaultExt = ".exe";
                dialog.Filter = $"Executable File (PizzaTower.exe)|PizzaTower.exe";
                dialog.Title = $"Select PizzaTower.exe from your Steam Install folder";
                dialog.Multiselect = false;
                dialog.InitialDirectory = Global.assemblyLocation;
                dialog.ShowDialog();
                if (!String.IsNullOrEmpty(dialog.FileName)
                    && Path.GetFileName(dialog.FileName).Equals("PizzaTower.exe", StringComparison.InvariantCultureIgnoreCase))
                    defaultPath = dialog.FileName;
                else if (!String.IsNullOrEmpty(dialog.FileName))
                {
                    Global.logger.WriteLine($"PizzaTower.exe not found", LoggerType.Error);
                    return false;
                }
                else
                    return false;
            }
            Global.config.ModsFolder = Path.GetDirectoryName(defaultPath);
            Global.config.Launcher = defaultPath;
            Global.UpdateConfig();
            Global.logger.WriteLine($"Setup completed for Pizza Tower!", LoggerType.Info);
            return true;
        }
    }
}
