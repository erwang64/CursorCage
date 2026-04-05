using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CursorCage.Models;

namespace CursorCage.Services;

public sealed class SettingsManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private AppSettings _settings = new();

    public SettingsManager()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CursorCage");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
    }

    public AppSettings Current => _settings;

    public void LoadSettings()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _settings = new AppSettings();
                return;
            }
            var json = File.ReadAllText(_filePath);
            _settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    public void SaveSettings()
    {
        var json = JsonSerializer.Serialize(_settings, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public HotkeyDefinition GetLockHotkey() => _settings.LockHotkey;

    public void SetLockHotkey(HotkeyDefinition def)
    {
        _settings.LockHotkey = def;
    }
}
