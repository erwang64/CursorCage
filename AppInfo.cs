using System.Reflection;

namespace CursorCage;

/// <summary>
/// Version affichée (accueil, barre latérale). Alignée sur <c>&lt;Version&gt;</c> du .csproj
/// — trois segments <c>major.minor.patch</c> (ex. <c>0.1.0</c>), sans numéro de révision.
/// </summary>
public static class AppInfo
{
    public static string DisplayVersion
    {
        get
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var v = asm.GetName().Version;
            if (v is null)
                return "0.1.0";

            var patch = v.Build >= 0 ? v.Build : 0;
            return $"{v.Major}.{v.Minor}.{patch}";
        }
    }
}
