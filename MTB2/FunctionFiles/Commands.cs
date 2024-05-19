using static MTB2.Debugger;

namespace MTB2;
internal class Commands
{
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
                Log($"No commands found for computer_id {Program.computerId}", LogLevel.Info);
                return new CommandResult { Success = true };
            }

            if (rawCommands.Contains("error"))
            {
                // Deserialize error message
                return HandleWebError(rawCommands);
            }

            Log($"Commands: {rawCommands}", LogLevel.Debug);

            var commands = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CommandClass>>(rawCommands);

            if (commands == null || commands.Count == 0)
            {
                return new CommandResult { Success = false, ErrorMessage = $"Failed to convert commands to CommandClass: {nameof(commands)} is null or empty" };
            }

            return new CommandResult { Success = true, Commands = commands };
        }
        catch (System.Net.WebException ex)
        {
            Log($"Failed to get commands: {ex.Message}", LogLevel.Error);
            throw;
        }
        catch (System.Text.Json.JsonException ex)
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
            string errorMessage = error != null && error.ContainsKey("error") ? error["error"] : rawCommands;
            string responseCode = error != null && error.ContainsKey("code") ? error["code"] : "No response code found";
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

    public class CommandClass
    {
        public string? Command { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public string? Response { get; set; }
        public string? Status { get; set; }
        public int? Id { get; set; }
        public string? ComputerId { get; set; }
        public DateTime? ReceivedAt { get; set; }
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
        SwitchCommand(command.Command, command.Parameters);
    }

    private static void SwitchCommand(string command, Dictionary<string, object>? parameters)
    {
        switch (command)
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
                    string message = parameters.ContainsKey("message") ? parameters["message"]?.ToString() ?? "Something went wrong (nll)" : "Something went wrong.";
                    string caption = parameters.ContainsKey("caption") ? parameters["caption"]?.ToString() ?? "Error_nll" : parameters.ContainsKey("title") ? parameters["title"]?.ToString() ?? "Error_nll" : "Error";
                    uint type = 0;
                    if (parameters.ContainsKey("type") && parameters["type"] is uint?)
                    {
                        type = (uint)parameters["type"];
                    }
                    else if (parameters.ContainsKey("buttons") && parameters["buttons"] is uint?)
                    {
                        type = (uint)parameters["buttons"];
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
                Log($"System info requested (command: {command})", LogLevel.Info);
                break;
            default:
                Log($"Unknown command received: {command}", LogLevel.Warning);
                Msb.Msbox($"Unknown command received: {command}", "MTB2Error", 0x40);
                break;
        }
    }
}




class CommandExecutor
{
    public static string GetSystemInfo()
    {
        // Get current open applications
        var openProcesses = System.Diagnostics.Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)).Select(p => p.ProcessName);

        string info = "System info:<br/><br/>\n\n";
        info += $"Computer ID: {Program.computerId}<br/>\n";
        info += $"MTB2 version: {Version.versionString}<br/>\n";
        info += $"Computer name: {Environment.MachineName}<br/>\n";
        info += $"Username: {Environment.UserName}<br/>\n";
        info += $"OS: {Environment.OSVersion}<br/>\n";
        info += $"Processor count: {Environment.ProcessorCount}<br/>\n";
        //info += $"System directory: {Environment.SystemDirectory}<br/>\n";
        info += $"Current directory: {Environment.CurrentDirectory}<br/>\n";
        //info += $"Is 64-bit OS: {Environment.Is64BitOperatingSystem}<br/>\n";
        //info += $"Is 64-bit process: {Environment.Is64BitProcess}<br/>\n";
        info += $"Current open applications: {string.Join(", ", openProcesses)}<br/>\n";
        info += $"Time: {DateTime.Now}<br/>\n";
        return info;
    }
}