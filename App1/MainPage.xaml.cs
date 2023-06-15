using DeviceSync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string IpAddressKey = "IpAddress";
        private const string LocalPortKey = "LocalPort";
        private const string RemotePortKey = "RemotePort";
        public MainPage()
        {
            InitializeComponent();
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            RemoteAddress.Text = localSettings.Values[IpAddressKey] as string ?? string.Empty;
            LocalPort.Text = localSettings.Values[LocalPortKey] as string ?? string.Empty;
            RemotePort.Text = localSettings.Values[RemotePortKey] as string ?? string.Empty;

            Address.Text = "Your IP Address: ...";

            Task.Run(async () =>
            {
                using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                socket.Connect("example.com", 80);
                string IPAdress = ((IPEndPoint)socket.LocalEndPoint).Address.ToString();

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Address.Text = "Your IP Address: " + IPAdress;
                });
            });
        }

        private void Message_Clicked(object sender, RoutedEventArgs e)
        {
            Singleton.Instance.IpAddress = IPAddress.Parse(RemoteAddress.Text);
            Singleton.Instance.LocalPort = Convert.ToInt32(LocalPort.Text);
            Singleton.Instance.RemotePort = Convert.ToInt32(RemotePort.Text);

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[IpAddressKey] = RemoteAddress.Text;
            localSettings.Values[LocalPortKey] = LocalPort.Text;
            localSettings.Values[RemotePortKey] = RemotePort.Text;

            Window.Current.Content = new Frame()
            {
                Content = new MessagePage()
            };
        }
    }
}
