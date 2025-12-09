using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Beep.Python.RuntimeEngine.Infrastructure;

public class ModelCatalogEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonPropertyName("modelType")]
    public string ModelType { get; set; } = "SLM";

    [JsonPropertyName("huggingFaceId")]
    public string HuggingFaceId { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;

    [JsonPropertyName("ramRequired")]
    public string RamRequired { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("maxContextLength")]
    public int MaxContextLength { get; set; }

    [JsonPropertyName("quantization")]
    public string Quantization { get; set; } = string.Empty;

    [JsonPropertyName("languages")]
    public List<string> Languages { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}

public class ModelCatalogRoot
{
    [JsonPropertyName("models")]
    public List<ModelCatalogEntry> Models { get; set; } = new();
}

public interface IModelCatalog
{
    List<ModelCatalogEntry> GetAllModels();
    List<string> GetModelIds();
    ModelCatalogEntry? GetModel(string id);
}

public class ModelCatalog : IModelCatalog
{
    private readonly List<ModelCatalogEntry> _models;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ModelCatalog()
    {
        _models = LoadModels();
    }

    private List<ModelCatalogEntry> LoadModels()
    {
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "models.json");
            if (!File.Exists(configPath))
            {
                // Fallback to default models if file doesn't exist
                return GetDefaultModels();
            }

            var json = File.ReadAllText(configPath);
            var root = JsonSerializer.Deserialize<ModelCatalogRoot>(json, JsonOptions);
            return root?.Models ?? GetDefaultModels();
        }
        catch
        {
            return GetDefaultModels();
        }
    }

    private List<ModelCatalogEntry> GetDefaultModels()
    {
        return new List<ModelCatalogEntry>
        {
            new()
            {
                Id = "dialogpt-small",
                HuggingFaceId = "microsoft/DialoGPT-small",
                Name = "DialoGPT Small",
                Description = "CPU-optimized fastest"
            },
            new()
            {
                Id = "phi-2",
                HuggingFaceId = "microsoft/phi-2",
                Name = "Microsoft Phi-2",
                Description = "CPU-optimized"
            }
        };
    }

    public List<ModelCatalogEntry> GetAllModels() => _models;

    public List<string> GetModelIds() => _models.Select(m => m.HuggingFaceId).ToList();

    public ModelCatalogEntry? GetModel(string id)
    {
        return _models.FirstOrDefault(m =>
            m.Id.Equals(id, StringComparison.OrdinalIgnoreCase) ||
            m.HuggingFaceId.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}
