Напиши программу для отправки файлов по UDPClient MAUI C# с подтверждение доставки 
Для отправки файла с использованием протокола UDP в MAUI C# и подтверждением доставки, вам потребуется разделить файл на пакеты и включить информацию о порядке пакетов. Также вам потребуется реализовать механизм подтверждения доставки. Вот пример реализации:

1. Создайте новый проект MAUI C#.
2. Добавьте следующий код в MainPage.xaml.cs:

```csharp
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace UdpFileSender
{
    public partial class MainPage : ContentPage
    {
        private const int BufferSize = 1024;
        private UdpClient _udpClient;
        private IPEndPoint _remoteEndPoint;

        public MainPage()
        {
            InitializeComponent();
            _udpClient = new UdpClient();
        }

        private async void OnSendFileButtonClicked(object sender, EventArgs e)
        {
            string filePath = "path/to/your/file";
            string ipAddress = "127.0.0.1";
            int port = 11000;

            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            await SendFileAsync(filePath);
        }

        private async Task SendFileAsync(string filePath)
        {
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            long fileLength = fs.Length;
            int totalPackets = (int)Math.Ceiling((double)fileLength / BufferSize);
            byte[] buffer = new byte[BufferSize + 8];

            for (int i = 0; i < totalPackets;++)
            {
                int bytesRead = fs.Read(buffer, 8, BufferSize);
                BitConverter.GetBytes(i).CopyTo(buffer, 0);
                BitConverter.GetBytes(totalPackets).CopyTo(buffer, 4);

                await _udpClient.SendAsync(buffer, bytesRead + 8, _remoteEndPoint);
                await Task.Delay(10);

                bool isAcknowledged = await WaitForAcknowledgmentAsync(i);
                while (!isAcknowledged)
                {
                    await _udpClient.SendAsync(buffer, bytesRead + 8, _remoteEndPoint);
                    isAcknowledged = await WaitForAcknowledgmentAsync(i);
                }
            }
        }

        private async Task<bool> WaitForAcknowledgmentAsync(int packetNumber)
        {
            CancellationTokenSource cts = new CancellationTokenSource(1000);
            try
            {
                UdpReceiveResult result = await _udpClient.ReceiveAsync().WithCancellation(cts.Token);
                string ackMessage = Encoding.ASCII.GetString(result.Buffer);
                int ackNumber = int.Parse(ackMessage.Split(':')[1]);

                if (ackNumber == packetNumber)
                {
                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout, no acknowledgment received
            }

            return false;
        }
    }
}
```

3. Замените "path/to/your/file" на путь к файлу, который вы хотите отправить, и укажите IP-адрес и порт получателя.

Помимо этого,ам также нужно будет создать приложение-получатель, которое будет принимать пакеты, собирать их в файл, а также отправлять подтверждения доставки.

Напиши код для получателя 
Для создания приложения-получателя, которое будет принимать пакеты, собирать их в файл и отправлять подтверждения доставки, выполните следующие шаги:

1. Создайте новый проект MAUI C#.
2. Добавьте следующий код в MainPage.xaml.cs:

```csharp
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace UdpFileReceiver
{
    public partial class MainPage : ContentPage
    {
        private const int BufferSize = 1024;
        private UdpClient _udpClient;
        private IPEndPoint _remoteEndPoint;
        private ConcurrentDictionary<int, byte[]> _receivedPackets;

        public MainPage()
        {
            InitializeComponent();
            _udpClient = new UdpClient(11000);
            _receivedPackets = new ConcurrentDictionary<int, byte[]>();
            _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            StartListening();
        }

        private async void StartListening()
        {
            while (true)
            {
                UdpReceiveResult result = await _udpClient.ReceiveAsync();
                byte[] data = result.Buffer;
                _remoteEndPoint = result.RemoteEndPoint;

                int packetNumber = BitConverter.ToInt32(data, 0);
                int totalPackets = BitConverter.ToInt32(data, 4);

                byte[] payload = new byte[data.Length - 8];
                Array.Copy(data, 8, payload, 0, payload.Length);
                _receivedPackets.TryAdd(packetNumber, payload);

                await SendAcknowledgmentAsync(packetNumber);

                if (_receivedPackets.Count == totalPackets)
                {
                    SaveReceivedFile("received_file.ext");
                    _receivedPackets.Clear();
                }
            }
        }

        private async Task SendAcknowledgmentAsync(int packetNumber)
        {
            string ackMessage = $"ACK:{packetNumber}";
            byte[] ackData = Encoding.ASCII.GetBytes(ackMessage);
            await _udpClient.SendAsync(ackData, ackData.Length, _remoteEndPoint);
        }

        private void SaveReceivedFile(string filePath)
        {
            using FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);

            for (int i = 0; i < _receivedPackets.Count; i++)
            {
                if (_receivedPackets.TryGetValue(i, out byte[] packetData))
                {
                    fs.Write(packetData, 0, packetData.Length);
                }
            }
        }
    }
}
```

3. Замените "received_file.ext" на путь и имя файла, который вы хотите сохранить.

После этого у вас будет готово приложение-получатель, которое принимает пакеты, собирает их в файл и отправляет подтверждения доставки.