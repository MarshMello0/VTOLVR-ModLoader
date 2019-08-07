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
using WpfAnimatedGif;
using System.Net;
using System.Xml.Serialization;

namespace VTOLVR_ModLoader
{
    public partial class MainWindow : Window
    {
        private enum gifStates { Paused, Play, Frame }

        private static float buildNumber = 2.0f; //This will be used for checking for updates
        private static string modsFolder = @"\mods";
        private static string injector = @"\injector.exe";
        private static string updatefile = @"\updates.xml";
        private static string url = @"https://vtolvr-mods.com";
        private string root;
        private bool continueDots = true;
        private string continueText = "Launching Game";

        private string[] needFiles = new string[] { "SharpMonoInjector.dll", "injector.exe", "ModLoader.dll", "WpfAnimatedGif.dll", "modloader.assets" };
        private string[] neededDLLFiles = new string[] { @"\Plugins\discord-rpc.dll" };

        private Updates updates;
        public MainWindow()
        {
            InitializeComponent();
            root = Directory.GetCurrentDirectory();
            CheckFolder();
            CheckForUpdates();
        }
        private void CheckFolder()
        {
            //Checking the folder which this is in
            string[] pathSplit = root.Split('\\');
            if (pathSplit[pathSplit.Length - 1] != "VTOLVR_ModLoader")
            {
                MessageBox.Show("It seems I am not in the folder \"VTOLVR_ModLoader\", place make sure I am in there other wise the in game menu won't load","Wrong Folder");
                Quit();
            }

            //Checking if the files we need to run are there
            foreach (string file in needFiles)
            {
                if (!File.Exists(root + @"\" + file))
                {
                    WrongFolder(file);
                    return;
                }
            }

            //Checking if the mods folder is there
            if (!Directory.Exists(root + modsFolder))
            {
                Directory.CreateDirectory(root + modsFolder);
            }

            //Checking the Managed Folder
            foreach (string file in neededDLLFiles)
            {
                if (!File.Exists(Directory.GetParent(Directory.GetCurrentDirectory()).FullName + @"\VTOLVR_Data" + file))
                {
                    MissingManagedFile(file);
                }
            }
        }
        private void WrongFolder(string file)
        {
            MessageBox.Show("I can't seem to find " + file + " in my folder. Make sure you place me in the same folder as this file.", "Missing File");
            Quit();
        }
        private void MissingManagedFile(string file)
        {
            MessageBox.Show("I can't seem to find " + file + " in VTOL VR > VTOLVR_Data, please make sure this file is here otherwise the mod loader won't work", "Missing File");
            Quit();
        }
        private void CheckForUpdates()
        {
            if (CheckForInternet())
            {
                if (!LoadUpdatesFile())
                    updates = new Updates(2, 2, 2);

                using (var client = new WebClient())
                {
                    string result = "";
                    try
                    {
                        result = client.DownloadString(url + "/update");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("It seems we can't load " + url + "\nMaybe its down?\n\n" + e.Message, "Server Down");
                        return;
                    }

                    try
                    {
                        string[] split = result.Split('|');

                        int exeversion = int.Parse(split[0]);
                        int dllversion = int.Parse(split[1]);
                        int assetsversion = int.Parse(split[3]);

                        if (exeversion > updates.exeversion)
                            UpdateExe();
                        if (assetsversion > updates.assetversion)
                            UpdateAssets(split[4], assetsversion);
                        if (dllversion > updates.dllversion)
                            UpdateDLL(split[2], dllversion);


                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("There was a strange error\n" + e.Message + "\nAuto updating seems to be broken.", "Super Strange Error");
                        return;
                    }



                    SaveUpdatesFile();
                }
            }
        }
        private bool CheckForInternet()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (client.OpenRead("http://clients3.google.com/generate_204"))
                    {
                        return true;
                    }
                }
                
            }
            catch
            {
                return false;
            }
        }
        private bool LoadUpdatesFile()
        {
            if (File.Exists(root + updatefile))
            {
                using (FileStream stream = new FileStream(root + updatefile, FileMode.Open))
                {
                    XmlSerializer xml = new XmlSerializer(typeof(Updates));
                    Updates deserialized = (Updates)xml.Deserialize(stream);
                    updates = new Updates(deserialized.exeversion, deserialized.dllversion, deserialized.assetversion);
                    return true;
                }
            }
            else
                return false;
        }
        private void UpdateDLL(string url, int newVersion)
        {
            using (var client = new WebClient())
            {
                if (File.Exists(root + @"\ModLoader.dll"))
                    File.Delete(root + @"\ModLoader.dll");
                client.DownloadFile(MainWindow.url + url, @"ModLoader.dll");
            }
            updates.dllversion = newVersion;
        }
        private void UpdateAssets(string url, int newVersion)
        {
            using (var client = new WebClient())
            {
                if (File.Exists(root + @"\modloader.assets"))
                    File.Delete(root + @"\modloader.assets");
                client.DownloadFile(MainWindow.url + url, @"modloader.assets");
            }
            updates.assetversion = newVersion;
        }
        private void UpdateExe()
        {
            MessageBox.Show("There is an update to the launcher\nPlease head over to " + url + " to download the latest version.","Launcher Update!");
            Quit();
        }
        private void SaveUpdatesFile()
        {
            using (FileStream stream = new FileStream(root + updatefile, FileMode.Create))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Updates));
                xml.Serialize(stream, updates);
            }
        }
        private void OpenGame(object sender, RoutedEventArgs e)
        {
            //Changing UI
            launchButton.Visibility = Visibility.Hidden;
            loadingText.Visibility = Visibility.Visible;
            LoadingDots();
            GifState(gifStates.Play);

            //Launching the game
            Process.Start("steam://run/667970");

            //Searching For Process
            WaitForProcess();

        }
        private async void LoadingDots()
        {
            //This will constanly loop, but we will just change the text before it when we need to change it
            int delay = 500;
            loadingText.Content = continueText + ".";
            await Task.Delay(delay);
            loadingText.Content = continueText + "..";
            await Task.Delay(delay);
            loadingText.Content = continueText + "...";
            await Task.Delay(delay);
            if (continueDots)
                LoadingDots();
        }
        private void GifState(gifStates state, int frame = 0)
        {
            //Changing the gif's state
            var controller = ImageBehavior.GetAnimationController(LogoGif);
            switch (state)
            {
                case gifStates.Paused:
                    controller.Pause();
                    break;
                case gifStates.Play:
                    controller.Play();
                    break;
                case gifStates.Frame:
                    controller.GotoFrame(frame);
                    break;
            }
        }
        private async void WaitForProcess()
        {
            int maxTries = 5;
            for (int i = 1; i <= maxTries; i++)
            {
                //Doing 5 tries to search for the process
                continueText = "Searching for Process";
                await Task.Delay(5000);

                if (Process.GetProcessesByName("vtolvr").Length == 1)
                {
                    break;
                }

                if (i == maxTries)
                {
                    //If we couldn't find it, go back to how it was at the start
                    GifState(gifStates.Paused);
                    continueText = "Launching Game";
                    launchButton.Visibility = Visibility.Visible;
                    loadingText.Visibility = Visibility.Hidden;
                    MessageBox.Show("Couldn't Find VTOL VR Process");
                    return;
                }
            }

            //A delay just to make sure the game has fully launched,
            continueText = "Waiting for Game";
            await Task.Delay(10000);

            //Injecting Default Mod
            continueText = "Injecting Mod Loader";
            InjectDefaultMod();
            //Closing Exe
            Quit();
        }
        private void InjectDefaultMod()
        {
            //Injecting the default mod
            string defaultStart = string.Format("inject -p {0} -a {1} -n {2} -c {3} -m {4}", "vtolvr", "ModLoader.dll", "ModLoader", "Load", "Init");
            Process.Start(root + injector, defaultStart);
        }
        private void Quit()
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}
