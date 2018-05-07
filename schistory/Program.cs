using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace schistory
{
    static class Program
    {
        private static string progName = "schistory";
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            if (args.Length > 0 && args[0] == "-i")
            {
                Setup();
                return;
            }
            if (args.Length > 0 && args[0] == "-a")
            {
                ProcessStartedByAdmin();
                Environment.Exit(0);
            }

            Application.Run(new Form1(args));
        }
        private static void ProcessStartedByAdmin(string arguments = null)
        {
            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);
            if (hasAdministrativeRight == false)
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(); //создаем новый процесс
                processInfo.Verb = "runas"; //в данном случае указываем, что процесс должен быть запущен с правами администратора
                processInfo.FileName = Assembly.GetExecutingAssembly().Location; //указываем исполняемый файл (программу) для запуска
                processInfo.Arguments = arguments;
                try
                {
                    Process.Start(processInfo); //пытаемся запустить процесс
                    Environment.Exit(0);
                }
                catch { }
            }
        }
        private static void Setup()
        {
            try
            {
                // Создаем папку в Program Files
                string ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string myPath = ProgramFiles + "\\" + "scorpclub" + "\\" + progName + "\\";

                // Копируем себя в свою рабочую папку
                string myLocation = Assembly.GetExecutingAssembly().Location;
                string myExeName = myLocation.Substring(myLocation.LastIndexOf("\\") + 1);
                Directory.CreateDirectory(myPath);
                File.Copy(myLocation, myPath + myExeName, true);

                // Копируем свои настройки и базу в свою рабочую папку
                if (File.Exists("settings.data"))
                {
                    File.Copy("settings.data", myPath + "settings.data", true);
                }
                if (File.Exists("nicknameuid.data"))
                {
                    File.Copy("nicknameuid.data", myPath + "nicknameuid.data", true);
                }
                if (File.Exists("schistory.data"))
                {
                    File.Copy("schistory.data", myPath + "schistory.data", true);
                }
                if (Directory.Exists("modules"))
                {
                    Directory.CreateDirectory(myPath + "modules");
                    foreach (string item in Directory.EnumerateFiles("modules"))
                    {
                        File.Copy(item, myPath + item);
                    }
                }

                // Создадим ярлык где была наша прога
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                string shortcutPath = myLocation.Remove(myLocation.LastIndexOf('.')) + ".lnk";
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = myPath + myExeName;
                shortcut.Arguments = "-a";
                shortcut.Save();

                // Удаление удаление модулей
                Directory.Delete("modules", true);

                // Самоудаление из прошлого места
                ProcessStartInfo startInfo2 = new ProcessStartInfo
                {
                    FileName = "cmd.exe",  // Путь к приложению
                    Arguments = "/C ping -n 1 -w 1000 192.168.254.254> NUL & " + "ERASE /Q " + '"' + myLocation + '"' +
                    " & ERASE /Q " + '"' + myLocation.Remove(myLocation.LastIndexOf("\\") + 1) + "schistory.log" + '"' +
                    " & ERASE /Q " + '"' + myLocation.Remove(myLocation.LastIndexOf("\\") + 1) + "schistory.old.log" + '"' +
                    " & ERASE /Q " + '"' + myLocation.Remove(myLocation.LastIndexOf("\\") + 1) + "settings.data" + '"' +
                    " & ERASE /Q " + '"' + myLocation.Remove(myLocation.LastIndexOf("\\") + 1) + "nicknameuid.data" + '"' +
                    " & ERASE /Q " + '"' + myLocation.Remove(myLocation.LastIndexOf("\\") + 1) + "schistory.data" + '"' +
                    " & ERASE /Q " + '"' + myLocation.Remove(myLocation.LastIndexOf("\\") + 1) + "Newtonsoft.Json.dll" + '"',
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(startInfo2);

                // Перезапускаем себя из своей папки
                Process.Start(myPath + myExeName);
                Environment.Exit(0);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
                return;
            }
        }
    }
}
