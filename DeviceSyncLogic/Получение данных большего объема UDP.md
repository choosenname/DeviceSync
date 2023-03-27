Для получения данных большего объема через UDP, вам нужно учесть, что размер пакета UDP ограничен (обычно до 64 КБ). Вам придется разбить данные на части и передавать их по частям. Вот как это можно сделать:

1. Разбейте данные на части (фрагменты) и добавьте информацию о номере части и общем количестве частей:

```csharp
public class DataFragment
{
    public string Type { get; set; }
    public int PartNumber { get; set; }
    public int TotalParts { get; set; }
    public byte[] Content { get; set; }
}
```

2. Отправьте каждый фрагмент данных отдельно:

```csharp
byte[] largeData = ... // Большие данные для передачи
int maxFragmentSize = 8192; // Размер фрагмента (8 КБ)
int totalParts = (int)Math.Ceiling((double)largeData.Length / maxFragmentSize);

for (int i = 0; i < totalParts; i++)
{
    int currentFragmentSize = Math.Min(maxFragmentSize, largeData.Length - i * maxFragmentSize);
    byte[] fragmentContent = new byte[currentFragmentSize];
    Array.Copy(largeData, i * maxFragmentSize, fragmentContent, 0, currentFragmentSize);

    DataFragment fragment = new DataFragment
    {
        Type = "File",
        PartNumber = i,
        TotalParts = totalParts,
        Content = fragmentContent
    };

    string jsonFragment = JsonConvert.SerializeObject(fragment);
    byte[] dataToSend = Encoding.UTF8.GetBytes(jsonFragment);
    // Отправка dataToSend через UDP
}
```

3. На стороне получателя, соберите все части данных и обработайте их:

```csharp
Dictionary<int, DataFragment> receivedFragments = new Dictionary<int, DataFragment>();
int receivedParts = 0;
int totalParts = 0;

// Внутри обработчика получения данных
byte[] receivedData = ... // Полученные данные
string receivedJson = Encoding.UTF8.GetString(receivedData);
DataFragment receivedFragment = JsonConvert.DeserializeObject<DataFragment>(receivedJson);

if (!receivedFragments.ContainsKey(receivedFragment.PartNumber))
{
    receivedFragments[receivedFragment.PartNumber] = receivedFragment;
    receivedParts++;
    totalParts = receivedFragment.TotalParts;
    
    if (receivedParts == totalParts)
    {
        // Все части получены, собираем данные
        byte[] largeData = new byte[receivedFragments.Values.Sum(f => f.Content.Length)];
        int currentIndex = 0;
        for (int i = 0; i < totalParts; i++)
        {
            Array.Copy(receivedFragments[i].Content, 0, largeData, currentIndex, receivedFragments[i].Content.Length);
            currentIndex += receivedFragments[i].Content.Length;
        }

        // Обработка собранных данных (largeData)
    }
}
```

Таким образом, вы сможете передавать и получать данные большего объема, разбивая их на части и собирая на стороне получателя.