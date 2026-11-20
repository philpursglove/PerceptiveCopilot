using System.Text;
using System.Text.Json;

public class DataPreprocessor
{
    private readonly string _basePath;
    public DataPreprocessor(string xwingDataPath)
    {
        _basePath = xwingDataPath;
    }

    public void GenerateXWingData()
    {
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

    }
}
