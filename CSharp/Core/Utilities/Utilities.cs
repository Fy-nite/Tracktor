using System.Reflection;

namespace Traktor.Core.Utilities;

public class Utilities
{
	/// <summary>
	/// Loads an embedded resource from the assembly's resources.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string? ResourceLoad(string path)
    {
        Assembly asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream($"Traktor.Core.Assets.{path.Replace("/", ".")}");
        if (stream == null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string? ResourceLoad(string path, Assembly assembly)
    {
        using var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{path.Replace("/", ".")}");
        if (stream == null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
