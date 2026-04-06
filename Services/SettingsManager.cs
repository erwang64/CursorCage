using System;
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
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly string _filePath;
    private AppSettings _settings = new();

    /// <summary>
    /// Chemin du fichier JSON (affichage / debug). Toujours sous LocalApplicationData après migration.
    /// </summary>
    public string SettingsFilePath => _filePath;

    public SettingsManager()
    {
        var localDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CursorCage");
        var roamingDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CursorCage");

        _filePath = Path.Combine(localDir, "settings.json");
        Directory.CreateDirectory(localDir);

        // Ancien emplacement : %AppData%\Roaming\CursorCage — copier vers Local si besoin
        var legacyPath = Path.Combine(roamingDir, "settings.json");
        try
        {
            if (!File.Exists(_filePath) && File.Exists(legacyPath))
                File.Copy(legacyPath, _filePath, overwrite: false);
        }
        catch
        {
            // migration best-effort
        }
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
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            NormalizeSettings(loaded);
            _settings = loaded;
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    public bool TrySaveSettings(out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            NormalizeSettings(_settings);
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(_filePath))
                File.Replace(tempPath, _filePath, destinationBackupFileName: null);
            else
                File.Move(tempPath, _filePath);

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public void SaveSettings() => TrySaveSettings(out _);

    public HotkeyDefinition GetLockHotkey()
    {
        if (_settings.LockHotkey is null)
            _settings.LockHotkey = CloneHotkey(HotkeyDefinition.Default);
        return _settings.LockHotkey;
    }

    public void SetLockHotkey(HotkeyDefinition def)
    {
        _settings.LockHotkey = def;
    }

    private static void NormalizeSettings(AppSettings s)
    {
        if (s.LockHotkey is null)
            s.LockHotkey = CloneHotkey(HotkeyDefinition.Default);
    }

    private static HotkeyDefinition CloneHotkey(HotkeyDefinition h) =>
        new() { Modifiers = h.Modifiers, VirtualKey = h.VirtualKey };
}
