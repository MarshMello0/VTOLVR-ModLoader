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

        private List<ModItem> unloadedMods;
        private List<ModItem> loadedMods = new List<ModItem>();

        public MainWindow()
        {
            InitializeComponent();
            root = Directory.GetCurrentDirectory();
            CheckFolder();

            FindMods();
        }

        private void CheckFolder()
        {
            if (!Directory.Exists(root + modsFolder))
            {
                Directory.CreateDirectory(root + modsFolder);
            }
        }

        private void FindMods()
        {
            DirectoryInfo folder = new DirectoryInfo(root + modsFolder);
            FileInfo[] files = folder.GetFiles("*.dll");
            unloadedMods = new List<ModItem>(files.Length);

            foreach (FileInfo file in files)
            {
                unloadedMods.Add(new ModItem(file.Name));
            }
            UpdateLists();
        }

        private void AddMod(string name)
        {
            ModItem mod = unloadedMods.Find(x => x.Title == name);
            loadedMods.Add(mod);
            unloadedMods.Remove(mod);
            UpdateLists();
        }

        private void RemoveMod(string name)
        {
            ModItem mod = loadedMods.Find(x => x.Title == name);
            unloadedMods.Add(mod);
            loadedMods.Remove(mod);
            UpdateLists();
        }

        private void UpdateLists()
        {
            UnloadedBox.ItemsSource = unloadedMods;
            LoadedBox.ItemsSource = loadedMods;
            UnloadedBox.Items.Refresh();
            LoadedBox.Items.Refresh();
        }


        private void OpenGame(object sender, RoutedEventArgs e)
        {
            Process.Start("steam://run/667970");
        }

        private void InjectButton(object sender, RoutedEventArgs e)
        {
            foreach (ModItem mod in loadedMods)
            {
                try
                {
                    string start = string.Format(" -t {0} -d {1} -n {2} -c {3} -m {4}", "VTOLVR.exe", @"mods\" + mod.Title, mod.Title.ToString().Split('.')[0], "Load", "Init");
                    Process.Start(root + injector, start);
                }
                catch
                {
                }
            }
        }

        private void LoadMod(object sender, RoutedEventArgs e)
        {
            string mod = ((Button)sender).DataContext.ToString();
            AddMod(mod);

        }

        private void UnloadMod(object sender, RoutedEventArgs e)
        {
            string mod = ((Button)sender).DataContext.ToString();
            RemoveMod(mod);
        }
    }

    public class ModItem
    {
        public string Title { get; set; }

        public ModItem (string Title)
        {
            this.Title = Title;
        }
    }
}
