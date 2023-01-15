using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace DeviceSync;

public partial class MessagePage : ContentPage
{
    // адрес и порт сервера, к которому будем подключаться
    IPAddress ipAddress; // адрес сервера
    int remotePort;
    int localPort;
    static UdpClient udpSender;
    static UdpClient udpReceiver;
    const int packetSize = 8192;
    //static IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, remotePort); // порт сервера

    public MessagePage(IPAddress ipAddress, int remotePort, int localPort)
    {
        InitializeComponent();
        this.ipAddress = ipAddress;
        this.remotePort = remotePort;
        this.localPort = localPort;
    }

    private void ShowEx(Exception ex)
    {
        DisplayAlert("Exception", ex.Message + "\n" + ex.StackTrace, "OK");
    }

    private void Send_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Создаем UdpClient
            UdpClient udpClient = new UdpClient();

            // Соединяемся с удаленным хостом
            udpClient.Connect(ipAddress, remotePort);

            udpClient.Send(new byte[] { 1 }, 1);

            // Отправка простого сообщения
            byte[] bytes = Encoding.UTF8.GetBytes(Message.Text);
            udpClient.Send(bytes, bytes.Length);

            // Закрываем соединение
            udpClient.Close();
        }
        catch (Exception ex)
        {
            ShowEx(ex);
        }
    }

    private void Screenshot_Clicked(object sender, EventArgs e)
    {
        try
        {
            using (udpSender = new UdpClient())
            {
                IPEndPoint sendEndPoint = new IPEndPoint(ipAddress, remotePort);
                IPEndPoint receiveEndPoint = null;

                try
                {
                    udpReceiver = new UdpClient(remotePort);
                }
                catch (Exception ex)
                {
                    ShowEx(ex);
                    return;
                }

                string path = Message.Text;
                using (FileStream fsSource = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    long numBytesToRead = fsSource.Length;
                    long numBytesReaded = 0;
                    string name = Path.GetFileName(path);
                    byte[] packetSend;
                    byte[] packetReceive;

                    packetSend = Encoding.Unicode.GetBytes(name);
                    udpReceiver.Client.ReceiveTimeout = 5000;
                    udpSender.Send(packetSend, packetSend.Length, sendEndPoint);
                    packetReceive = udpReceiver.Receive(ref receiveEndPoint);

                    long parts = fsSource.Length / packetSize;
                    if (fsSource.Length % packetSize != 0) parts++;

                    packetSend = BitConverter.GetBytes(parts);
                    udpSender.Send(packetSend, packetSend.Length, sendEndPoint);
                    packetReceive = udpReceiver.Receive(ref receiveEndPoint);

                    int n = 0;
                    packetSend = new byte[packetSize];
                    for (int i = 0; i < parts - 1; i++)
                    {
                        n = fsSource.Read(packetSend, 0, packetSize);
                        if (n == 0) break;
                        numBytesReaded += n;
                        numBytesToRead -= n;

                        udpSender.Send(packetSend, packetSend.Length, sendEndPoint);
                        packetReceive = udpReceiver.Receive(ref receiveEndPoint);
                    }

                    packetSend = new byte[numBytesToRead];

                    udpSender.Send(packetSend, packetSend.Length, sendEndPoint);
                    packetReceive = udpReceiver.Receive(ref receiveEndPoint);
                }

                udpReceiver.Close();
            }
        }
        catch (Exception ex)
        {
            ShowEx(ex);
        }
        udpSender.Close();
    }

    private void OpenApp_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Создаем UdpClient
            UdpClient udpClient = new UdpClient();

            // Соединяемся с удаленным хостом
            udpClient.Connect(ipAddress, remotePort);

            udpClient.Send(new byte[] { 3 }, 1);

            // Закрываем соединение
            udpClient.Close();
        }
        catch (Exception ex)
        {
            ShowEx(ex);
        }
    }

    private void Command_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Создаем UdpClient
            UdpClient udpClient = new UdpClient();

            // Соединяемся с удаленным хостом
            udpClient.Connect(ipAddress, remotePort);

            udpClient.Send(new byte[] { 4 }, 1);

            byte[] bytes = Encoding.UTF8.GetBytes(Message.Text);
            udpClient.Send(bytes, bytes.Length);

            // Закрываем соединение
            udpClient.Close();
        }
        catch (Exception ex)
        {
            ShowEx(ex);
        }
    }

    private void Recceive_Clicked(object sender, EventArgs e)
    {
        using (udpSender = new UdpClient())
        {
            IPEndPoint receiveEndPoint = null;
            try
            {
                udpReceiver = new UdpClient(localPort);
            }
            catch (Exception ex)
            {
                ShowEx(ex);
            }

            byte[] packetSend = new byte[1];
            byte[] packetReceive;
            packetSend[0] = 7;

            packetReceive = udpReceiver.Receive(ref receiveEndPoint);
            IPEndPoint sendEndPoint = new IPEndPoint(receiveEndPoint.Address, remotePort);

            string name = Encoding.Unicode.GetString(packetReceive);
            udpSender.Connect(ipAddress, remotePort);
            udpSender.Send(packetSend, packetSend.Length);

            udpReceiver.Client.ReceiveTimeout = 5000;
            packetReceive = udpReceiver.Receive(ref receiveEndPoint);

            int parts = BitConverter.ToInt32(packetReceive, 0);
            udpSender.Send(packetSend, packetSend.Length, sendEndPoint);

            using (FileStream fsDest = new FileStream(name, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < parts; i++)
                {
                    packetReceive = udpReceiver.Receive(ref receiveEndPoint);
                    fsDest.Write(packetReceive, 0, packetReceive.Length);
                    udpSender.Send(packetSend, packetSend.Length, sendEndPoint);
                }
            }
            DisplayAlert("Exception", "File received", "OK");
        }
    }

    private void Receive1_Clicked(object sender, EventArgs e)
    {
        UdpClient receivingUdpClient = new UdpClient(localPort);

        IPEndPoint RemoteIpEndPoint = null;

        // Ожидание дейтаграммы
        byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);

        switch (receiveBytes[0])
        {
            case 1:
                {
                    receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                    // Преобразуем и отображаем данные
                    string returnData = Encoding.UTF8.GetString(receiveBytes);
                    Chat.Text += "--> " + returnData.ToString();
                    break;
                }
            case 2:
                {
                    receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                    // Преобразуем и отображаем данные
                    string returnData = Encoding.UTF8.GetString(receiveBytes);
                    Chat.Text += "Скачан файл: " + returnData.ToString();
                    break;
                }
            case 3:
                {
                    Process.Start("CMD.exe", "/C " + "explorer");
                    break;
                }
            case 4:
                {
                    receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                    // Преобразуем и отображаем данные
                    string returnData = Encoding.UTF8.GetString(receiveBytes);
                    Process.Start("CMD.exe", "/C " + returnData);
                    Chat.Text += "Выполнена команда: " + returnData.ToString();
                    break;
                }
        }
        receivingUdpClient.Close();
    }
}