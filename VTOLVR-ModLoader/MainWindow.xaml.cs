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

namespace VTOLVR_ModLoader
{
    public partial class MainWindow : Window
    {
        private enum gifStates { Paused, Play, Frame }

        private static string modsFolder = @"\mods";
        private static string injector = @"\injector.exe";
        private string root;
        private bool continueDots = true;
        private string continueText = "Launching Game";

        private string[] needFiles = new string[] { "SharpMonoInjector.dll", "injector.exe", "ModLoader.dll", "WpfAnimatedGif.dll" };

        public MainWindow()
        {
            InitializeComponent();
            root = Directory.GetCurrentDirectory();
            CheckFolder();
        }
        private void CheckFolder()
        {
            foreach (string file in needFiles)
            {
                if (!File.Exists(root + @"\" + file))
                {
                    WrongFolder(file);
                    return;
                }
            }
            if (!Directory.Exists(root + modsFolder))
            {
                Directory.CreateDirectory(root + modsFolder);
            }
        }
        private void WrongFolder(string file)
        {
            MessageBox.Show("I can't seem to find " + file + " in my folder. Make sure you place me in the same folder as this file.", "Missing File");
            Quit();
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
            await Task.Delay(20000);

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
