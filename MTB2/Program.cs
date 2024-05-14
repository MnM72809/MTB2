using System.Diagnostics;
using static MTB2.Debugger;

namespace MTB2;

internal class Program
{
    public static string[]? publicArgs;
    private static void Main(string[] args)
    {

#if DEBUG
        // Set current loglevel to Debug
        currentLevel = LogLevel.Debug;

        Log("---------- Debug mode enabled (source: compiler) ----------", LogLevel.Debug);

        args = Arguments.PromptArguments(args);

        // Add --enableui to args
        args = args.Append("--enableui").ToArray();
#else
        // Handle UI
        Functions.HandleUI(args);
#endif

        publicArgs = args;

        Process currentProcess = Process.GetCurrentProcess();
        currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
        
        // Handle arguments
        Arguments.HandleArguments(args);


        // Check for updates
        Version.HandleUpdate();
    }
}


static class Settings
{
    public const int cursorPos = 6;
}






partial class Version {
    public const string versionString = "0.0.1";
    public static readonly System.Version version = new("0.0.1");
    public const string versionUrl = "https://site-mm.rf.gd/v2/";
}