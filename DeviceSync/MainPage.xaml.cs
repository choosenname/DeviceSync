namespace DeviceSync;

public partial class MainPage : ContentPage
{
    private const string IpAddressKey = "IpAddress";
    private const string LocalPortKey = "LocalPort";
    private const string RemotePortKey = "RemotePort";

    public MainPage()
    {
        InitializeComponent();

        RemoteAddress.Text = Preferences.Default.Get(IpAddressKey, "192.168.0.1");
        LocalPort.Text = Preferences.Default.Get(LocalPortKey, "5001");
        RemotePort.Text = Preferences.Default.Get(RemotePortKey, "5002");

        Task.Run(() =>
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect("example.com", 80);
                Address.Text = "Your IP Address: " + ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
            }
        });
    }

    private void Message_Clicked(object sender, EventArgs e)
    {
        Singleton.Instance.IpAddress = IPAddress.Parse(RemoteAddress.Text);
        Singleton.Instance.LocalPort = Convert.ToInt32(LocalPort.Text);
        Singleton.Instance.RemotePort = Convert.ToInt32(RemotePort.Text);

        Preferences.Default.Set(IpAddressKey, RemoteAddress.Text);
        Preferences.Default.Set(LocalPortKey, LocalPort.Text);
        Preferences.Default.Set(RemotePortKey, RemotePort.Text);

        Application.Current.MainPage = new NavigationPage(new MessagePage());
    }
}