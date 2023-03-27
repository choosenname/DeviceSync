Напиши программу для передачии файлов по TCP в MAUI C# для сохранения данных файлов используй класс 
public class FileMessage
{
    public string Type { get; set; } = "File";
    public byte[] Content { get; set; }
    public string FileName { get; set; }
}
который можешь изменять как угодно
Для реализации передачи файлов по TCP в MAUI C# с использованием класса `FileMessage`, вам нужно создать сервер и клиент. Ниже приведен пример реализации:

1. Класс `FileMessage`:
```csharp
public class FileMessage
{
    public string Type { get; set; } = "File";
    public byte[] Content { get; set; }
    public string FileName { get; set; }
}
```

2. TCP сервер:
```csharp
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

public class TcpFileServer
{
    private readonly int _port;

    public TcpFileServer(int port)
    {
        _port = port;
    }

    public async Task StartAsync()
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();

        while (true)
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var fileMessage = JsonSerializer.Deserialize<FileMessage>(ms.ToArray());
            var filePath = Path.Combine("ReceivedFiles", fileMessage.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            await File.WriteAllBytesAsync(filePath, fileMessage.Content);
        }
    }
}
```

3. TCP клиент:
```csharp
using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;

public class TcpFileClient
{
    private readonly string _serverAddress;
    private readonly int _port;

    public TcpFileClient(string serverAddress, int port)
    {
        _serverAddress = serverAddress;
        _port = port;
    }

    public async Task SendFileAsync(string filePath)
    {
        var fileMessage = new FileMessage
        {
            FileName = Path.GetFileName(filePath),
            Content = await File.ReadAllBytesAsync(filePath)
        };

        var serializedMessage = JsonSerializer.Serialize(fileMessage);

        using var client = new TcpClient();
        await client.ConnectAsync(_serverAddress, _port);
        using var stream = client.GetStream();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(serializedMessage);
        await writer.FlushAsync();
    }
}
```

Теперь вы можете использовать эти классы для передачи файлов по TCP в вашем приложении MAUI. Создайте экземпляры `TcpFileServer` и `TcpFileClient`, и используйте их для отправки и получения файлов.