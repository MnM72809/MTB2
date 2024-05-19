using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Chrome;
using static MTB2.Debugger;

namespace MTB2;

internal class Functions
{
    public static string[] uiArgs = new string[] { "--enableui", "--enableconsoleui", "--enableUI", "--ui", "--UI" };
    public static void HandleUI(string[] args)
    {

        if (!args.Any(arg => uiArgs.Contains(arg, StringComparer.OrdinalIgnoreCase)))
        {
            try
            {
                StartNewProcessWithUI(args);
            }
            catch (Exception ex)
            {
                HandleProcessStartError(ex);
            }
        }
    }

    private static void StartNewProcessWithUI(string[] args)
    {
        Process currentProcess = Process.GetCurrentProcess();
        string? fileName = currentProcess.MainModule?.FileName;
        if (string.IsNullOrEmpty(fileName))
        {
            throw new InvalidOperationException("Main module or file name is null.");
        }

        args = AddEnableUIArgument(args);
        string argsString = string.Join(" ", args);

        ProcessStartInfo startInfo = CreateProcessStartInfo(fileName, argsString);
        Process newProcess = StartNewProcess(startInfo);

        if (newProcess == null || newProcess.HasExited)
        {
            throw new InvalidOperationException("New process did not start successfully.");
        }

        Environment.Exit(0);
    }

    private static string[] AddEnableUIArgument(string[] args)
    {
        return args.Concat(args.Contains("--enableui", StringComparer.OrdinalIgnoreCase) ? Array.Empty<string>() : new[] { "--enableui" }).ToArray();
    }

    private static ProcessStartInfo CreateProcessStartInfo(string fileName, string argsString)
    {
        return new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = argsString,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }

    private static Process StartNewProcess(ProcessStartInfo startInfo)
    {
        return Process.Start(startInfo) ?? throw new InvalidOperationException("New process did not start successfully (is null).");
    }

    private static void HandleProcessStartError(Exception ex)
    {
        Log($"An error occurred: {ex.Message}", LogLevel.Critical);
        Log($"Stack trace: {ex.StackTrace}", LogLevel.Critical);
        Log("Failed to start new process. Press any key to exit, or press \"c\" to continue with console enabled.", LogLevel.Critical);
        if (Console.ReadKey().Key != ConsoleKey.C)
        {
            Environment.Exit(1);
        }
    }





    /// <summary>
    /// Ensures that the specified directory exists. If the directory does not exist, it will be created. Will log any errors that occur, and rethrow the exception.
    /// </summary>
    /// <param name="dirPath">The path of the directory to ensure.</param>
    /// <returns><c>true</c> if the directory exists or was successfully created, otherwise <c>false</c>.</returns>
    public static bool EnsureDirExists(string dirPath)
    {
        if (!Directory.Exists(dirPath))
        {
            try
            {
                Directory.CreateDirectory(dirPath);
                return true;
            }
            catch (Exception ex)
            {
                Log($"An error occurred: {ex.Message}", LogLevel.Critical);
                Log($"Stack trace: {ex.StackTrace}", LogLevel.Critical);
                throw;
                //return false;
            }
        }
        return false;
    }
}


/*class ProgressBar
{

    /// <summary>
    /// Draws a progress bar on the console.
    /// </summary>
    /// <param name="complete">The current progress value.</param>
    /// <param name="maxVal">The maximum value of the progress.</param>
    /// <param name="barSize">The size of the progress bar.</param>
    /// <param name="progressCharacter">The character used to represent the progress.</param>
    public static void DrawProgressBar(int complete, int maxVal, int barSize, char progressCharacter)
    {
        Console.CursorVisible = false;
        int left = Console.CursorLeft;
        decimal perc = complete / (decimal)maxVal;
        int chars = (int)Math.Floor(perc / (1 / (decimal)barSize));
        string p1 = string.Empty, p2 = string.Empty;

        for (int i = 0; i < chars; i++)
        {
            p1 += progressCharacter;
        }

        for (int i = 0; i < barSize - chars; i++)
        {
            p2 += progressCharacter;
        }

        Console.Write($"\r[{p1}{p2}] {perc:P1}");
        Console.CursorLeft = left;
    }
}*/






