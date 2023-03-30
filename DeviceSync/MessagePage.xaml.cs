using DeviceSync.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeviceSync;

public partial class MessagePage : ContentPage
{
    private readonly UdpClient _udpSender;
    private readonly UdpClient _udpReceiver;
    private const int bufferSize = 8 * 1024; // Maximum UDP packet size

    public event EventHandler<Exception> ExceptionNotify;
    AutoResetEvent waitHandler = new AutoResetEvent(true);  // объект-событие

    public MessagePage()
    {
        InitializeComponent();

        ExceptionNotify += (sender, ex) => DisplayAlert("Exception", ex.Message + ex.Source + ex.StackTrace, "OK");

        _udpSender = new UdpClient();
        _udpSender.Connect(Singleton.Instance.IpAddress, Singleton.Instance.RemotePort);

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
                            AddMessageToChat("Плюс кусок");
                            waitHandler.Set();
                            FileChunk fileMessage = receivedObject.ToObject<FileChunk>();
                            await SaveFile(fileMessage);
                            break;
                        }

                    case PackageType.Acknowledge:
                        {
                            AddMessageToChat("Подтвержден");
                            AcknowledgePackage acknowledgePackage = receivedObject.ToObject<AcknowledgePackage>();
                            await SendChunk(acknowledgePackage);
                            break;
                        }
                }
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Dispatch(() => ExceptionNotify?.Invoke(this, ex));
        }
    }

    /*private void ResendAcknowledge(AcknowledgePackage acknowledgePackage)
    {
        if (!waitHandler.WaitOne(1000))
        {
            AddMessageToChat("Переотправил");
            SendPackage(acknowledgePackage);
        }
    }*/

    private async Task SaveFile(FileChunk fileMessage)
    {
        try
        {
            string fileName = Path.GetFileName(fileMessage.FileName);
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

#if __ANDROID__
    var downloadsDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
    folderPath = downloadsDir.AbsolutePath;
#endif

            // Путь к файлу, который нужно сохранить
            string filePathToSave = Path.Combine(folderPath, fileName);

            using (var fileStream = new FileStream(filePathToSave, FileMode.Append, FileAccess.Write))
            {
                await fileStream.WriteAsync(fileMessage.Content.AsMemory(0, fileMessage.Content.Length));
            }

            var package = new AcknowledgePackage(fileMessage.FileName, fileMessage.CurrentChunk);

            SendPackage(package);

            await Task.Delay(1000);
            if (!waitHandler.WaitOne(0))
            {
                AddMessageToChat("Переотправил");
                SendPackage(package);
            }

            AddMessageToChat($"Файл {fileName} сохранен! {filePathToSave}");
        }
        catch (Exception ex)
        {
            ExceptionNotify?.Invoke(this, ex);
        }
    }

    private void SendPackage(Message package)
    {
        string jsonFragment = JsonConvert.SerializeObject(package);
        byte[] dataToSend = Encoding.UTF8.GetBytes(jsonFragment);

        _udpSender.Send(dataToSend);
    }

    private void SendMessage_Clicked(object sender, EventArgs e)
    {
        try
        {
            StringMessage stringMessage = new(PackageType.Text, Message.Text);

            SendPackage(stringMessage);

            AddMessageToChat("Сообщение отправлено!");
        }
        catch (Exception ex)
        {
            ExceptionNotify?.Invoke(this, ex);
        }
    }

    private void AddMessageToChat(string message)
    {
        Dispatcher.Dispatch(() =>
        {
            Chat.Text += message + "\t" + DateTime.Now.ToString("HH:mm:ss:ffff") + "\n";
        });
    }

    private async void SendFile_Clicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync();

            if (result != null)
            {
                await SendChunk(new AcknowledgePackage(result.FullPath, 0));
                AddMessageToChat("File sent successfully!");
            }
        }
        catch (Exception ex)
        {
            ExceptionNotify?.Invoke(this, ex);
        }
    }

    private Task SendChunk(AcknowledgePackage package)
    {
        return Task.Run(() =>
        {
            try
            {
                string filePath = package.Content;
                int currentChunk = package.CurrentChunk; // Current chunk being sent
                byte[] buffer = new byte[bufferSize]; // Buffer for the data to be sent

                long fileSize = new FileInfo(filePath).Length; // Size of the file to be sent
                int numChunks = (int)Math.Ceiling((double)fileSize / bufferSize); // Number of chunks needed to send the file
                if (currentChunk < numChunks)
                {
                    using FileStream fs = File.OpenRead(filePath);
                    fs.Position = currentChunk * bufferSize;

                    currentChunk++;

                    int bytesRead = fs.Read(buffer, 0, buffer.Length);
                    FileChunk fileMessage = new(PackageType.FileChunk, buffer.Take(bytesRead).ToArray(), filePath, currentChunk, numChunks);

                    SendPackage(fileMessage);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Dispatch(() => ExceptionNotify?.Invoke(this, ex));
            }
        });
    }

    /*private void SendFile(string filePath)
    {
        try
        {
            const int bufferSize = 65507; // Maximum UDP packet size
            byte[] buffer = new byte[bufferSize]; // Buffer for the data to be sent
            long fileSize = new FileInfo(filePath).Length; // Size of the file to be sent
            int numChunks = (int)Math.Ceiling((double)fileSize / bufferSize); // Number of chunks needed to send the file
            int currentChunk = 0; // Current chunk being sent

            using FileStream fs = File.OpenRead(filePath);

            while (currentChunk < numChunks)
            {
                int bytesRead = fs.Read(buffer, 0, buffer.Length);
                FileChunk fileMessage = new(PackageType.FileChunk, buffer.Take(bytesRead).ToArray(), Path.GetFileName(filePath), currentChunk, numChunks);

                string jsonFragment = JsonConvert.SerializeObject(fileMessage);
                byte[] dataToSend = Encoding.UTF8.GetBytes(jsonFragment);
                _udpSender.Send(dataToSend);

                currentChunk++;
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Dispatch(() => ExceptionNotify?.Invoke(this, ex));
        }
    }*/
}