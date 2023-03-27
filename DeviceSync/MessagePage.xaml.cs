using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeviceSync;

public partial class MessagePage : ContentPage
{
    private readonly UdpClient _udpSender;
    private readonly UdpClient _udpReceiver;

    public event EventHandler<Exception> ExceptionNotify;

    public MessagePage()
    {
        InitializeComponent();

        ExceptionNotify += (sender, ex) => DisplayAlert("Exception", ex.Message + "\n" + ex.StackTrace, "OK");

        _udpSender = new UdpClient();
        _udpReceiver = new UdpClient(Singleton.Instance.LocalPort);

        Receiver();
    }

    private async void Receiver()
    {
        try
        {
            while (true)
            {
                var result = await _udpReceiver.ReceiveAsync();
                var receivedJson = Encoding.UTF8.GetString(result.Buffer);
                JObject receivedObject = JObject.Parse(receivedJson);
                PackageType messageType = receivedObject["Type"].ToObject<PackageType>();

                switch (messageType)
                {
                    case PackageType.Text:
                        {
                            StringMessage textMessage = receivedObject.ToObject<StringMessage>();
                            AddMessageToChat("--> " + textMessage.Content);
                            break;
                        }

                    case PackageType.FileChunk:
                        {
                            FileChunk fileMessage = receivedObject.ToObject<FileChunk>();



                            break;
                        }
                }
            }
        }
        catch (Exception ex)
        {
            ExceptionNotify?.Invoke(this, ex);
        }
    }

    private void SendMessage_Clicked(object sender, EventArgs e)
    {
        try
        {
            StringMessage stringMessage = new StringMessage(PackageType.Text, Message.Text);

            _udpSender.Connect(Singleton.Instance.IpAddress, Singleton.Instance.RemotePort);

            string jsonFragment = JsonConvert.SerializeObject(stringMessage);
            byte[] dataToSend = Encoding.UTF8.GetBytes(jsonFragment);

            _udpSender.Send(dataToSend);

            AddMessageToChat("Сообщение отправлено!");
        }
        catch (Exception ex)
        {
            ExceptionNotify?.Invoke(this, ex);
        }
    }

    private void AddMessageToChat(string message)
    {
        Chat.Text += message + "\t" + DateTime.Now.ToString("HH:mm:ss:ffff") + "\n";
    }

    private void SendFile_Clicked(object sender, EventArgs e)
    {
        try
        {
            /*FileResult result = await FilePicker.PickAsync();

            if (result != null)
            {
                string filePath = result.FullPath;
                AddMessageToChat("Путь: " + filePath);
            }*/

            string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string fileName = "example.txt";
            string filePath = Path.Combine(downloadsFolder, fileName);
            File.WriteAllText(filePath, "Пример текста для записи в файл.");
            AddMessageToChat(downloadsFolder);
        }
        catch (Exception ex)
        {
            ExceptionNotify?.Invoke(this, ex);
        }
    }
}