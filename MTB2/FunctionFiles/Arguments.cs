using System.Diagnostics;
using static MTB2.Debugger;

namespace MTB2;
internal class Arguments
{
    /// <summary>
    /// Handles the command line arguments.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
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
        string[] otherUiArg = { "--version", "-v", "-h", "--help" };
        foreach (string arg in args)
        {
            if (!Functions.uiArgs.Contains(arg) || otherUiArg.Contains(arg))
            {
                HandleArgument(arg);
            }
            else
            {
                Log("UI argument provided", LogLevel.Debug);
            }
        }

        Log("Finished switching through arguments\n", LogLevel.Debug);
    }

    /// <summary>
    /// Handles the specified argument.
    /// </summary>
    /// <param name="arg">The argument to handle.</param>
    private static void HandleArgument(string arg)
    {
        switch (arg)
        {
            case "-h":
            case "--help":
                Log("Help requested", LogLevel.Info);
                ShowHelp();
                Environment.Exit(0);
                break;
            case "-v":
            case "--version":
                Log("Version requested", LogLevel.Debug);
                Log($"---------- Version {Version.versionString} (source: arguments) ----------", LogLevel.Info, force: true);
                Environment.Exit(0);
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

    /// <summary>
    /// Displays the help information.
    /// </summary>
    private static void ShowHelp()
    {
        Console.WriteLine("\n---------- Help (source: arguments) ----------");
        Console.WriteLine("\nArguments:");
        Console.WriteLine("\n-h, --help: \n\tShow help information");
        Console.WriteLine("\n-v, --version: \n\tShow version information");
        Console.WriteLine("\n-d, --debug: \n\tEnable debug mode");
        Console.WriteLine("\n--forceupdate, --forceinstall, --update, --install, -u: \n\tRequest an update");
        Console.WriteLine("\n--finishupdate, --finishinstall: \n\tFinish an update");
        Console.WriteLine("\n--setpriority, --normalpriority, --priority, --setprocesspriority, --normalprocesspriority, --processpriority: \n\tSet process priority to normal");
        Console.WriteLine("\n--loglinenumbers, --linenumbers, --lognumbers, --debugloglines, --loglines, -ln: \n\tEnable log line numbers");
        Console.WriteLine("\n---------- End of help ----------\n");
    }





    // If #debug, prompt to change arguments
    /// <summary>
    /// Prompts the user to change the arguments if in debug mode.
    /// </summary>
    /// <param name="args">The current arguments.</param>
    /// <returns>The updated arguments.</returns>
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