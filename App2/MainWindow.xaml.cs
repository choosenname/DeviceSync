using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using DeviceSync;

namespace App2
{
    public partial class MainWindow : Window
    {
        private const string IpAddressKey = "IpAddress";
        private const string LocalPortKey = "LocalPort";
        private const string RemotePortKey = "RemotePort";

        public MainWindow()
        {
            InitializeComponent();

            RemoteAddress.Text = Properties.Settings.Default.IpAddress;
            LocalPort.Text = Properties.Settings.Default.LocalPort;
            RemotePort.Text = Properties.Settings.Default.RemotePort;

            Address.Content = "Your IP Address: ...";

            Task.Run(() =>
            {
                using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect("example.com", 80);
                    string ipAddress = ((IPEndPoint)socket.LocalEndPoint).Address.ToString();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Address.Content = "Your IP Address: " + ipAddress;
                    });
                }
            });
        }

        private void Message_Clicked(object sender, RoutedEventArgs e)
        {
            Singleton.Instance.IpAddress = IPAddress.Parse(RemoteAddress.Text);
            Singleton.Instance.LocalPort = Convert.ToInt32(LocalPort.Text);
            Singleton.Instance.RemotePort = Convert.ToInt32(RemotePort.Text);

            Properties.Settings.Default.IpAddress = RemoteAddress.Text;
            Properties.Settings.Default.LocalPort = LocalPort.Text;
            Properties.Settings.Default.RemotePort = RemotePort.Text;
            Properties.Settings.Default.Save();

            var messageWindow = new MessageWindow();
            messageWindow.Show();
            Close();
        }
    }
}