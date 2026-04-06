using System;
using System.Windows;

namespace CursorCage.Services;

public static class TranslationManager
{
    /// <summary>Codes pris en charge ; tout le reste retombe sur l’anglais.</summary>
    public static string NormalizeLanguage(string? languageCode) =>
        languageCode?.Trim().ToLowerInvariant() switch
        {
            "fr" => "fr",
            _ => "en"
        };

    public static void ApplyLanguage(string languageCode)
    {
        var code = NormalizeLanguage(languageCode);
        var dict = new ResourceDictionary();
        try
        {
            dict.Source = new Uri($"/Resources/Lang.{code}.xaml", UriKind.Relative);
        }
        catch
        {
            dict.Source = new Uri("/Resources/Lang.en.xaml", UriKind.Relative);
        }

        // We assume the language dictionary is always the first one in MergedDictionaries
        if (System.Windows.Application.Current.Resources.MergedDictionaries.Count > 0)
        {
            System.Windows.Application.Current.Resources.MergedDictionaries[0] = dict;
        }
        else
        {
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }

    public static string GetString(string key)
    {
        if (System.Windows.Application.Current.TryFindResource(key) is string value)
        {
            return value;
        }
        return key; // return key if missing
    }
}
