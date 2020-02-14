using System;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading.Tasks;
using WpfAnimatedGif;
using System.Net;
using System.Xml.Serialization;
using System.ComponentModel;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace VTOLVR_ModLoader
{
    public partial class MainWindow : Window
    {
        private enum gifStates { Paused, Play, Frame }

        public static string modsFolder = @"\mods";
        private static string skinsFolder = @"\skins";
        private static string injector = @"\injector.exe";
        private static string updatesFile = @"\updates.xml";
        private static string updatesFileTemp = @"\updates_TEMP.xml";
        private static string updatesURL = @"/files/updates.xml";
        private string url = @"https://vtolvr-mods.com";
        public static string root;
        public static string vtolFolder;

        //Startup
        private string[] needFiles = new string[] { "SharpMonoInjector.dll", "injector.exe", "Updater.exe" };
        private string[] neededDLLFiles = new string[] { @"\Plugins\discord-rpc.dll", @"\Managed\0Harmony.dll" };
        private string[] args;
        private bool autoStart;

        //Moving Window
        private bool holdingDown;
        private Point lm = new Point();
        private bool isBusy;
        //Updates
        WebClient client;
        //URI
        private bool uriSet = false;
        private string uriDownload;
        private string uriFileName;
        //Notifications
        private NotificationWindow notification;
        //Storing completed tasks
        private int extractedMods = 0;
        private int extractedSkins = 0;
        private int movedDep = 0;
        //Dev Console
        public static bool devConsole;
        //Settings
        public static Settings settings;
        public static Pilot pilotSelected;
        public static Scenario scenarioSelected;
        public static List<string> modsToLoad = new List<string>();
        public static SettingsSave save;
        public static readonly string savePath = @"settings.xml";

        private static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        #region Startup
        public MainWindow()
        {
            SearchForProcess();
#if DEBUG
            url = "http://localhost";
#endif
            InitializeComponent();
        }
        private void SearchForProcess()
        {
            //Stopping their being more than one open (Yes this could close the other one half way through a download)
            Process[] p = Process.GetProcessesByName("VTOLVR-ModLoader");
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Id != Process.GetCurrentProcess().Id)
                {
                    p[i].Kill();
                }
            }
        }

        private void Start(object sender, EventArgs e)
        {
            root = Directory.GetCurrentDirectory();
            vtolFolder = root.Replace("VTOLVR_ModLoader", "");
            args = Environment.GetCommandLineArgs();
            WaitAsync();
        }

        private async void WaitAsync()
        {
            await Task.Delay(500);

            if (args.Length == 2 && args[1].Contains("vtolvrml"))
                URICheck();
            else if (CheckForArg("autostart"))
                autoStart = true;
            else
                CheckBaseFolder();

            GetData();
            if (SettingsSaveExists())
            {
                save = LoadSettings();
                devConsole = save.devConsole;
                pilotSelected = save.previousPilot;
                scenarioSelected = save.previousScenario;
            }
            else
                save = new SettingsSave();
        }
        public bool CheckForArg(string arg)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(arg))
                    return true;
            }
            return false;
        }
        private void URICheck()
        {
            root = args[0];
            //This is removing the "\VTOLVR-ModLoader.exe" at the end, it will always be a fixed 21 characters
            root = root.Remove(root.Length - 21, 21);
            vtolFolder = root.Replace("VTOLVR_ModLoader", "");

            string argument = args[1].Remove(0, 11);
            if (argument.Contains("files"))
            {
                uriDownload = argument;
                uriSet = true;
            }
            else
                MessageBox.Show(argument, "URI Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (!File.Exists(Directory.GetParent(root).FullName + @"\VTOLVR_Data" + file))
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
        private void GetData()
        {
            SetProgress(0, "Getting Changelog...");
            SetPlayButton(true);
            if (CheckForInternet())
            {
                client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(UpdatesProgress);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(UpdatesDone);
                client.DownloadFileAsync(new Uri(url + updatesURL), root + updatesFileTemp);
            }
            else
            {
                if (File.Exists(root + updatesFile))
                    LoadData();
                SetProgress(100, "Failed to connect to the internet");
                SetPlayButton(false);
                if (autoStart)
                    OpenGame(null, null);
            }
        }
        private void UpdatesProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage / 100, "Downloading data...");
        }
        private void UpdatesDone(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                if (File.Exists(root + updatesFile))
                    File.Delete(root + updatesFile);
                File.Move(root + updatesFileTemp, root + updatesFile);
                SetProgress(100, "Downloaded updates.xml");
            }
            else
            {
                SetProgress(100, "Failed to connect to server.");
                Console.WriteLine("Failed getting feed \n" + e.Error.ToString());
                if (File.Exists(root + updatesFileTemp))
                    File.Delete(root + updatesFileTemp);
                SetPlayButton(true);
            }
            LoadData();
            client.Dispose();
        }
        private void LoadData()
        {
            using (FileStream stream = new FileStream(root + updatesFile, FileMode.Open))
            {
                XmlSerializer xml = new XmlSerializer(typeof(UpdateData));
                UpdateData deserialized = (UpdateData)xml.Deserialize(stream);
                //Updating Feed from file
                updateFeed.ItemsSource = deserialized.Updates;

                //Checking versions. If auto start is true, skip checking for updates.
                if (autoStart == false && CheckForInternet())
                {
                    bool needsUpdate = false;
                    Update lastUpdate = deserialized.Updates[0];

                    for (int i = 0; i < lastUpdate.Files.Length; i++)
                    {
                        if (!File.Exists(vtolFolder + lastUpdate.Files[i].FileLocation) ||
                            CalculateMD5(vtolFolder + lastUpdate.Files[i].FileLocation) != lastUpdate.Files[i].FileHash.ToLower())
                        {
                            needsUpdate = true;
                        }
                    }

                    if (needsUpdate && File.Exists(root + "/Updater.exe"))
                    {
                        if (MessageBox.Show("Would you like to download the update?", "Update Available!", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            Process.Start(root + "/Updater.exe");
                            Quit();
                            return;
                        }
                    }
                }
            }

            SetPlayButton(false);
            ExtractMods();
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
            
            if (devConsole || 
                (pilotSelected != null && scenarioSelected != null) || 
                (modsToLoad != null && modsToLoad.Count > 0))
            {
                string regPath = (string)Registry.GetValue(
    @"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam",
    @"SteamPath",
    @"NULL");
                string args = string.Empty;
                if (pilotSelected != null && scenarioSelected != null &&
                    pilotSelected.Name != "No Selection" && scenarioSelected.Name != "No Selection")
                {
                    args += $" PILOT={pilotSelected.Name} SCENARIO_CID={scenarioSelected.cID} SCENARIO_ID={scenarioSelected.ID}";
                }

                if (devConsole)
                {
                    args += " dev";
                }

                if (modsToLoad != null && modsToLoad.Count > 0)
                {
                    for (int i = 0; i < modsToLoad.Count; i++)
                    {
                        args += " mod=" + modsToLoad[i] + ""; //Example " mod=NoGravity\NoGravity.dll "
                    }
                }

                Process process = new Process();
                process.StartInfo.FileName = regPath + @"\steam.exe";
                process.StartInfo.Arguments = @"-applaunch 667970" + args;
                process.Start();
            }
                
            else
            
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
                    SetPlayButton(false);
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

        #region Handeling Mods
        private void ExtractMods()
        {
            if (uriSet)
            {
                DownloadFile();
                return;
            }
            SetPlayButton(true);
            SetProgress(0, "Extracting  mods...");
            DirectoryInfo folder = new DirectoryInfo(root + modsFolder);
            FileInfo[] files = folder.GetFiles("*.zip");
            if (files.Length == 0)
            {
                SetPlayButton(false);
                SetProgress(100, "No new mods were found");
                MoveDependencies();
                return;
            }
            float zipAmount = 100 / files.Length;
            string currentFolder;

            for (int i = 0; i < files.Length; i++)
            {
                SetProgress((int)Math.Ceiling(zipAmount * i), "Extracting mods... [" + files[i].Name + "]");
                //This should remove the .zip at the end for the folder path
                currentFolder = files[i].FullName.Split('.')[0];

                //We don't want to overide any mod folder incase of user data
                //So mod users have to update by hand
                if (Directory.Exists(currentFolder))
                    continue;

                Directory.CreateDirectory(currentFolder);
                ZipFile.ExtractToDirectory(files[i].FullName, currentFolder);
                extractedMods++;

                //Deleting the zip
                //File.Delete(files[i].FullName);
            }

            SetPlayButton(false);
            SetProgress(100, extractedMods == 0 ? "No mods were extracted" : "Extracted " + extractedMods +
                (extractedMods == 1 ? " mod" : " mods"));
            MoveDependencies();

        }
        private void ExtractSkins()
        {
            SetPlayButton(true);
            SetProgress(0, "Extracting skins...");
            DirectoryInfo folder = new DirectoryInfo(root + skinsFolder);
            FileInfo[] files = folder.GetFiles("*.zip");
            if (files.Length == 0)
            {
                SetPlayButton(false);
                SetProgress(100,
                    (extractedMods == 0 ? "0 Mods" : (extractedMods == 1 ? "1 Mod" : extractedMods + " Mods")) +
                    " and " +
                    (extractedSkins == 0 ? "0 Skins" : (extractedSkins == 1 ? "1 Skin" : extractedSkins + " Skins")) +
                    " extracted" +
                    " and " +
                    (movedDep == 0 ? "0 Dependencies" : (movedDep == 1 ? "1 Dependencies" : movedDep + " Dependencies")) +
                    " moved");
                if (autoStart)
                    OpenGame(null, null);
                return;
            }
            float zipAmount = 100 / files.Length;
            string currentFolder;

            for (int i = 0; i < files.Length; i++)
            {
                SetProgress((int)Math.Ceiling(zipAmount * i), "Extracting skins... [" + files[i].Name + "]");
                //This should remove the .zip at the end for the folder path
                currentFolder = files[i].FullName.Split('.')[0];

                //We don't want to overide any mod folder incase of user data
                //So mod users have to update by hand
                if (Directory.Exists(currentFolder))
                    continue;

                Directory.CreateDirectory(currentFolder);
                ZipFile.ExtractToDirectory(files[i].FullName, currentFolder);
                extractedSkins++;
            }

            SetPlayButton(false);
            //This is the final text displayed in the progress text
            SetProgress(100,
                (extractedMods == 0 ? "0 Mods" : (extractedMods == 1 ? "1 Mod" : extractedMods + " Mods")) +
                " and " +
                (extractedSkins == 0 ? "0 Skins" : (extractedSkins == 1 ? "1 Skin" : extractedSkins + " Skins")) +
                " extracted" +
                " and " +
                (movedDep == 0 ? "0 Dependencies" : (movedDep == 1 ? "1 Dependencies" : movedDep + " Dependencies")) +
                " moved");
            if (autoStart)
                OpenGame(null, null);
        }

        private void MoveDependencies()
        {
            SetPlayButton(true);
            string[] modFolders = Directory.GetDirectories(root + modsFolder);

            string fileName;
            string[] split;
            for (int i = 0; i < modFolders.Length; i++)
            {
                string[] subFolders = Directory.GetDirectories(modFolders[i]);
                for (int j = 0; j < subFolders.Length; j++)
                {
                    Console.WriteLine("Checking " + subFolders[j].ToLower());
                    if (subFolders[j].ToLower().Contains("dependencies"))
                    {
                        Console.WriteLine("Found the folder dependencies");
                        string[] depFiles = Directory.GetFiles(subFolders[j], "*.dll");
                        for (int k = 0; k < depFiles.Length; k++)
                        {
                            split = depFiles[k].Split('\\');
                            fileName = split[split.Length - 1];

                            if (File.Exists(Directory.GetParent(root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName))
                            {
                                string oldHash = CalculateMD5(Directory.GetParent(root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName);
                                string newHash = CalculateMD5(depFiles[k]);
                                if (!oldHash.Equals(newHash))
                                {
                                    File.Copy(depFiles[k], Directory.GetParent(root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName,
                                        true);
                                    movedDep++;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Moved file \n" + Directory.GetParent(root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName);
                                File.Copy(depFiles[k], Directory.GetParent(root).FullName +
                                            @"\VTOLVR_Data\Managed\" + fileName,
                                            true);
                                movedDep++;
                            }
                        }
                        break;
                    }
                }
            }

            SetPlayButton(false);
            SetProgress(100, movedDep == 0 ? "Checked Dependencies" : "Moved " + movedDep
                + (movedDep == 1 ? " dependency" : " dependencies"));

            ExtractSkins();
        }

        private void DownloadFile()
        {
            if (uriDownload.Equals(string.Empty) || uriDownload.Split('/').Length < 4)
                return;

            uriFileName = uriDownload.Split('/')[3];
            bool isMod = uriDownload.Contains("mods");
            client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(FileProgress);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(FileDone);
            client.DownloadFileAsync(new Uri(url + "/" + uriDownload), root + (isMod ? modsFolder : skinsFolder) + @"\" + uriFileName);
        }

        private void FileDone(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                ShowNotification("Downloaded " + uriFileName);
                //Checking if they already had the mod extracted incase they wanted to update it
                bool isMod = uriDownload.Contains("mods");
                if (Directory.Exists(root + (isMod ? modsFolder : skinsFolder) + @"\" + uriFileName.Split('.')[0]))
                {
                    Directory.Delete(root + (isMod ? modsFolder : skinsFolder) + @"\" + uriFileName.Split('.')[0], true);
                }
            }
            else
            {
                MessageBox.Show("Failed Downloading " + uriFileName + "\n" + e.Error.ToString(),
                    "Failed Downloading File", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            uriSet = false;
            SetProgress(100, "Downloaded " + uriFileName);
            SetPlayButton(false);
            ExtractMods();
        }

        private void FileProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage / 100, "Downloading " + uriFileName + "...");
        }

        #endregion

        private void ShowNotification(string text)
        {
            if (notification != null)
            {
                notification.Close();
            }
            notification = new NotificationWindow(text, this, 5);
            notification.Owner = this;
            notification.Show();
        }
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

        private void Website(object sender, RoutedEventArgs e)
        {
            Process.Start("https://vtolvr-mods.com");
        }
        private void Discord(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/49HDD7m");
        }
        private void Patreon(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.patreon.com/vtolvrmods");
        }
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            Process.Start(root);
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            Quit();
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

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            if (settings != null)
            {
                settings.Activate();
                return;
            }
            settings = new Settings();
            settings.Show();
        }

        private bool SettingsSaveExists()
        {
            return File.Exists(root + @"\" + savePath);
        }

        private SettingsSave LoadSettings()
        {
            using (FileStream stream = new FileStream(root + @"\" + savePath, FileMode.Open))
            {
                XmlSerializer xml = new XmlSerializer(typeof(SettingsSave));
                return (SettingsSave)xml.Deserialize(stream);
            }
        }
    }

    public class Mod
    {
        public string name;
        public string description;
        public Mod() { }

        public Mod(string name, string description)
        {
            this.name = name;
            this.description = description;
        }
    }
}
