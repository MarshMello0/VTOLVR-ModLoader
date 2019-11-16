﻿using System;
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
using System.IO.Compression;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WpfAnimatedGif;
using System.Net;
using System.Xml.Serialization;
using System.ComponentModel;
using Caliburn.Micro;
using Microsoft.Win32;
using System.Security.Cryptography;

namespace VTOLVR_ModLoader
{
    public partial class MainWindow : Window
    {
        private enum gifStates { Paused, Play, Frame }

        private static string modsFolder = @"\mods";
        private static string skinsFolder = @"\skins";
        private static string injector = @"\injector.exe";
        private static string dataFile = @"\data.xml";
        private static string dataFileTemp = @"\data_TEMP.xml";
        private static string dataURL = @"/files/data.xml";
        private static string url = @"http://localhost:81/";
        private static string versionsFile = @"\versions.xml";
        private static string uriPath = @"HKEY_CLASSES_ROOT\VTOLVRML";
        private string root;

        private static int currentEXEVersion = 200;

        //Startup
        private string[] needFiles = new string[] { "SharpMonoInjector.dll", "injector.exe" };
        private string[] neededDLLFiles = new string[] { @"\Plugins\discord-rpc.dll", @"\Managed\0Harmony.dll" };
        private string[] args;

        //Moving Window
        private bool holdingDown;
        private Point lm = new Point();
        private bool isBusy;
        //Updates
        private bool hasVersions, updateExe;
        private int newExeVersion;
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

