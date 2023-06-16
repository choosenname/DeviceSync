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

    private readonly AutoResetEvent waitHandler = new AutoResetEvent(true); // объект-событие

    public MessagePage()
    {
        InitializeComponent();

        ExceptionNotify += (sender, ex) => DisplayAlert("Exception", ex.Message + ex.Source + ex.StackTrace, "OK");

        _udpSender = new UdpClient();
        _udpSender.Connect(Singleton.Instance.IpAddress, Singleton.Instance.RemotePort);

        _udpReceiver = new UdpClient(Singleton.Instance.LocalPort);

        ReceiverAsync().ConfigureAwait(false);
    }

    private async Task ReceiverAsync()
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
                            AddText($"--> {textMessage.Content}");
                            break;
                        }
                    case PackageType.FileChunk:
                        {
                            AddText("Плюс кусок");
                            waitHandler.Set();
                            FileChunk fileMessage = receivedObject.ToObject<FileChunk>();
                            await SaveFileAsync(fileMessage);
                            break;
                        }
                    case PackageType.Acknowledge:
                        {
                            AddText("Подтвержден");
                            AcknowledgePackage acknowledgePackage = receivedObject.ToObject<AcknowledgePackage>();
                            await SendChunkAsync(acknowledgePackage);
                            break;
                        }
                }
            }
        }
        catch (Exception ex)
        {
            Device.BeginInvokeOnMainThread(() => ExceptionNotify?.Invoke(this, ex));
        }
    }

    private async Task SaveFileAsync(FileChunk file)
    {
        try
        {
            string fileName = Path.GetFileName(file.FileName);

#if __ANDROID__
            var downloadsDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
            string folderPath = downloadsDir.AbsolutePath;

#else
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

#endif


            string filePathToSave = Path.Combine(folderPath, fileName);

            using (FileStream fs = new(filePathToSave, FileMode.Append, FileAccess.Write))
            {
                await fs.WriteAsync(file.Content.AsMemory(0, file.Content.Length));

                var package = new AcknowledgePackage(file.FileName, file.CurrentChunk + 1);
                await SendAsync(package);
            }

            await Task.Delay(250);

            if (!waitHandler.WaitOne(0))
            {
                AddText($"Переотправил");
                var package = new AcknowledgePackage(file.FileName, file.CurrentChunk + 1);
                await SendAsync(package);
            }

            AddText($"Файл {fileName} сохранен! {filePathToSave}");
        }
        catch (Exception ex)
        {
            ExceptionNotify?.Invoke(this, ex);
        }
    }

    private async Task SendAsync(Message package)
    {
        string jsonFragment = JsonConvert.SerializeObject(package);
        byte[] dataToSend = Encoding.UTF8.GetBytes(jsonFragment);
        await _udpSender.SendAsync(dataToSend, dataToSend.Length);
    }

    private async void SendMessage_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(Message.Text))
            {
                if (Message.Text.StartsWith("/"))
                {
                    string command = Message.Text.TrimStart('/');
                    StringMessage commandMessage = new StringMessage(PackageType.Command, command);
                    await SendAsync(commandMessage);
                    AddText("Console Command sent!");
                }
                else
                {
                    StringMessage stringMessage = new StringMessage(PackageType.Text, Message.Text);
                    await SendAsync(stringMessage);
                    AddText("Message sent!");
                }
            }
        }
        catch (Exception ex)
        {
            ExceptionNotify?.Invoke(this, ex);
        }
    }

    private void AddText(string message) =>
        Device.BeginInvokeOnMainThread(() => { Chat.Text += $"{message}\t{DateTime.Now:HH:mm:ss:ffff}\n"; });

    private async void SendFile_Clicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync();

            if (result != null)
            {
                await SendChunkAsync(new AcknowledgePackage(result.FullPath, 0));
                AddText("File send successfully!");
            }
        }
        catch (Exception ex)
        {
            ExceptionNotify?.Invoke(this, ex);
        }
    }

    private async Task SendChunkAsync(AcknowledgePackage package)
    {
        try
        {
            string filePath = package.Content;
            int currentChunk = package.CurrentChunk; // Current chunk being sent
            long fileSize = new FileInfo(filePath).Length; // Size of the file to be sent
            int numChunks = (int)Math.Ceiling((double)fileSize / bufferSize); // Number of chunks needed to send the file

            using FileStream fs = File.OpenRead(filePath);

            if (currentChunk < numChunks)
            {
                fs.Position = currentChunk * bufferSize;
                currentChunk++;

                byte[] buffer = new byte[bufferSize]; // Buffer for the data to be sent

                while (fs.Position < fs.Length)
                {
                    int bytesRead = await fs.ReadAsync(buffer.AsMemory(0, bufferSize));
                    await SendAsync(new FileChunk(PackageType.FileChunk, buffer.Take(bytesRead).ToArray(), filePath, currentChunk, numChunks));
                    ++currentChunk;
                }
            }
        }
        catch (Exception ex)
        {
            Device.BeginInvokeOnMainThread(() => ExceptionNotify?.Invoke(this, ex));
        }
    }
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

/*private void ResendAcknowledge(AcknowledgePackage acknowledgePackage)
{
    if (!waitHandler.WaitOne(1000))
    {
        AddMessageToChat("Переотправил");
        SendPackage(acknowledgePackage);
    }
}*/
