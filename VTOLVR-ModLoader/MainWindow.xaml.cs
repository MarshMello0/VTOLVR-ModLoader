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
using System.ComponentModel;
using Caliburn.Micro;

namespace VTOLVR_ModLoader
{
    public partial class MainWindow : Window
    {
        private enum gifStates { Paused, Play, Frame }

        private static string modsFolder = @"\mods";
        private static string injector = @"\injector.exe";
        private static string updatefile = @"\updates.xml";
        private static string updatesFeedFile = @"\feed.xml";
        private static string updatesFeed = @"/files/updatesfeed.xml";
        private static string url = @"http://localhost";
        private string root;

        private static int currentDLLVersion = 2;
        private static int currentAssetsVersion = 2;
        private static int currentEXEVersion = 2;

        //Startup
        private string[] needFiles = new string[] { "SharpMonoInjector.dll", "injector.exe" };
        private string[] neededDLLFiles = new string[] { @"\Plugins\discord-rpc.dll" };

        
        //Moving Window
        private bool holdingDown;
        private Point lm = new Point();
        private bool isBusy;

        //Updates 
        private Updates updates;
        private int assetsversion;
        private string assetsURL;

        private int dllNewVersion, assetsNewVersion;

        #region Startup
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Loaded(object sender, EventArgs e)
        {
            root = Directory.GetCurrentDirectory();
            CheckBaseFolder();
            GetUpdateFeed();
        }
        private void CheckBaseFolder()
        {
            //Checking the folder which this is in
            string[] pathSplit = root.Split('\\');
            if (pathSplit[pathSplit.Length - 1] != "VTOLVR_ModLoader")
            {
                MessageBox.Show("It seems I am not in the folder \"VTOLVR_ModLoader\", place make sure I am in there other wise the in game menu won't load", "Wrong Folder");
                Quit();
            }

            //Now it should be in the correct folder, but just need to check if its in the games folder
            string vtolexe = root.Replace("VTOLVR_ModLoader", "VTOLVR.exe");
            if (!File.Exists(vtolexe))
            {
                MessageBox.Show("It seems the VTOLVR_ModLoader folder isn't with the other games files\nPlease move me to VTOL VR's game root directory.", "Wrong Folder Location");
                Quit();
            }

            CheckFolder();
        }
        /// <summary>
        /// Checks for files which the Mod Loader needs to work such as .dll files
        /// </summary>
        private void CheckFolder()
        {
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
        #endregion

        #region Auto Updater
        private void GetUpdateFeed()
        {
            SetProgress(0, "Getting Update Feed...");
            SetPlayButton(true);
            if (CheckForInternet())
            {
                try
                {
                    if (File.Exists(root + updatesFeedFile))
                        File.Delete(root + updatesFeedFile);

                    WebClient client = new WebClient();
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(FeedProgress);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(FeedDone);
                    client.DownloadFileAsync(new Uri(url + updatesFeed), root + updatesFeedFile);
                }
                catch (Exception e)
                {
                    SetPlayButton(true);
                    SetProgress(100, "Failed to connect to server");
                    CheckForUpdates();
                }
                
            }
        }

        private void FeedDone(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                using (FileStream stream = new FileStream(root + updatesFeedFile, FileMode.Open))
                {
                    XmlSerializer xml = new XmlSerializer(typeof(UpdateFeed));
                    UpdateFeed deserialized = (UpdateFeed)xml.Deserialize(stream);
                    updateFeed.ItemsSource = deserialized.feed;
                }
                CheckForUpdates();
            }
            else
            {
                SetProgress(100, "Failed to connect to server.");
                Console.WriteLine("Failed getting feed \n" + e.Error.ToString());
                SetPlayButton(true);
                CheckForUpdates();
            }
        }