public class Msb
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(int hWnd, string text, string caption, uint type);




    /// <summary>
    /// Displays a message box with the specified message, caption, and type.
    /// </summary>
    /// <param name="message">The message to display in the message box. Default value is "Something went wrong."</param>
    /// <param name="caption">The caption to display in the message box. Default value is "Error."</param>
    /// <param name="type">The type of message box to display. Default value is 0 (0x0).<br/><br/>Possible values are: <br/>
    /// 0 (0x0, MB_OK): The message box contains one push button: OK. This is the default.<br/>
    /// 1 (0x1, MB_OKCANCEL): The message box contains two push buttons: OK and Cancel.<br/>
    /// 2 (0x2, MB_ABORTRETRYIGNORE): The message box contains three push buttons: Abort, Retry, and Ignore.<br/>
    /// 3 (0x3, MB_YESNOCANCEL): The message box contains three push buttons: Yes, No, and Cancel.<br/>
    /// 4 (0x4, MB_YESNO): The message box contains two push buttons: Yes and No.<br/>
    /// 5 (0x5, MB_RETRYCANCEL): The message box contains two push buttons: Retry and Cancel.<br/><br/>
    /// In addition to these basic options, you can specify icons to appear in the message box by adding one of the following values:<br/>
    /// 16 (0x10, MB_ICONHAND): An error-message icon appears in the message box.<br/>
    /// 32 (0x20, MB_ICONQUESTION): A question-mark icon appears in the message box.<br/>
    /// 48 (0x30, MB_ICONEXCLAMATION): An exclamation-point icon appears in the message box.<br/>
    /// 64 (0x40, MB_ICONASTERISK): An information icon appears in the message box.
    /// </param>
    /// <returns>The result of the message box.</returns>
    public static int Msbox(string message = "Something went wrong.", string caption = "Error", uint type = 0)
    {
        // Always return the message box with the type and the MB_SYSTEMMODAL flag
        return MessageBox(0, message, caption, type | 0x1000);
    }
}












class WebRequest
{
    public static string GetPageContent(string url)
    {
        OpenQA.Selenium.IWebDriver? driver = null;

        try
        {
            try
            {
                driver = CreateChromeDriver();
            }
            catch (OpenQA.Selenium.WebDriverException)
            {
                driver = CreateEdgeDriver();
            }

            driver.Navigate().GoToUrl(url);

            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(drv => drv.FindElement(OpenQA.Selenium.By.TagName("body")));

            string pageContent = driver.FindElement(OpenQA.Selenium.By.TagName("body")).Text;

            return pageContent;
        }
        catch (Exception ex)
        {
            Log($"An error occurred: {ex.Message}", LogLevel.Critical);
            Log($"Stack trace: {ex.StackTrace}", LogLevel.Critical);
            throw;
            //return string.Empty;
        }
        finally
        {
            driver?.Quit();
        }
    }

    private static OpenQA.Selenium.IWebDriver CreateChromeDriver()
    {
        var chromeOptions = new ChromeOptions();
        chromeOptions.AddArgument("headless");

        var chromeService = ChromeDriverService.CreateDefaultService();
        chromeService.HideCommandPromptWindow = true;
        chromeService.SuppressInitialDiagnosticInformation = true;

        return new ChromeDriver(chromeService, chromeOptions);
    }

    private static OpenQA.Selenium.IWebDriver CreateEdgeDriver()
    {
        var edgeOptions = new EdgeOptions();
        edgeOptions.AddArgument("headless");

        var edgeService = EdgeDriverService.CreateDefaultService();
        edgeService.HideCommandPromptWindow = true;
        edgeService.SuppressInitialDiagnosticInformation = true;

        return new EdgeDriver(edgeService, edgeOptions);
    }
}


















