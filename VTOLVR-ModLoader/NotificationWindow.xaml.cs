using System;
using System.Collections.Generic;
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
using System.Windows.Threading;
namespace VTOLVR_ModLoader
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        DispatcherTimer timer = new DispatcherTimer();
        MainWindow main;
        public NotificationWindow(string message, MainWindow main, int displayTime)
        {
            InitializeComponent();
            this.main = main;
            MessageText.Text = message;
            Left = main.Left + (main.Width / 2f) - (Width / 2f);
            Top = main.Top + (main.Height / 2f) - (Height / 2f);

            timer.Tick += CloseWindow;
            timer.Interval = new TimeSpan(0,0,displayTime);
            timer.Start();
        }

        private void CloseWindow(object sender, EventArgs e)
        {
            Close();
        }
    }
}
