

using System.Net.Sockets;
using System.Net;

namespace DeviceSync;

public partial class MessagePage : ContentPage
{
    static UdpClient udpSender;
    static UdpClient udpReceiver;

    event ExceptionHandler ExceptionNotify;


    delegate void ExceptionHandler(Exception ex);
    public MessagePage()
    {
        InitializeComponent();

        ExceptionNotify += (ex) => { DisplayAlert("Exception", ex.Message + "\n" + ex.StackTrace, "OK"); };

        try
        {
            Receiver();
        }
        catch (Exception ex)
        {
            ExceptionNotify(ex);
        }
    }

    private async void Receiver()
    {
        while (true)
        {
            using (udpReceiver = new UdpClient(Singleton.Instance.LocalPort))
            {
                var result = await udpReceiver.ReceiveAsync();

                var message = Encoding.UTF8.GetString(result.Buffer);

                AddMessageToChat("--> " + message);
            }
        }
    }

    private void SendMessage_Clicked(object sender, EventArgs e)
    {
        try
        {
            using (udpSender = new UdpClient())
            {
                udpSender.Connect(Singleton.Instance.IpAddress, Singleton.Instance.RemotePort);

                udpSender.Send(Encoding.UTF8.GetBytes(Message.Text));

                AddMessageToChat("Сообщение отправлено!");
            }
        }
        catch(Exception ex)
        {
            ExceptionNotify(ex);
        }
    }

    private void AddMessageToChat(string message)
    {
        Chat.Text += message + "\t" + DateTime.Now.ToString("HH:mm:ss") + "\n";
    }
}