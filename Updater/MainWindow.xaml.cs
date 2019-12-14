using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Net;
using System.ComponentModel;
using System.Xml;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string LogPath = @"\updater_log.txt";
        private readonly string url = "https://vtolvr-mods.com/files/updates.xml";

        private string path;
        private UpdateData updateData;
        private WebClient client;
        public MainWindow()
        {
            path = Directory.GetCurrentDirectory();
#if DEBUG
            url = "http://localhost/files/updates.xml";
            Log("Using Localhost as URL");
            GenerateUpdatesXML();
#endif

            InitializeComponent();
            Log("Updater has been launched");

            FetchUpdatesData();
        }

        private void GenerateUpdatesXML()
        {
            updateData = new UpdateData();
            updateData.Updates = new Update[]
            {
                new Update("-Item one\n-Item Two", new Item[]
                {
                    new Item("downloadURL", "File Location", "File hash")
                })
            };

            using (FileStream stream = new FileStream(path + @"\updates.xml", FileMode.Create))
            {
                XmlSerializer xml = new XmlSerializer(typeof(UpdateData));
                xml.Serialize(stream, updateData);
            }
            
        }

        private void FetchUpdatesData()
        {
            if (CheckForInternet())
            {
                SetText("Fetching Update Data");
                client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(UpdateDataProgress);
                client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(UpdateDataDone);
                client.DownloadStringAsync(new Uri(url));
            }
        }

        private void UpdateDataProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage);
            Log("UpdateData Progress = " + e.ProgressPercentage);
        }

        private void UpdateDataDone(object sender, DownloadStringCompletedEventArgs e)
        {
            Log("Done Downloading Update Data");
            if (e.Error != null)
            {
                Log("Erroed \n" + e.Error.Message);
            }
            else if (e.Cancelled)
            {
                Log("User Cancelled the download");
            }
            else
            {
                Log("Download Complete");
                updateData = (UpdateData)new XmlSerializer(typeof(UpdateData))
                    .Deserialize(new XmlTextReader(new StringReader(e.Result)));
                UpdateFiles();
            }
        }

        private void UpdateFiles()
        {
            Log("Updating Files");
            SetText("Updating Files");

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
                //There is a second try incase they have blocked googles domain
                try
                {
                    using (var client = new WebClient())
                    {
                        using (client.OpenRead("https://vtolvr-mods.com/"))
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
        }

        private void OpenLog(object sender, RoutedEventArgs e)
        {
            Log("Opening Log");
            if (File.Exists(path + LogPath))
                Process.Start(path + LogPath);
        }

        private void SetText(string text)
        {
            Log("progressText = " + text);
            progressText.Text = text;
        }

        private void SetProgress(float amount)
        {
            progress.Value = amount;
        }

        private void Log(string message, bool includeDate = true)
        {
            DateTime Now = DateTime.Now;
            File.AppendAllText(path + LogPath,
                (includeDate ? $"[{Now.Day}/{Now.Month}/{Now.Year} {Now.Hour}:{Now.Minute}:{Now.Second}] " : "") + 
                message + "\n");
        }
    }
}
