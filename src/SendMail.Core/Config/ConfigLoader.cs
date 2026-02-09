using System;
using System.IO;
using System.Text.Json;

namespace SendMail.Core.Config;

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static (AppConfig? Config, string? Error) LoadFromJson(string json)
    {
        try
        {
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
            if (config is null)
            {
                return (null, "Config JSON parsed to null.");
            }

            return (config, null);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return (null, ex.Message);
        }
    }

    public static (AppConfig? Config, string? Error) LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return (null, "Config path is empty.");
        }

        if (!File.Exists(path))
        {
            return (null, $"Config file not found: {path}");
        }

        try
        {
            var json = File.ReadAllText(path);
            return LoadFromJson(json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (null, ex.Message);
        }
    }
}

