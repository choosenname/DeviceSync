using DeviceSync.Messages;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DeviceSync;
using System.Diagnostics;

namespace App2
{
    public partial class MessageWindow : Window
    {
        private System.Diagnostics.Process _process;
        private readonly UdpClient _udpSender;
        private readonly UdpClient _udpReceiver;
        private const int bufferSize = 8 * 1024; // Maximum UDP packet size

        public event EventHandler<Exception> ExceptionNotify;

        private readonly System.Threading.AutoResetEvent waitHandler = new System.Threading.AutoResetEvent(true); // объект-событие

        public MessageWindow()
        {
            InitializeComponent();

            ExceptionNotify += (sender, ex) => MessageBox.Show($"Exception: {ex.Message}\n{ex.Source}\n{ex.StackTrace}", "Exception");

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

                    PackageType messageType = receivedObject["Type"]!.ToObject<PackageType>();
                    switch (messageType)
                    {
                        case PackageType.Text:
                            {
                                StringMessage? textMessage = receivedObject.ToObject<StringMessage>();
                                AddText($"--> {textMessage?.Content}");
                                break;
                            }
                        case PackageType.FileChunk:
                            {
                                AddText("Плюс кусок");
                                waitHandler.Set();
                                FileChunk? fileMessage = receivedObject.ToObject<FileChunk>();
                                await SaveFileAsync(fileMessage);
                                break;
                            }
                        case PackageType.Acknowledge:
                            {
                                AddText("Подтвержден");
                                AcknowledgePackage? acknowledgePackage = receivedObject.ToObject<AcknowledgePackage>();
                                await SendChunkAsync(acknowledgePackage);
                                break;
                            }
                        case PackageType.Command:
                        {
                            StringMessage commandMessage = receivedObject.ToObject<StringMessage>();
                            ExecuteCommand(commandMessage.Content);
                            break;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => ExceptionNotify?.Invoke(this, ex));
            }
        }

        private async Task ExecuteCommand(string command)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                StringBuilder outputBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuilder.AppendLine(e.Data);
                        SendCommandOutput(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuilder.AppendLine(e.Data);
                        SendCommandOutput(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.StandardInput.WriteLine(command);
                process.StandardInput.WriteLine("exit");
                process.WaitForExit();

                string output = outputBuilder.ToString();
                AddText("Command executed!");
                AddText(output);
            }
            catch (Exception ex)
            {
                ExceptionNotify?.Invoke(this, ex);
            }
        }

        private async Task SendCommandOutput(string output)
        {
            if (!string.IsNullOrEmpty(output))
            {
                StringMessage stringMessage = new StringMessage(PackageType.Text, output);
                await SendAsync(stringMessage);
            }
        }


        private async Task SaveFileAsync(FileChunk? file)
        {
            try
            {
                string? fileName = Path.GetFileName(file?.FileName);

                string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string downloadsFolderPath = Path.Combine(folderPath, "Downloads");

                if (fileName != null)
                {
                    string filePathToSave = Path.Combine(downloadsFolderPath, fileName);

                    await using (FileStream fs = new(filePathToSave, FileMode.Append, FileAccess.Write))
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
            Application.Current.Dispatcher.Invoke(() => { Chat.Text += $"{message}\t{DateTime.Now:HH:mm:ss:ffff}\n"; });

        private async void SendFile_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    await SendChunkAsync(new AcknowledgePackage(filePath, 0));
                    AddText("File send successfully!");
                }
            }
            catch (Exception ex)
            {
                ExceptionNotify?.Invoke(this, ex);
            }
        }

        private async Task SendChunkAsync(AcknowledgePackage? package)
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
                Application.Current.Dispatcher.Invoke(() => ExceptionNotify?.Invoke(this, ex));
            }
        }
    }
}
