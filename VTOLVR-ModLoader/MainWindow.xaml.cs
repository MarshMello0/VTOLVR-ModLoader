using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace VTOLVR_ModLoader
{
    public partial class MainWindow : Window
    {
        private static string modsFolder = @"\mods";
        private static string injector = @"\MonoInjector64.exe";
        private string root;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Start()
        {
            root = Directory.GetCurrentDirectory();
            CheckFolder();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Start();
            Task task = LaunchGame();
        }

        private void CheckFolder()
        {
            if (!Directory.Exists(root + modsFolder))
            {
                Directory.CreateDirectory(root + modsFolder);
            }
        }
        private async Task LaunchGame()
        {
            Process[] processesByName = Process.GetProcessesByName("VTOLVR");
            if (processesByName.Length != 0)
            {
                LoadMods();
            }
            else
            {
                Process.Start("steam://run/667970");
                int count = 0;
                while (Process.GetProcessesByName("VTOLVR").Length == 0)
                {
                    count++;
                    await Task.Delay(1000);
                    if (count >= 10)
                    {
                        Process.GetCurrentProcess().Kill();
                        return;
                    }
                }
                await Task.Delay(40000);
                LoadMods();
            }
        }

        private void LoadMods()
        {
            DirectoryInfo folder = new DirectoryInfo(root + modsFolder);
            foreach (FileInfo file in folder.GetFiles("*.dll"))
            {
                try
                {
                    string start = string.Format(" -t {0} -d {1} -n {2} -c {3} -m {4}", "VTOLVR.exe", @"mods\" + file.Name, file.Name.ToString().Split('.')[0], "Load", "Init");
                    Process.Start(root + injector, start);
                }
                catch
                {
                }
            }

            Process.GetCurrentProcess().Kill();
        }

        private void InjectButton(object sender, RoutedEventArgs e)
        {
            Start();
            LoadMods();
        }
    }
}
