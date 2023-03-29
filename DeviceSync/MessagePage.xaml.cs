using DeviceSync.Messages;
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

        ExceptionNotify += (sender, ex) => DisplayAlert("Exception", ex.ToString(), "OK");

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
                            BytesMessage fileMessage = receivedObject.ToObject<BytesMessage>();
                            SaveFile(fileMessage);
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
    private async void SaveFile(BytesMessage fileMessage)
    {
        try
        {
            string fileName = Path.GetFileName(fileMessage.FileName);
            string systemDownloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            // Путь к файлу, который нужно сохранить
            string filePathToSave = Path.Combine(systemDownloadsPath, fileName);

            using (var fileStream = new FileStream(filePathToSave, FileMode.Create, FileAccess.Write))
            {
                await fileStream.WriteAsync(fileMessage.Content, 0, fileMessage.Content.Length);
            }

            AddMessageToChat($"Файл {fileName} сохранен!");
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
            StringMessage stringMessage = new(PackageType.Text, Message.Text);

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

    private async void SendFile_Clicked(object sender, EventArgs e)
    {
        FileResult result = await FilePicker.PickAsync();

        if (result != null)
        {
            string filePath = result.FullPath;

            // Open the file and read its contents
            using (FileStream fs = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[1024]; // Buffer for the data to be sent
                int bytesRead = 0; // Number of bytes read from the file

                // Loop through the file and send its contents in chunks
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Create a new BytesMessage to send the chunk of file
                    BytesMessage fileMessage = new BytesMessage(PackageType.FileChunk, buffer.Take(bytesRead).ToArray(), Path.GetFileName(filePath));

                    // Convert the message to a JSON string
                    string jsonFragment = JsonConvert.SerializeObject(fileMessage);

                    // Convert the JSON string to a byte array
                    byte[] dataToSend = Encoding.UTF8.GetBytes(jsonFragment);

                    // Display a message to the user to indicate that the packet was sent successfully
                    AddMessageToChat("Packet sent successfully.");
                }

                // Display a message to the user to indicate that the file was sent successfully
                AddMessageToChat("File sent successfully!");
            }
        }
    }

}