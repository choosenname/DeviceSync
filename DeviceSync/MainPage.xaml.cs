
using Android.Content;
using Android.Net.Wifi;
using System.Net.NetworkInformation;

namespace DeviceSync;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        RemoteAddress.Text = Preferences.Default.Get("IpAddress", "192.168.0.1");
        LocalPort.Text = Preferences.Default.Get("LocalPort", "5001");
        RemotePort.Text = Preferences.Default.Get("RemotePort", "5002");

        string localIP;
        using (var socket = new Socket(SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp))
        {
            socket.Connect("example.com", 80);
            localIP = ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
        }

        Address.Text = $"IP Address: " + localIP;
    }

    private void Message_Clicked(object sender, EventArgs e)
    {
        Singleton.Instance.IpAddress = IPAddress.Parse(RemoteAddress.Text);
        Singleton.Instance.LocalPort = Convert.ToInt32(LocalPort.Text);
        Singleton.Instance.RemotePort = Convert.ToInt32(RemotePort.Text);

        Preferences.Default.Set("IpAddress", RemoteAddress.Text);
        Preferences.Default.Set("LocalPort", LocalPort.Text);
        Preferences.Default.Set("RemotePort", RemotePort.Text);

        Application.Current.MainPage = new NavigationPage(new MessagePage());
    }
}