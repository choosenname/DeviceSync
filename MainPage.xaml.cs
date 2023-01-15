using System.Net;
using System.Net.Sockets;

namespace DeviceSync;

public partial class MainPage : ContentPage
{
    // адрес и порт сервера, к которому будем подключаться
    IPAddress ipAddress; // адрес сервера
    int remotePort;
    int localPort;

    public MainPage()
	{
		InitializeComponent();
        IPAddress[] localIp = Dns.GetHostAddresses(Dns.GetHostName());
        foreach (IPAddress address in localIp)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                ipAddress = address;
            }
        }
        Address.Text = String.Format(@"Started listening requests at: {0} : {1}", ipAddress, localPort);
    }

    private void Message_Clicked(object sender, EventArgs e)
    {
        ipAddress = IPAddress.Parse(RemoteAddress.Text);
        localPort = Convert.ToInt32(LocalPort.Text);
        remotePort = Convert.ToInt32(RemotePort.Text);
        Application.Current.MainPage = new NavigationPage(new MessagePage(ipAddress, remotePort, localPort));
    }
}

