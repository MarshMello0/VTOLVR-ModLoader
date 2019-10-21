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
using System.IO.Compression;
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
        private static string dataFile = @"\data.xml";
        private static string dataFileTemp = @"\data_TEMP.xml";
        private static string dataURL = @"/files/data.xml";
        private static string url = @"http://vtolvr-mods.com/";
        private static string versionsFile = @"\versions.xml";
        private string root;

        private static int currentDLLVersion = 200;
        private static int currentEXEVersion = 200;

        //Startup
        private string[] needFiles = new string[] { "SharpMonoInjector.dll", "injector.exe" };
        private string[] neededDLLFiles = new string[] { @"\Plugins\discord-rpc.dll" };


        //Moving Window
        private bool holdingDown;
        private Point lm = new Point();
        private bool isBusy;
        //Updates
        private bool hasVersions;
        private int newDLLVersion;
        WebClient client;
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

            Update newUpdate = new Update("Title", "20/10/2019", "Description");
            FileUpdate newFileUpdate = new FileUpdate(200, 200);

            List<Update> updates = newData.updateFeed.ToList();
            List<FileUpdate> fileUpdates = newData.fileUpdates.ToList();
            updates.Insert(0,newUpdate);
            fileUpdates.Insert(0,newFileUpdate);
            newData.updateFeed = updates.ToArray();
            newData.fileUpdates = fileUpdates.ToArray();

            using (FileStream stream = new FileStream(root + @"\Data.xml", FileMode.Create))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Data));
                xml.Serialize(stream, newData);
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
            CheckBaseFolder();
            LoadVersions();
            GetData();
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
        private void LoadVersions()
        {
            if (File.Exists(root + versionsFile))
            {
                using (FileStream stream = new FileStream(root + versionsFile, FileMode.Open))
                {
                    XmlSerializer xml = new XmlSerializer(typeof(Versions));
                    Versions deserialized = (Versions)xml.Deserialize(stream);
                    currentDLLVersion = deserialized.currentDLLVersion;
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
                    UpdateExe();
                    CheckModsDependency();
                }
                else if (currentDLLVersion < deserialized.fileUpdates[0].dllVersion)
                {
                    UpdateDLL(url + "/files/updates/dll/" + deserialized.fileUpdates[0].dllVersion + "/ModLoader.dll",
                        deserialized.fileUpdates[0].dllVersion);
                }
                else if (!hasVersions)
                {
                    UpdateVersions();
                }
            }

            SetPlayButton(false);
            
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
        private void UpdateDLL(string url, int newVersion)
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
                    newDLLVersion = newVersion;
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
                currentDLLVersion = newDLLVersion;
                UpdateVersions();
                SetProgress(100, "Finished downloading updates");
                SetPlayButton(false);
                CheckModsDependency();
            }
        }
        private void DLLProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage / 100, "Downloading \"ModLoader.dll\"...");
            Console.WriteLine("Downloading \"ModLoader.dll\"... [" + e.ProgressPercentage / 100 + "]");
        }
        private void UpdateExe()
        {
            MessageBox.Show("There is an update to the launcher\nPlease head over to " + url + " to download the latest version.", "Launcher Update!");
        }
        private void UpdateVersions()
        {
            using (FileStream stream = File.Create(root + versionsFile))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Versions));
                xml.Serialize(stream, new Versions(currentDLLVersion,currentEXEVersion));
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

        private void CheckModsDependency()
        {
            SetPlayButton(true);
            DirectoryInfo folder = new DirectoryInfo(root + modsFolder);
            FileInfo[] files = folder.GetFiles("*.zip");
            float zipAmount = 100 / files.Length;
            int filesMoved = 0;
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo fileInfo = files[i];
                try
                {
                    FileStream file = File.OpenRead(fileInfo.FullName);

                    using (ZipArchive zip = new ZipArchive(file, ZipArchiveMode.Read))
                    {
                        foreach (ZipArchiveEntry entry in zip.Entries)
                        {
                            if (entry.FullName.Contains("Dependencies/"))
                            {
                                string name = entry.FullName.Split('/')[1];
                                if (name.Length > 0)
                                {
                                    MemoryStream ms = new MemoryStream();
                                    entry.Open().CopyTo(ms);
                                    FileStream file1 = new FileStream(Directory.GetParent(Directory.GetCurrentDirectory()).FullName +
                                        @"\VTOLVR_Data\Managed\" + entry.Name,
                                        FileMode.Create, FileAccess.Write);
                                    ms.WriteTo(file1);
                                    file1.Close();
                                    ms.Close();
                                    filesMoved++;
                                }

                            }
                        }
                    }
                    file.Close();
                    SetProgress((int)Math.Ceiling(zipAmount * i), "Moved dependencies for" + fileInfo.Name);
                }
                catch { }
                
            }
            SetProgress(100, filesMoved == 0 ? "No Dependencies to move" : "Moved " + filesMoved + " dependencies");
            SetPlayButton(false);
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
