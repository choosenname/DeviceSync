using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;

namespace DeviceSync.Application.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private const int Port = 30304;
    static readonly UdpClient UdpClient = new();


    public MainWindowViewModel()
    {
        UdpClient.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
        Task.Run(ReceiveAllMessages);
        SendRequestsCommand = ReactiveCommand.Create(SendRequests);
    }

    public ObservableCollection<string> ReceivedMessages { get; } = new();

    public ICommand SendRequestsCommand { get; }

    private void ReceiveAllMessages()
    {
        var from = new IPEndPoint(0, 0);
        var task = Task.Run(() =>
        {
            while (true)
            {
                var recvBuffer = UdpClient.Receive(ref from);
                ReceivedMessages.Add(Encoding.UTF8.GetString(recvBuffer));
            }
        });
    }

    private void SendRequests()
    {
        var data = Encoding.UTF8.GetBytes("ABCD");
        UdpClient.Send(data, data.Length, "255.255.255.255", Port);
    }
}