        #region Releasing Update
        private void CreateUpdatedFeed()
        {
            Data newData = new Data();
            if (File.Exists(root + @"\Data.xml"))
            {
                using (FileStream stream = new FileStream(root + @"\Data.xml", FileMode.Open))
                {
                    XmlSerializer xml = new XmlSerializer(typeof(Data));
                    newData = (Data)xml.Deserialize(stream);
                }
            }

            //Update newUpdate = new Update("Title", "20/10/2019", "Description");
            string newHash = CalculateMD5(root + @"\ModLoader.dll");
            FileUpdate newFileUpdate = new FileUpdate(210, new ModLoaderDLL(210,newHash));

            //List<Update> updates = newData.updateFeed.ToList();
            List<FileUpdate> fileUpdates = newData.fileUpdates.ToList();
            //updates.Insert(0, newUpdate);
            fileUpdates.Insert(0, newFileUpdate);
            //newData.updateFeed = updates.ToArray();
            newData.fileUpdates = fileUpdates.ToArray();

            using (FileStream stream = new FileStream(root + @"\Data.xml", FileMode.Create))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Data));
                xml.Serialize(stream, newData);
            }
        }

        static string CalculateMD5(string filename)
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
        #endregion

        #region Startup
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Start(object sender, EventArgs e)
        {
            root = Directory.GetCurrentDirectory();
            args = Environment.GetCommandLineArgs();
            WaitAsync();
        }

        private async void WaitAsync()
        {
            await Task.Delay(500);

            URICheck();
            if (!uriSet)
                CreateURI();

            CheckBaseFolder();
            LoadVersions();
            GetData();
        }
        private void URICheck()
        {
            if (args.Length == 2 && root.Contains("System32"))
            {
                root = args[0];
                //This is removing the "\VTOLVR-ModLoader.exe" at the end, it will always be a fixed 21 characters
                root = root.Remove(root.Length - 21, 21);
                if (args[1].Contains("file="))
                {
                    string argument = args[1].Remove(0, 11);
                    if (argument.Contains("file="))
                    {
                        string splitFile = argument.Remove(0,5);
                        if (!string.IsNullOrEmpty(splitFile))
                        {
                            uriDownload = splitFile;
                            uriSet = true;
                        }
                        else
                            MessageBox.Show(argument, "URI Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private void CreateURI()
        {
            string value = (string)Registry.GetValue(
                uriPath,
                @"",
                @"");
            if (value == null)
            {
                //Setting Default
                Registry.SetValue(
                uriPath,
                @"",
                @"URL:VTOLVRML");
                //Setting URL Protocol
                Registry.SetValue(
                uriPath,
                @"URL Protocol",
                @"");
                //Setting Default Icon
                Registry.SetValue(
                    uriPath + @"\DefaultIcon",
                    @"",
                    root + @"\VTOLVR-ModLoader.exe,1");
                //Setting Command
                Registry.SetValue(
                    uriPath + @"\shell\open\command",
                    @"",
                    "\"" + root + @"\VTOLVR-ModLoader.exe" + "\" \"" + @"%1" + "\"");
            }
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
        private void LoadVersions()
        {
            if (File.Exists(root + versionsFile))
            {
                using (FileStream stream = new FileStream(root + versionsFile, FileMode.Open))
                {
                    XmlSerializer xml = new XmlSerializer(typeof(Versions));
                    Versions deserialized = (Versions)xml.Deserialize(stream);
                    currentEXEVersion = deserialized.currentEXEVersion;
                    hasVersions = true;
                }
            }
            else
            {
                hasVersions = false;
            }
        }
        #endregion

        #region Auto Updater
        private void GetData()
        {
            SetProgress(0, "Getting Data Feed...");
            SetPlayButton(true);
            if (CheckForInternet())
            {
                client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DataProgress);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(DataDone);
                client.DownloadFileAsync(new Uri(url + dataURL), root + dataFileTemp);
            }
            else
            {
                SetProgress(100, "Failed to connect to the internet");
                SetPlayButton(false);
            }
        }
        private void DataProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage / 100, "Downloading data...");
        }
        private void DataDone(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                if (File.Exists(root + dataFile))
                    File.Delete(root + dataFile);
                File.Move(root + dataFileTemp, root + dataFile);
                SetProgress(100, "Downloaded Data.xml");
                LoadData();
            }
            else
            {
                SetProgress(100, "Failed to connect to server.");
                Console.WriteLine("Failed getting feed \n" + e.Error.ToString());
                if (File.Exists(root + dataFileTemp))
                    File.Delete(root + dataFileTemp);
                SetPlayButton(true);
                LoadData();
            }
            client.Dispose();
        }
        private void LoadData()
        {
            using (FileStream stream = new FileStream(root + dataFile, FileMode.Open))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Data));
                Data deserialized = (Data)xml.Deserialize(stream);
                //Updating Feed from file
                updateFeed.ItemsSource = deserialized.updateFeed;

                //Checking versions
                if (currentEXEVersion < deserialized.fileUpdates[0].exeVersion)
                {
                    UpdateExe(deserialized.fileUpdates[0].exeVersion);
                }
                else if (CalculateMD5(root + @"\ModLoader.dll") != deserialized.fileUpdates[0].dll.hash)
                {
                    UpdateDLL(url + "/files/updates/dll/" + deserialized.fileUpdates[0].dll.version + "/ModLoader.dll");
                }
                else if (!hasVersions)
                {
                    UpdateVersions();
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
        private void UpdateDLL(string url)
        {
            try
            {
                if (File.Exists(root + @"\ModLoader.dll"))
                    File.Delete(root + @"\ModLoader.dll");

                using (client = new WebClient())
                {
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DLLProgress);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(DLLDone);
                    client.DownloadFileAsync(new Uri(url), @"ModLoader.dll");
                }
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
                MessageBox.Show("There was a strange error, please download the mod loader again from the website." +
                    "\n" + e.Error.Message);
            }
            else
            {
                UpdateVersions();
                SetProgress(100, "Finished downloading updates");
                SetPlayButton(false);
                ExtractMods();
            }
        }
        private void DLLProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage / 100, "Downloading \"ModLoader.dll\"...");
            Console.WriteLine("Downloading \"ModLoader.dll\"... [" + e.ProgressPercentage / 100 + "]");
        }
        private void UpdateExe(int newVersion)
        {
            ShowNotification("The launcher will auto update next time you launch VTOL");
            newExeVersion = newVersion;
            updateExe = true;

            if (!hasVersions)
                UpdateVersions();
        }
        private void UpdateVersions()
        {
            using (FileStream stream = File.Create(root + versionsFile))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Versions));
                xml.Serialize(stream, new Versions(0, currentEXEVersion));
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
            if (updateExe)
            {
                string regPath = (string)Registry.GetValue(
    @"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam",
    @"SteamPath",
    @"NULL");

                Process process = new Process();
                process.StartInfo.FileName = regPath + @"\steam.exe";
                process.StartInfo.Arguments = @"-applaunch 667970" + " -updateLauncher " + newExeVersion;
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
                SetProgress(100, "No new skins were found");
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
                (extractedMods == 0? "0 Mods" : (extractedMods == 1? "1 Mod" : extractedMods + " Mods")) + 
                " and " +
                (extractedSkins == 0 ? "0 Skins" : (extractedSkins == 1 ? "1 Skin" : extractedSkins + " Skins")) + 
                " extracted" + 
                " and " +
                (movedDep == 0 ? "0 Dependencies" : (movedDep == 1 ? "1 Dependencies" : movedDep + " Dependencies")) +
                " moved");
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
                            Console.WriteLine("Moved file \n" + Directory.GetParent(root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName);
                            File.Copy(depFiles[k], Directory.GetParent(root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName,
                                        true);

                            movedDep++;
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
            
            uriFileName = uriDownload.Split('/')[4];
            bool isMod = uriDownload.Contains("mods");
            client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(FileProgress);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(FileDone);
            client.DownloadFileAsync(new Uri(url + uriDownload.Remove(0,1)), root + (isMod? modsFolder : skinsFolder) + @"\" + uriFileName);
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
                    Directory.Delete(root + (isMod ? modsFolder : skinsFolder) + @"\" + uriFileName.Split('.')[0],true);
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
            notification = new NotificationWindow(text, this,5);
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

        private void ModCreator(object sender, RoutedEventArgs e)
        {
            Mod newMod = new Mod();
            newMod.name = "Mod Name";
            newMod.description = "Mod Description";
            using (FileStream stream = new FileStream(root + @"\info.xml", FileMode.Create))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Mod));
                xml.Serialize(stream, newMod);
            }

            MessageBox.Show("Created info.xml in \n\"" + root + "\"", "Created Info.xml", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }

    public class Mod
    {
        public string name;
        public string description;
        public Mod() { }
    }
}
