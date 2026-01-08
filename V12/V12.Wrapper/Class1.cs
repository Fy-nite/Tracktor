namespace V12.Wrapper;

public class Info
{
    public static string Name => "V12 Wrapper";
    public static string Description => "This is a wrapper for V12 functionality.";
    public static string Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
}