        private void FeedProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage / 100, "Downloading update feed...");
        }

        private void CheckForUpdates()
        {
            SetProgress(0, "Checking for updates...");
            SetPlayButton(true);
            if (CheckForInternet())
            {
                if (!LoadUpdatesFile())
                    updates = new Updates(currentEXEVersion, currentDLLVersion, currentAssetsVersion);

                using (var client = new WebClient())
                {
                    try
                    {
                        WebClient webClient = new WebClient();
                        webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadedUpdate);
                        webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadUpdateProgress);
                        webClient.DownloadStringAsync(new Uri(url + "/update.php"));
                    }
                    catch (Exception e)
                    {
                        SetProgress(100, "Unable to connect to the server");
                        SetPlayButton(false);
                        return;
                    }
                }
            }
        }
        private void DownloadUpdateProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(50 * (e.ProgressPercentage / 100), "Checking for updates...");
        }
        private void DownloadedUpdate(object sender, DownloadStringCompletedEventArgs e)
        {
            //When the download is done of the update.php this method runs
            if (!e.Cancelled && e.Error == null)
            {
                try
                {
                    string result = e.Result;
                    SetProgress(50, "Checking for updates...");
                    string[] split = result.Split('|');

                    int exeversion = int.Parse(split[0]);
                    int dllversion = int.Parse(split[1]);
                    assetsversion = int.Parse(split[3]);
                    assetsURL = split[4];
                    if (exeversion > updates.exeversion)
                        UpdateExe();
                    else
                    {
                        if (dllversion > updates.dllversion)
                        {
                            UpdateDLL(split[2], dllversion);
                            return;
                        }
                        else
                        {
                            if (assetsversion > updates.assetversion)
                            {
                                UpdateAssets(assetsURL, assetsversion);
                                return;
                            }
                        }
                    }
                }
                catch 
                {
                    SetProgress(100, "There was a strange error. Auto updating seems to be broken.");
                    SetPlayButton(false);
                    return;
                }

                SaveUpdatesFile();
                SetProgress(100, "Finished checking for updates");
                SetPlayButton(false);
            }
            else
            {
                //This is if it failed
                SetProgress(100, "Unable to connect to the server");
                SetPlayButton(false);
                return;
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
            try
            {
                if (File.Exists(root + @"\ModLoader.dll"))
                    File.Delete(root + @"\ModLoader.dll");

                WebClient client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DLLProgress);
                client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(DLLDone);
                client.DownloadFileAsync(new Uri(MainWindow.url + url), @"ModLoader.dll");
                dllNewVersion = newVersion;
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed downloading the .dll file, please print screen this and send to @. Marsh.Mello .#3194 on discord\n" + e.ToString());
            }

        }

        private void DLLDone(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled && e.Error != null)
            {
                SetProgress(100, "Failed downloading \"ModLoader.dll\"");
            }
            else
            {
                updates.dllversion = dllNewVersion;
                if (assetsversion > updates.assetversion)
                    UpdateAssets(assetsURL, assetsversion);
                else
                {
                    SaveUpdatesFile();
                    SetProgress(100, "Finished downloading updates");
                    SetPlayButton(false);
                }
            }
        }

        private void DLLProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage / 100, "Downloading \"ModLoader.dll\"...");
        }

        private void UpdateAssets(string url, int newVersion)
        {
            try
            {
                if (File.Exists(root + @"\modloader.assets"))
                    File.Delete(root + @"\modloader.assets");

                WebClient client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(AssetsProgress);
                client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(AssetsDone);
                client.DownloadFileAsync(new Uri(MainWindow.url + url), @"modloader.assets");
                assetsNewVersion = newVersion;
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed downloading the .dll file, please print screen this and send to @. Marsh.Mello .#3194 on discord\n" + e.ToString());
            }
            updates.assetversion = newVersion;
        }

        private void AssetsDone(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled && e.Error != null)
            {
                SetProgress(100, "Failed downloading \"modloader.assets\"");
            }
            else
            {
                updates.assetversion = assetsversion;

                SaveUpdatesFile();
                SetProgress(100, "Finished downloading updates");
                SetPlayButton(false);
            }
        }

        private void AssetsProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage / 100, "Downloading \"modloader.assets\"...");
        }

        private void UpdateExe()
        {
            MessageBox.Show("There is an update to the launcher\nPlease head over to " + url + " to download the latest version.","Launcher Update!");
        }
        private void SaveUpdatesFile()
        {
            using (FileStream stream = new FileStream(root + updatefile, FileMode.Create))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Updates));
                xml.Serialize(stream, updates);
            }
        }

        #endregion

        #region Launching Game
        private void OpenGame(object sender, RoutedEventArgs e)
        {
            if (isBusy)
                return;
            SetPlayButton(false);
            SetProgress(0, "Launching Game");
            GifState(gifStates.Play);

            //Launching the game
            Process.Start("steam://run/667970");

            //Searching For Process
            WaitForProcess();

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
                SetProgress(10 * i, "Searching for process...   (Attempt " + i + ")");
                await Task.Delay(5000);

                if (Process.GetProcessesByName("vtolvr").Length == 1)
                {
                    break;
                }

                if (i == maxTries)
                {
                    //If we couldn't find it, go back to how it was at the start
                    GifState(gifStates.Paused);
                    SetProgress(100, "Couldn't find VTOLVR process.");
                    SetPlayButton(true);
                    return;
                }
            }

            //A delay just to make sure the game has fully launched,
            SetProgress(50, "Waiting for game...");
            await Task.Delay(10000);

            //Injecting Default Mod
            SetProgress(75, "Injecting Mod Loader...");
            InjectDefaultMod();
            //Closing Exe
            Quit();
        }
        private void InjectDefaultMod()
        {
            //Injecting the default mod
            string defaultStart = string.Format("inject -p {0} -a {1} -n {2} -c {3} -m {4}", "vtolvr", "ModLoader.dll", "ModLoader", "Load", "Init");
            Process.Start(root + injector, defaultStart);
            Quit();
        }
        #endregion

        private void SetProgress(int barValue, string text)
        {
            
            progressText.Text = text;
            progressBar.Value = barValue;
        }
        private void SetPlayButton(bool disabled)
        {
            launchButton.Content = disabled ? "Busy" : "Play";
            isBusy = disabled;
        }

        private void Quit()
        {
            Process.GetCurrentProcess().Kill();
        }

        private void WebsiteMods(object sender, RoutedEventArgs e)
        {
            Process.Start("https://vtolvr-mods.com/mods.php");
        }

        private void WebsiteSkins(object sender, RoutedEventArgs e)
        {
            Process.Start("https://vtolvr-mods.com/skins.php");
        }

        private void Discord(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/49HDD7m");
        }

        private void Github(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/MarshMello0/VTOLVR-ModLoader");
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            Quit();
        }

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
            if (File.Exists(root + updatesFeedFile))
                File.Delete(root + updatesFeedFile);
        }

        private void TopBarLeave(object sender, MouseEventArgs e)
        {
            holdingDown = false;
        }
    }
}
