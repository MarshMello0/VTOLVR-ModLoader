using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace VTOLVR_ModLoader
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {

        //Moving Window
        private bool holdingDown;
        private Point lm = new Point();
        private bool isBusy;

        public Settings()
        {
            InitializeComponent();
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            Quit();
        }
        private void Quit()
        {
            Settings s = MainWindow.settings;
            MainWindow.settings = null;
            s.Close();
        }
        #region Moving Window
        private void TopBarDown(object sender, MouseButtonEventArgs e)
        {
            holdingDown = true;
            lm = Mouse.GetPosition(this);
        }

        private void TopBarUp(object sender, MouseButtonEventArgs e)
        {
            holdingDown = false;
        }

        private void TopBarMove(object sender, MouseEventArgs e)
        {
            if (holdingDown)
            {
                this.Left += Mouse.GetPosition(this).X - lm.X;
                this.Top += Mouse.GetPosition(this).Y - lm.Y;
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

        private void DevConsole(object sender, RoutedEventArgs e)
        {
            if (devConsoleCheckbox.IsChecked == true)
                MainWindow.devConsole = true;
            else if (devConsoleCheckbox.IsChecked == false)
                MainWindow.devConsole = false;
        }

        private void CreateInfo(object sender, RoutedEventArgs e)
        {
            Mod newMod = new Mod(modName.Text, modDescription.Text);

            Directory.CreateDirectory(MainWindow.root + $"\\mods\\{modName.Text}");

            using (FileStream stream = new FileStream(MainWindow.root + $"\\mods\\{modName.Text}\\info.xml", FileMode.Create))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Mod));
                xml.Serialize(stream, newMod);
            }

            MessageBox.Show("Created info.xml in \n\"" + MainWindow.root + $"\\mods\\{modName.Text}\"", "Created Info.xml", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
