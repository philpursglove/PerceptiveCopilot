using System.Text;
using System.Text.Json;

public class DataPreprocessor
{
    private readonly string _basePath;
    public DataPreprocessor(string xwingDataPath)
    {
        _basePath = xwingDataPath;
    }

    public List<string> GenerateXWingData()
    {
        var filePaths = new List<string>();

        var filePath = Path.Combine(_basePath, "conditions\\conditions.json");
        var conditionsJson = File.ReadAllText(filePath);
        var conditions = JsonSerializer.Deserialize<Condition[]>(conditionsJson);

        var conditionsStringBuilder = new StringBuilder();
        foreach (var condition in conditions.OrderBy(c => c.Name))
        {
            conditionsStringBuilder.AppendLine(
                $"{condition.Name} - Limited {condition.Limited} - {condition.Ability}");
        }

        var tempPath = Path.GetTempPath();
        var textFilePath = Path.Combine(tempPath, "conditions.txt");
        File.WriteAllText(textFilePath, conditionsStringBuilder.ToString());
        filePaths.Add(textFilePath);

        filePath = Path.Combine(_basePath, "pilots\\first-order\\tie-bainterceptor.json");
        var pilotsJson = File.ReadAllText(filePath);
        var pilots = JsonSerializer.Deserialize<Pilot[]>(pilotsJson);

        return filePaths;
    }
}