class Debugger
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    public static bool logLineNumbers = false;

    public static LogLevel currentLevel = LogLevel.Info;

    /// <summary>
    /// Logs a message with the specified log level.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    /// <param name="level">The log level of the message (default is 1).</param>
    /// <param name="force">A flag indicating whether to force the message to be logged, regardless of the current log level (default is false).</param>
    public static void Log(string message, int level, bool force = false)
    {
        // Convert the int level to a LogLevel enum
        LogLevel logLevel = (LogLevel)level;
        Log(message, logLevel, force);
    }

    /// <summary>
    /// Logs a message with the specified log level.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    /// <param name="level">The log level of the message (default is LogLevel.Info).</param>
    /// <param name="force">A flag indicating whether to force the message to be logged, regardless of the current log level (default is false).</param>
    public static void Log(string message, LogLevel level = LogLevel.Info, bool force = false)
    {
        // Check for "\n" in the beginning of the message
        if (message.StartsWith("\n"))
        {
            while (message.StartsWith("\n"))
            {
                // Log a new line
                Console.WriteLine();
                // Remove the "\n" from the message
                message = message[1..];
            }
        }


        // If the level of this message is lower than the current level and force is not true, don't log the message
        if (!force && level < currentLevel)
        {
            return;
        }

        string shortLevel = "!E!";

        switch (level)
        {
            case LogLevel.Debug:
                Console.ForegroundColor = ConsoleColor.Blue;
                shortLevel = "Debug";
                break;
            case LogLevel.Info:
                Console.ForegroundColor = ConsoleColor.Green;
                shortLevel = "Info";
                break;
            case LogLevel.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                shortLevel = "Warn";
                break;
            case LogLevel.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                shortLevel = "Error";
                break;
            case LogLevel.Critical:
                Console.ForegroundColor = ConsoleColor.DarkRed;
                shortLevel = "Crit";
                break;
        }

        // Log the message with the LogLevel
        Console.Write($"{shortLevel}");

        // Log the line number and file name if the flag is set
        if (logLineNumbers)
        {
            setCursor(Settings.initCursorPos);
            Settings.cursorPos = Settings.initCursorPos + 12;
            string? fileName = Path.GetFileName(new StackFrame(1, true).GetFileName());
            if (fileName != null)
            {
                string? shortFileName = Path.GetFileNameWithoutExtension(fileName);
                if (shortFileName != null)
                {
                    shortFileName = shortFileName[..Math.Min(7, shortFileName.Length)]; // Neemt de eerste 8 tekens van de bestandsnaam zonder extensie
                }
                Console.Write($"L{new StackFrame(1, true).GetFileLineNumber()}F{shortFileName}");
            }
            else
            {
                Console.Write($"L{new StackFrame(1, true).GetFileLineNumber()}");
            }
        }

        // Set the cursor position to a fixed column
        setCursor(Settings.cursorPos);

        Console.Write("--> ");

        // Reset the console color
        Console.ResetColor();

        Console.Write($"{message}");
        Console.WriteLine();



        static void setCursor(int cursorPos)
        {
            try
            {
                Console.SetCursorPosition(cursorPos, Console.CursorTop);
            }
            catch (Exception)
            {
                Console.WriteLine(" ");
            }
        }
    }








    public static char ReadKey()
    {
        Console.Write("Input");
        // Set the cursor position to a fixed column
        try
        {
            Console.SetCursorPosition(Settings.cursorPos, Console.CursorTop);
        }
        catch (Exception)
        {
            Console.WriteLine(" ");
        }
        Console.Write("--> ");
        char key = Console.ReadKey().KeyChar;
        Console.WriteLine();
        return key;
    }

    public static string? ReadLine()
    {
        Console.Write("Input");
        // Set the cursor position to a fixed column
        try
        {
            Console.SetCursorPosition(Settings.cursorPos, Console.CursorTop);
        }
        catch (Exception)
        {
            Console.WriteLine(" ");
        }
        Console.Write("--> ");
        string? input = Console.ReadLine();
        Console.WriteLine();
        return input;
    }
}