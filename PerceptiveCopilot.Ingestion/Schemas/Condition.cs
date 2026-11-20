using System.Text.Json.Serialization;

public class Condition
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("limited")]
    public int Limited { get; set; }
    [JsonPropertyName("ability")]
    public required string Ability { get; set; }
}
