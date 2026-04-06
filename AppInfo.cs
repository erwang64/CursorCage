using System.Reflection;

namespace CursorCage;

/// <summary>Version affichée (alignée sur <c>Version</c> du .csproj).</summary>
public static class AppInfo
{
    public static string DisplayVersion
    {
        get
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var v = asm.GetName().Version;
            if (v is null)
                return "0.1";
            if (v.Build <= 0 && v.Revision <= 0)
                return $"{v.Major}.{v.Minor}";
            if (v.Revision <= 0)
                return $"{v.Major}.{v.Minor}.{v.Build}";
            return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
        }
    }
}
