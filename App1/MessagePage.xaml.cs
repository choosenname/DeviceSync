using DeviceSync.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using DeviceSync;
using static DeviceSync.Singleton;
using System.Threading;

namespace App1
{
    public sealed partial class MessagePage : Page
    {
        private readonly UdpClient _udpSender;
        private readonly UdpClient _udpReceiver;
        private const int bufferSize = 8 * 1024; // Maximum UDP packet size

        public MessagePage()
        {
            InitializeComponent();

                        _udpSender = new UdpClient();
            _udpSender.Connect(Singleton.Instance.IpAddress, Singleton.Instance.RemotePort);

            _udpReceiver = new UdpClient(Instance.LocalPort);

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
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ExceptionNotify?.Invoke(this, ex));
            }
        }

        private async Task SaveFileAsync(FileChunk file)
        {
            try
            {
                string fileName = Path.GetFileName(file.FileName);

                string folderPath = ApplicationData.Current.LocalFolder.Path;

                string filePathToSave = Path.Combine(folderPath, fileName);

                using (FileStream fs = new(filePathToSave, FileMode.Append, FileAccess.Write))
                {
                    await fs.WriteAsync(file.Content, 0, file.Content.Length, CancellationToken.None);

                    var package = new AcknowledgePackage(file.FileName, file.CurrentChunk + 1);

                    await SendAsync(package);
                }

                await Task.Delay(1000);

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

        private async void SendMessage_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringMessage stringMessage = new(PackageType.Text, MessageView.Text);

                await SendAsync(stringMessage);

                AddText("Сообщение отправлено!");
            }
            catch (Exception ex)
            {
                ExceptionNotify?.Invoke(this, ex);
            }
        }

        private void AddText(string message)
        {
            Chat.Text += $"{message}\t{DateTime.Now:HH:mm:ss:ffff}\n";
        }

        private async void SendFile_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add("*");

                var file = await picker.PickSingleFileAsync();

                if (file != null)
                {
                    await SendChunkAsync(new AcknowledgePackage(file.Path, 0));
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
                ulong fileSize = (await (await StorageFile.GetFileFromPathAsync(filePath)).GetBasicPropertiesAsync()).Size; // Size of the file to be sent
                int numChunks = (int)Math.Ceiling((double)fileSize / bufferSize); // Number of chunks needed to send the file

                using (FileStream fs = File.OpenRead(filePath))
                {
                    if (currentChunk < numChunks)
                    {
                        fs.Position = currentChunk * bufferSize;
                        currentChunk++;

                        byte[] buffer = new byte[bufferSize]; // Buffer for the data to be sent

                        while (fs.Position < fs.Length)
                        {
                            int bytesRead = await fs.ReadAsync(buffer, 0, bufferSize);
                            await SendAsync(new FileChunk(PackageType.FileChunk, buffer.Take(bytesRead).ToArray(), filePath, currentChunk, numChunks));
                            ++currentChunk;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ExceptionNotify?.Invoke(this, ex));
            }
        }

        public event EventHandler<Exception> ExceptionNotify;

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            _udpSender?.Close();
            _udpReceiver?.Close();
        }
    }
}
