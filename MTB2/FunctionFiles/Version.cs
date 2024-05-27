using Newtonsoft.Json;
using ShellProgressBar;
using System.Diagnostics;
using System.Net;
using static MTB2.Debugger;

namespace MTB2;

internal partial class Version
{
    public static string programDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MTB2", "files");

    private const int timesToRetry = 3;
    public static void HandleUpdate(bool force = false, int retryTime = 0)
    {
        try
        {
            // Remove install folder if it still exists
            string installDir = Path.Combine(programDir, "install");
            if (Directory.Exists(installDir)) Directory.Delete(installDir, true);


            // Check for updates
            string latestVersionString = CheckForUpdates();
            System.Version latestVersion = new(latestVersionString);

            Log($"\nCurrent version: {versionString}", LogLevel.Debug);
            Log($"Latest version: {latestVersionString}", LogLevel.Debug);
            if (latestVersion > version)
            {
                Log("Update available", LogLevel.Info);
            }
            else
            {
                Log("No update available", LogLevel.Info);
                if (!force)
                {
                    return;
                }
                else
                {
                    Log("Installing anyway (forced)", LogLevel.Info);
                }
            }

            // Update STEP 1 ------------------------------------------------------
            Log("\nInitiating update", LogLevel.Info);

            // Determine the files to download
            List<string> files = GetFilesToDownload();

            string downloadDir = Path.Combine(programDir, "install", "download");
            _ = Functions.EnsureDirExists(downloadDir);

            DownloadFiles(files, downloadDir);




            // Update STEP 2 ------------------------------------------------------
            Log("Download complete, updating...", LogLevel.Info);
            Log("Combining files...", LogLevel.Debug);
            string zipfileDirectory = Path.Combine(programDir, "install", "combine");
            _ = Functions.EnsureDirExists(zipfileDirectory);
            CombineFiles(zipfileDirectory);


            // Update STEP 3 ------------------------------------------------------
            Log("Files combined successfully, extracting zip...", LogLevel.Debug);
            // Extract the zip
            string ExtractDir = Path.Combine(programDir, "install", "extract");
            _ = Functions.EnsureDirExists(ExtractDir);
            ExtractZipFiles(zipfileDirectory, ExtractDir);


            // Update STEP 4 ------------------------------------------------------
            Log("Files extracted successfully, copying files...", LogLevel.Debug);
            StartUpdateBatch(ExtractDir);
            Log("Exiting program to finish update...", LogLevel.Info);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Log($"Error during update: {ex.Message}", LogLevel.Error);

            // Retry (but not infinitely)
            if (retryTime < timesToRetry - 1)
            {
                Log("Retrying update...", LogLevel.Info);
                HandleUpdate(retryTime: retryTime + 1);
            }
        }
    }

    private static string CheckForUpdates()
    {
        try
        {
            // Check version-url + version.txt to compare versions
            string latestVersionString = WebRequest.GetPageContent(versionUrl + "version.txt");
            // Check if the string is a valid version
            System.Version _ = new(latestVersionString);
            return latestVersionString;
        }
        catch (Exception ex)
        {
            Log($"Error checking for updates (rethrowing): {ex.Message}", LogLevel.Error);
            throw;
        }
    }

    private static List<string> GetFilesToDownload()
    {
        try
        {
            // Get files to download from version-url + "data/getFiles.php"
            string filesJson = WebRequest.GetPageContent(versionUrl + "data/getFiles.php");
            List<string> files = JsonConvert.DeserializeObject<List<string>>(filesJson) ?? throw new JsonException("Error deserialising response");
            return files;
        }
        catch (Exception ex)
        {
            Log($"Error getting files to download (rethrow): {ex.Message}", LogLevel.Error);
            throw;
        }
    }

    private static void DownloadFiles(List<string> files, string downloadDir)
    {
        int totalTicks = 10000;
        ProgressBarOptions options = new()
        {
            ForegroundColor = ConsoleColor.Yellow,
            ForegroundColorDone = ConsoleColor.Green,
            BackgroundColor = ConsoleColor.DarkGray,
            BackgroundCharacter = '─',
            ProgressCharacter = '\u2588',
            ProgressBarOnBottom = true
        };
        using ProgressBar bar = new(totalTicks, "Downloading files...", options);
        // Download the files
        string ftpUrl = "ftp://ftpupload.net";
        string username = "if0_36162692";
        string password = "sitemm728";

        FluentFTP.FtpClient ftpClient = new(ftpUrl)
        {
            Credentials = new NetworkCredential(username, password)
        };

        try
        {
            ftpClient.Connect();
            int currentFileIndex = 0;
            foreach (string file in files)
            {
                string localFilePath = Path.Combine(downloadDir, file);
                string remoteFilePath = "/htdocs/v2/data/files/" + file;

                // Create a progress reporter that updates the progress bar
                IProgress<double> progress = bar.AsProgress<double>();

                _ = ftpClient.DownloadFile(localFilePath, remoteFilePath, progress: (FluentFTP.FtpProgress ftpProgress) =>
                {
                    double overallProgress = ((double)currentFileIndex / files.Count * 100) + (ftpProgress.Progress / files.Count);
                    progress.Report(overallProgress / 100);
                });

                currentFileIndex++;
            }
        }
        catch (Exception ex)
        {
            Log($"Error during file download (rethrow): {ex.Message}", LogLevel.Error);
            throw;
        }
        finally
        {
            ftpClient.Disconnect();
        }
    }

