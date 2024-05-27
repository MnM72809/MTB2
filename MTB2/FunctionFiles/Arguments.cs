using System.Diagnostics;
using static MTB2.Debugger;

namespace MTB2;
internal class Arguments
{
    public static void HandleArguments(string[] args)
    {
        // Convert arguments to lowercase
        args = args.Select(x => x.ToLower()).ToArray();

        // Check for arguments
        if (args.Length == 0)
        {
            Log("No arguments provided", LogLevel.Info);
            return;
        }

        // Switch through arguments
        Log("\nSwitching through arguments", LogLevel.Debug);
        foreach (string arg in args)
        {
            if (Functions.uiArgs.Contains(arg))
            {
                Log("UI argument provided", LogLevel.Debug);
            }
            else
            {
                HandleArgument(arg);
            }
        }

        Log("Finished switching through arguments\n", LogLevel.Debug);
    }

    private static void HandleArgument(string arg)
    {
        switch (arg)
        {
            case "-h":
            case "--help":
                Log("Help requested", LogLevel.Info);
                break;
            case "-v":
            case "--version":
                Log("Version requested", LogLevel.Debug);
                Log($"---------- Version {Version.versionString} (source: arguments) ----------", LogLevel.Info, force: true);
                break;
            case "-d":
            case "--debug":
                Log("Debug mode requested", LogLevel.Debug);
                currentLevel = LogLevel.Debug;
                Log("---------- Debug mode enabled (source: arguments) ----------", LogLevel.Debug);
                break;
            case "--forceupdate":
            case "--forceinstall":
            case "--update":
            case "--install":
            case "-u":
                Log("Update requested");
                Version.HandleUpdate(true);
                break;
            case "--finishupdate":
            case "--finishinstall":
                Log("Finishing update...", LogLevel.Info);
                Version.FinishUpdate();
                Log("Update finished", LogLevel.Info);
                break;
            case "--setpriority":
            case "--normalpriority":
            case "--priority":
            case "--setprocesspriority":
            case "--normalprocesspriority":
            case "--processpriority":
                Process currentProcess = Process.GetCurrentProcess();
                currentProcess.PriorityClass = ProcessPriorityClass.Normal;
                break;
            case "--loglinenumbers":
            case "--linenumbers":
            case "--lognumbers":
            case "--debugloglines":
            case "--loglines":
            case "-ln":
                logLineNumbers = true;
                Log("Log line numbers enabled", LogLevel.Debug);
                break;
            default:
                Log($"Unknown argument provided: {arg}", LogLevel.Warning);
                break;
        }
    }

    //private static void HandleHelpArgument()
    //{
    //    Log("Help requested", LogLevel.Info);
    //}

    //private static void HandleVersionArgument()
    //{
    //    Log("Version requested", LogLevel.Debug);
    //    Log($"---------- Version {Version.versionString} (source: arguments) ----------", LogLevel.Info, force: true);
    //}

    //private static void HandleDebugArgument()
    //{
    //    Log("Debug mode requested", LogLevel.Debug);
    //    currentLevel = LogLevel.Debug;
    //    Log("---------- Debug mode enabled (source: arguments) ----------", LogLevel.Debug);
    //}

    //private static void HandleUpdateArgument()
    //{
    //    Log("Update requested");
    //    Version.HandleUpdate(true);
    //}

    //private static void FinishUpdate()
    //{
    //    Log("Finishing update...", LogLevel.Info);
    //    Version.FinishUpdate();
    //    Log("Update finished", LogLevel.Info);
    //}

    //private static void SetProcessPriority()
    //{
    //    Process currentProcess = Process.GetCurrentProcess();
    //    currentProcess.PriorityClass = ProcessPriorityClass.Normal;
    //}

    //private static void HandleUnknownArgument(string arg)
    //{
    //    Log($"Unknown argument provided: {arg}", LogLevel.Warning);
    //}







    // If #debug, prompt to change arguments
    public static string[] PromptArguments(string[] args)
    {
        // Temporarily change log level
        LogLevel tempLevel = currentLevel;
        currentLevel = LogLevel.Debug;

        Log("Current args:", LogLevel.Debug);
        Log(string.Join(' ', args), LogLevel.Debug);
        Log("Would you like to change the arguments? (y/N)", LogLevel.Debug);
        string? input = ReadKey().ToString().ToLower();
        if (input == "y")
        {
            Log("Please provide the new arguments:", LogLevel.Debug);
            string? newArgsInput = ReadLine();
            if (newArgsInput != null)
            {
                string[] newArgs = newArgsInput.Split(' ');
                if (newArgs.Contains("--stopDebug") || newArgs.Contains("--stopdebug") || newArgs.Contains("--nodebug"))
                {
                    Log("Debug mode disabled (source: arguments)", LogLevel.Debug);
                    tempLevel = LogLevel.Info;
                    // Remove --stopDebug from newArgs
                    newArgs = newArgs.Where(x => x != "--stopDebug" && x != "--stopdebug" && x != "--nodebug").ToArray();
                }
                args = newArgs;
            }
            else
            {
                Log("Invalid input. Not changing arguments", LogLevel.Warning);
            }
        }
        else
        {
            Log("Not changing arguments", LogLevel.Debug);
        }

        // Reset log level
        currentLevel = tempLevel;
        return args;
    }
}