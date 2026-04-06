using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace CursorCage.Services;

/// <summary>
/// Vérifie les releases GitHub et propose le téléchargement de l’installateur.
/// Attachez à chaque release un exécutable nommé de préférence <c>CursorCage-Setup.exe</c>.
/// </summary>
public sealed class GitHubUpdateService : IDisposable
{
    private const string Owner = "erwang64";
    private const string Repo = "CursorCage";
    private const string PreferredAssetName = "CursorCage-Setup.exe";

    private static readonly Uri LatestReleaseApi = new($"https://api.github.com/repos/{Owner}/{Repo}/releases/latest");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(45) };
    private readonly UIManager _ui;
    private readonly SettingsManager _settings;
    private string? _sessionNotifiedTag;
    private Window? _dialogOwner;

    public GitHubUpdateService(UIManager ui, SettingsManager settings)
    {
        _ui = ui;
        _settings = settings;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "CursorCage-update-check (+https://github.com/erwang64/CursorCage)");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public void SetDialogOwner(Window? window) => _dialogOwner = window;

    public void ScheduleStartupCheck()
    {
        if (ShouldCheckOnStartup() is false)
            return;

        _ = CheckOnStartupAsync();
    }

    private bool ShouldCheckOnStartup() => _settings.Current.CheckForUpdatesOnStartup != false;

    private async Task CheckOnStartupAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(4));
            var result = await FetchLatestReleaseAsync();
            if (result is not { } r || r.TagName is null)
                return;

            var remote = ParseVersionLoose(r.TagName);
            if (remote is null)
                return;

            var local = GetRunningVersion();
            if (remote <= local)
                return;

            if (_sessionNotifiedTag == r.TagName)
                return;
            _sessionNotifiedTag = r.TagName;

            var url = PickInstallerUrl(r) ?? r.HtmlUrl;
            if (string.IsNullOrEmpty(url))
                return;

            _ui.ShowUpdateAvailableBalloon(remote.ToString(), url);
        }
        catch
        {
            // silencieux au démarrage
        }
    }

    public async Task CheckManuallyAsync()
    {
        try
        {
            var result = await FetchLatestReleaseAsync();
            if (result is null)
            {
                ShowMb(TranslationManager.GetString("StrUpdateCheckFailed"), MessageBoxImage.Warning);
                return;
            }

            var remote = ParseVersionLoose(result.TagName);
            if (remote is null)
            {
                ShowMb(TranslationManager.GetString("StrUpdateNoVersionTag"), MessageBoxImage.Warning);
                return;
            }

            var local = GetRunningVersion();
            if (remote <= local)
            {
                ShowMb(
                    string.Format(TranslationManager.GetString("StrUpdateAlreadyLatestFmt"), local),
                    MessageBoxImage.Information);
                return;
            }

            var downloadUrl = PickInstallerUrl(result);
            var openUrl = downloadUrl ?? result.HtmlUrl;
            if (string.IsNullOrEmpty(openUrl))
            {
                ShowMb(TranslationManager.GetString("StrUpdateNoInstallerAsset"), MessageBoxImage.Warning);
                return;
            }

            var notes = string.IsNullOrWhiteSpace(result.Body)
                ? string.Empty
                : Truncate(result.Body.Trim(), 900);
            var msg = string.Format(
                TranslationManager.GetString("StrUpdateAvailablePromptFmt"),
                remote,
                local,
                Environment.NewLine,
                string.IsNullOrEmpty(notes) ? "—" : notes);

            var want = System.Windows.MessageBox.Show(
                _dialogOwner,
                msg,
                TranslationManager.GetString("StrUpdateAvailableTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (want != MessageBoxResult.Yes)
                return;

            if (downloadUrl is not null)
                await DownloadAndLaunchInstallerAsync(downloadUrl, remote.ToString());
            else
                OpenInBrowser(openUrl);
        }
        catch (Exception ex)
        {
            ShowMb(
                string.Format(TranslationManager.GetString("StrUpdateErrorFmt"), ex.Message),
                MessageBoxImage.Error);
        }
    }

    private void ShowMb(string text, MessageBoxImage icon) =>
        System.Windows.MessageBox.Show(_dialogOwner, text, TranslationManager.GetString("StrAppTitle"), MessageBoxButton.OK, icon);

    private async Task<GitHubReleaseDto?> FetchLatestReleaseAsync()
    {
        using var response = await _http.GetAsync(LatestReleaseApi);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<GitHubReleaseDto>(stream, JsonOptions);
    }

    private static string? PickInstallerUrl(GitHubReleaseDto r)
    {
        var assets = r.Assets;
        if (assets is null || assets.Count == 0)
            return null;

        foreach (var a in assets)
        {
            if (a is { Name: { } name, BrowserDownloadUrl: { } url }
                && name.Equals(PreferredAssetName, StringComparison.OrdinalIgnoreCase))
                return url;
        }

        foreach (var a in assets)
        {
            if (a is { Name: { } n1, BrowserDownloadUrl: { } u1 }
                && n1.EndsWith("Setup.exe", StringComparison.OrdinalIgnoreCase))
                return u1;
        }

        foreach (var a in assets)
        {
            if (a is { Name: { } n2, BrowserDownloadUrl: { } u2 }
                && n2.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                && n2.Contains("CursorCage", StringComparison.OrdinalIgnoreCase))
                return u2;
        }

        foreach (var a in assets)
        {
            if (a is { Name: { } n3, BrowserDownloadUrl: { } u3 } && n3.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return u3;
        }

        return null;
    }

    private async Task DownloadAndLaunchInstallerAsync(string assetUrl, string versionLabel)
    {
        var safe = new string(versionLabel.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
        var path = Path.Combine(Path.GetTempPath(), $"CursorCage-update-{safe}.exe");
        try
        {
            using var response = await _http.GetAsync(assetUrl);
            response.EnsureSuccessStatusCode();
            await using var fs = File.Create(path);
            await response.Content.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            ShowMb(
                string.Format(TranslationManager.GetString("StrUpdateDownloadFailedFmt"), ex.Message),
                MessageBoxImage.Error);
            return;
        }

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        ShowMb(TranslationManager.GetString("StrUpdateCloseAppHint"), MessageBoxImage.Information);
    }

    private static void OpenInBrowser(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private static Version GetRunningVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(info))
        {
            var core = info.Split('+')[0];
            var dash = core.IndexOf('-');
            if (dash > 0)
                core = core[..dash];
            if (Version.TryParse(core, out var v))
                return NormalizeVersion(v);
        }

        return NormalizeVersion(asm.GetName().Version ?? new Version(0, 0));
    }

    /// <summary>Normalise les composants -1 de <see cref="Version"/> (ex. 1.2 seul).</summary>
    private static Version NormalizeVersion(Version v)
    {
        var build = v.Build >= 0 ? v.Build : 0;
        var rev = v.Revision >= 0 ? v.Revision : 0;
        return new Version(v.Major, v.Minor, build, rev);
    }

    private static Version? ParseVersionLoose(string? tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return null;
        var s = tagName.Trim().TrimStart('v', 'V');
        var dash = s.IndexOf('-');
        if (dash >= 0)
            s = s[..dash];
        return Version.TryParse(s, out var v) ? NormalizeVersion(v) : null;
    }

    private static string Truncate(string s, int max)
    {
        if (s.Length <= max)
            return s;
        return s[..max] + "…";
    }

    public void Dispose() => _http.Dispose();
}

internal sealed class GitHubReleaseDto
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubAssetDto>? Assets { get; set; }
}

internal sealed class GitHubAssetDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
}