    private static void CombineFiles(string zipfileDirectory)
    {
        string firstPartFilePath = Path.Combine(programDir, "install", "download", "updateParts.zip.001");


        ProcessStartInfo p = new()
        {
            FileName = "7za.exe",
            Arguments = $"e {firstPartFilePath} -o{zipfileDirectory} -y",
            //WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        Process process = new() { StartInfo = p };
        _ = process.Start();

        //#if DEBUG
        // Some debug things
        //Log("Processname: " + process.ProcessName, LogLevel.Debug);
        try
        {
            Log("Path to 7za: " + process.MainModule?.FileName ?? "No filename found", LogLevel.Debug);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Log($"Unable to access main module: {ex.Message}", LogLevel.Debug);
        }
        //#endif

        process.WaitForExit();
    }


    private static void ExtractZipFiles(string zipfileDirectory, string ExtractDir)
    {
        string[] zipFilePaths = Directory.GetFiles(zipfileDirectory, "*.zip");

        foreach (string zipFilePath in zipFilePaths)
        {
            ProcessStartInfo p2 = new()
            {
                FileName = "7za.exe",
                Arguments = $"x {zipFilePath} -o{ExtractDir} -y",
                //WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            Process process2 = new() { StartInfo = p2 };
            _ = process2.Start();
            process2.WaitForExit();
        }
    }


    private static void StartUpdateBatch(string extractedFilesPath)
    {
        //string batchFilePath = Path.Combine(programDir, "install", "update.bat");
        string programToStartPath = Path.Combine(programDir, "program", "MTB2.exe");

        string arguments = string.Join(" ", Program.publicArgs ?? Array.Empty<string>()) + " --finishUpdate";
        // Remove update argument
        arguments = arguments.Replace("--update ", "").Replace("--install ", "").Replace("--forceupdate ", "").Replace("--forceinstall ", "").Replace("-u ", "");

        using (StreamWriter writer = new(batchFilePath))
        {
            writer.WriteLine("@echo off");
            writer.WriteLine("timeout /t 5 > nul"); // Wait for 5 seconds
            writer.WriteLine("echo Copying files; finishing update...");
            writer.WriteLine($"xcopy /s /y \"{extractedFilesPath}\" \"{Path.Combine(programDir, "program")}\"");
            writer.WriteLine($"start \"\" \"{programToStartPath}\" {arguments}");
            //writer.WriteLine("pause");
        }

        ProcessStartInfo p3 = new()
        {
            FileName = batchFilePath,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = true,
            //RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        Process process3 = new() { StartInfo = p3 };
        _ = process3.Start();
        return;
    }

    private static string batchFilePath = Path.Combine(programDir, "install", "update.bat");

    public static void FinishUpdate()
    {
        // Delete update batch file
        if (File.Exists(batchFilePath)) File.Delete(batchFilePath);
        Log("Deleted update batch file", LogLevel.Debug);

        // Delete update files to avoid errors
        string installDir = Path.Combine(programDir, "install");
        if (Directory.Exists(installDir)) Directory.Delete(installDir, true);
        Log("Deleted install directory", LogLevel.Debug);

        // Add to path
        string? path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
        if (path != null && !path.Contains(programDir))
        {
            Environment.SetEnvironmentVariable("PATH", path + ";" + programDir, EnvironmentVariableTarget.User);
            Log("Added program directory to PATH", LogLevel.Debug);
        }
        else
        {
            Log("Program directory already in PATH", LogLevel.Debug);
        }

        return;
    }






    public static void Uninstall()
    {
        string uninstallPath = Path.Combine(programDir, "uninstall");
        // Delete program directory
        //if (Directory.Exists(programDir)) Directory.Delete(programDir, true);
        //Log("Deleted program directory", LogLevel.Debug);

        // Remove from PATH
        string? path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
        if (path != null && path.Contains(programDir))
        {
            path = path.Replace(programDir, "");
            Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.User);
            Log("Removed program directory from PATH", LogLevel.Debug);
        }
        else
        {
            Log("Program directory not in PATH", LogLevel.Debug);
        }



        // Write a VBScript to show a messagebox
        string vbsFilePath = Path.Combine(uninstallPath, "messagebox.vbs");
        using (StreamWriter vbsWriter = new(vbsFilePath))
        {
            vbsWriter.WriteLine("x=msgbox(\"Uninstallation of MTB2 complete\",0,\"MTB2Uninstaller\")");
        }

        string dirToDelete = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MTB2");

        // Write a batch file to delete the program directory and then itself
        string batchFilePath = Path.Combine(uninstallPath, "uninstall.bat");
        using StreamWriter writer = new(batchFilePath);
        writer.WriteLine("@echo off");
        writer.WriteLine("echo Uninstalling program...");
        writer.WriteLine("timeout /t 5 > nul"); // Wait for 5 seconds
        writer.WriteLine($"Deleting program directory: {programDir}");
        writer.WriteLine($"rmdir /s /q \"{programDir}\"");
        writer.WriteLine($"Deleting files: {dirToDelete}");
        writer.WriteLine($"rmdir /s /q \"{dirToDelete}\"");

        // Run the VBScript
        writer.WriteLine($"cscript //nologo \"{vbsFilePath}\"");

        writer.WriteLine("echo Uninstallation complete");
        writer.WriteLine($"start cmd.exe /C \"timeout /T 3 && del /Q /F \"\"%~f0\"\"\" && del /Q /F \"{vbsFilePath}\"");
        writer.WriteLine("exit");
        return;
    }
}
