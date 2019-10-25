using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using System.IO.Compression;
using Path = System.IO.Path;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Moving Window
        private bool holdingDown;
        private Point lm = new Point();

        //Pages
        public enum Page{ About, SelectFolder, Confirm, Extracting,Finished,Error}
        private Page currentPage;

        //
        private string vtFolder;
        public MainWindow()
        {
            InitializeComponent();
            SwitchPage();
        }
        
        private void Window_Initialized(object sender, EventArgs e)
        {
            
        }

        private string FindVTOL()
        {
            string regPath = (string)Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam",
                @"InstallPath",
                @"NULL");

            string gameFolder = File.ReadAllText(regPath + @"\steamapps\libraryfolders.vdf").Split('"')[13];
            string[] split = gameFolder.Split('\\');
            string result = "";
            for (int i = 0; i < split.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(split[i]))
                    result += split[i] + @"\";
            }
            result += @"steamapps\common\VTOL VR\";
            return result;
            
        }
        private void InstallFiles()
        {
            if (Directory.Exists(vtFolder + @"VTOLVR_ModLoader"))
            {
                //It must be already installed
                SetProgress(100);
                currentPage++;
                SwitchPage();
                return;
            }
            SetProgress(0);
            
            try
            {
                if (File.Exists(vtFolder + @"ModLoader.zip"))
                    File.Delete(vtFolder + @"ModLoader.zip");

                //Extracting the zip from resources to files
                File.WriteAllBytes(vtFolder + "ModLoader.zip", Properties.Resources.ModLoader);

                //Stopping a possible error
                if (File.Exists(vtFolder + @"VTOLVR_Data\Plugins\discord-rpc.dll"))
                    File.Delete(vtFolder + @"VTOLVR_Data\Plugins\discord-rpc.dll");
                if (File.Exists(vtFolder + @"VTOLVR_Data\Managed\0Harmony.dll"))
                    File.Delete(vtFolder + @"VTOLVR_Data\Managed\0Harmony.dll");
                if (File.Exists(vtFolder + @"VTOLVR_Data\Managed\mscorlib.dll"))
                    File.Delete(vtFolder + @"VTOLVR_Data\Managed\mscorlib.dll");


                ZipFile.ExtractToDirectory(vtFolder + @"ModLoader.zip", vtFolder);
                SetProgress(50);
                File.Delete(vtFolder + @"ModLoader.zip");
                SetProgress(75);

                if (dShortcut.IsChecked == true)
                    CreateShortcut(
                        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\VTOL VR Mod Loader.lnk",
                        vtFolder + @"VTOLVR_ModLoader\VTOLVR-ModLoader.exe");
                if (smShortcut.IsChecked == true)
                    CreateShortcut(
                        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\VTOL VR Mod Loader.lnk",
                        vtFolder + @"VTOLVR_ModLoader\VTOLVR-ModLoader.exe");
            }
            catch (Exception e)
            {
                errorTextBox.Text = e.ToString();
                currentPage = Page.Error;
                backButton.Visibility = Visibility.Hidden;
                nextButotn.Visibility = Visibility.Hidden;
                cancelButton.Content = "Close";
                SwitchPage();
                return;
            }
            SetProgress(100);
            currentPage++;
            SwitchPage();

        }
        private void CreateShortcut(string shortcutPath, string targetPath)
        {
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.Description = "Open VTOL VR with mods";
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = vtFolder + @"VTOLVR_ModLoader\";
            shortcut.Save();
        }

        private void SwitchPage()
        {
            aboutPage.Visibility = Visibility.Hidden;
            folderPage.Visibility = Visibility.Hidden;
            confirmPage.Visibility = Visibility.Hidden;
            extractingPage.Visibility = Visibility.Hidden;
            finishedPage.Visibility = Visibility.Hidden;
            errorPage.Visibility = Visibility.Hidden;
            switch (currentPage)
            {
                case Page.About:
                    aboutPage.Visibility = Visibility.Visible;
                    break;
                case Page.SelectFolder:
                    if (string.IsNullOrEmpty(vtFolder))
                        vtFolder = FindVTOL();
                    folderBox.Text = vtFolder;
                    folderPage.Visibility = Visibility.Visible;
                    break;
                case Page.Confirm:
                    confirmPage.Visibility = Visibility.Visible;
                    break;
                case Page.Extracting:
                    extractingPage.Visibility = Visibility.Visible;
                    InstallFiles();
                    break;
                case Page.Finished:
                    finishedPage.Visibility = Visibility.Visible;
                    cancelButton.Visibility = Visibility.Hidden;
                    backButton.Content = "Launch";
                    nextButotn.Content = "Close";
                    break;
                case Page.Error:
                    errorPage.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Quit();
        }
        private void Quit()
        {
            Process.GetCurrentProcess().Kill();
        }

        private void NextButotn_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage == Page.Finished)
                Quit();
            if (currentPage == Page.Extracting)
                return;
            currentPage++;
            SwitchPage();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage <= 0)
                return;
            if (currentPage == Page.Finished)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(vtFolder + @"VTOLVR_ModLoader\VTOLVR-ModLoader.exe");
                startInfo.WorkingDirectory = vtFolder + @"VTOLVR_ModLoader\";
                Process.Start(startInfo);
                Quit();
            }
            currentPage--;
            SwitchPage();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileBrowser();
        }
        private void SetProgress(float barValue)
        {
            progressBar.Value = barValue;
        }

        private void OpenFileBrowser()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            openFileDialog.Filter = "exe files (*.exe)|*.exe";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Title = "Open the VTOLVR.exe";
            openFileDialog.FileName = "VTOLVR.exe";

            if (openFileDialog.ShowDialog() == true)
            {
                if (openFileDialog.FileName.Contains("VTOLVR.exe"))
                {
                    vtFolder = openFileDialog.FileName.Replace("VTOLVR.exe", "");
                    folderBox.Text = vtFolder;
                }
                else
                {
                    MessageBox.Show("Couldn't find VTOLVR.exe, please try again.");
                    OpenFileBrowser();
                }
            }
        }

        #region Moving Window
        private void TopBarDown(object sender, MouseButtonEventArgs e)
        {
            holdingDown = true;
            lm = Mouse.GetPosition(Application.Current.MainWindow);
        }

        private void TopBarUp(object sender, MouseButtonEventArgs e)
        {
            holdingDown = false;
        }

        private void TopBarMove(object sender, MouseEventArgs e)
        {
            if (holdingDown)
            {
                this.Left += Mouse.GetPosition(Application.Current.MainWindow).X - lm.X;
                this.Top += Mouse.GetPosition(Application.Current.MainWindow).Y - lm.Y;
            }
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {

        }

        private void TopBarLeave(object sender, MouseEventArgs e)
        {
            holdingDown = false;
        }

        #endregion

        
    }
}
