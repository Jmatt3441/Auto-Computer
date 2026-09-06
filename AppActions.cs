using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoComputer;

// This file contains actions that launch, close, switch, or create files.
public partial class Form1
{
    // This opens the default browser to a known website.
    private void OpenBrowser()
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://www.microsoft.com") { UseShellExecute = true });
            AddLog("Opened browser.", true);
            SpeakActionMessage("open", "the browser");
        }
        catch (Exception ex)
        {
            AddLog($"Could not open browser: {ex.Message}", true);
        }
    }

    // This opens a program by name and says a random opening message.
    private void OpenApp(string appName)
    {
        try
        {
            if (IsAppRunning(appName))
            {
                AddLog($"{appName} is already running.", true);
                SpeakActionMessage("open", appName);
                return;
            }

            Process.Start(appName);
            AddLog($"Opened {appName}.", true);
            SpeakActionMessage("open", appName);
        }
        catch (Exception ex)
        {
            AddLog($"Could not open {appName}: {ex.Message}", true);
        }
    }

    // This closes the common browser processes that are currently running.
    private void CloseBrowser()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("chrome"))
            {
                process.Kill();
            }

            foreach (var process in Process.GetProcessesByName("firefox"))
            {
                process.Kill();
            }

            foreach (var process in Process.GetProcessesByName("msedge"))
            {
                process.Kill();
            }

            AddLog("Closed browser.", true);
            SpeakActionMessage("close", "the browser");
        }
        catch (Exception ex)
        {
            AddLog($"Could not close browser: {ex.Message}", true);
        }
    }

    // This closes any app that is currently running by using its process name.
    private void CloseApp(string appName)
    {
        string processName = NormalizeAppName(appName);
        var processes = Process.GetProcessesByName(processName);

        if (processes.Length == 0)
        {
            AddLog($"{appName} is not running.", true);
            return;
        }

        foreach (var process in processes)
        {
            try
            {
                if (!process.CloseMainWindow())
                {
                    process.Kill();
                }

                process.WaitForExit(3000);
            }
            catch (Exception ex)
            {
                AddLog($"Could not close {appName}: {ex.Message}", true);
            }
        }

        AddLog($"Closed {appName}.", true);
        SpeakActionMessage("close", appName);
    }

    // This checks whether a process with the requested name is already running.
    private bool IsAppRunning(string appName)
    {
        string processName = NormalizeAppName(appName);
        return Process.GetProcessesByName(processName).Length > 0;
    }

    // This converts friendly names like "notepad" or "word" into the correct Windows process name.
    private string NormalizeAppName(string appName)
    {
        string normalized = appName.Trim().ToLowerInvariant();

        return normalized switch
        {
            "notepad" or "notepad.exe" or "windows notepad" => "notepad",
            "calculator" or "calc" or "calc.exe" or "windows calculator" => "calc",
            "word" or "winword" or "winword.exe" or "microsoft word" => "winword",
            "chrome" or "google chrome" => "chrome",
            "firefox" or "mozilla firefox" => "firefox",
            "edge" or "msedge" or "microsoft edge" => "msedge",
            _ => Path.GetFileNameWithoutExtension(appName)
        };
    }

private const int SW_RESTORE = 9;

[DllImport("user32.dll", SetLastError = true)]
private static extern bool SetForegroundWindow(IntPtr hWnd);

[DllImport("user32.dll")]
private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

private bool IsCalculatorOpen()
{
    return GetCalculatorWindowHandle() != IntPtr.Zero;
}

private IntPtr GetCalculatorWindowHandle()
{
    var calculatorProcesses = Process.GetProcessesByName("Calculator");
    if (calculatorProcesses.Length == 0)
        calculatorProcesses = Process.GetProcessesByName("calc");

    foreach (var process in calculatorProcesses)
    {
        if(process.MainWindowHandle != IntPtr.Zero)
            return process.MainWindowHandle;
    }    

    return IntPtr.Zero;
}

    // This checks whether a program is already running and prepares to switch to it.
    private void SwitchToApp(string appName)
    {
        string processName = Path.GetFileNameWithoutExtension(appName);
        var processes = Process.GetProcessesByName(processName);

        if (processes.Length > 0)
        {
            AddLog($"Switched to {appName}.", true);
            SpeakActionMessage("switch", appName);
        }
        else
        {
            AddLog($"{appName} is not running.", true);
        }
    }

    // This creates a simple text file on the desktop with the provided content.
    private void WriteNoteFile(string content)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath, "AutoComputerNote.txt");

        File.WriteAllText(filePath, content);
        AddLog($"Created note file: {filePath}", true);
    }

    private void SearchWeb(string query)
    {
        try
        {
            string encodedQuery = Uri.EscapeDataString(query);
            string url = $"https://www.google.com/search?q={encodedQuery}";

            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });

            AddLog($"Searching the web for: {query}", true);
            SpeakActionMessage("open", $"search results for {query}");
        }
        catch (Exception ex)
        {
            AddLog($"Could not search the web: {ex.Message}", true);
        }
    }

    // Open the requested website in the default browser.
    // The raw target can be a simple site name like "facebook",
    // a full address like "https://www.example.com", or a phrase.
    private void OpenWebsite(string rawTarget)
    {
        try
        {
            string target = rawTarget.Trim();

            if (string.IsNullOrEmpty(target))
            {
                AddLog("No website specified.", true);
                return;
            }

            string url = BuildWebsiteUrl(target);

            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });

            AddLog($"Opening website: {target}", true);
            SpeakActionMessage("open", target);
        }
        catch (Exception ex)
        {
            AddLog($"Could not open website {rawTarget}: {ex.Message}", true);
        }
    }

    // Build a safe URL from the target text.
    // - If the user already provided a full URL, use it directly.
    // - If the user gave a plain name like "google", convert it to "https://www.google.com".
    // - If the user gave a multi-word phrase, search it on Google.
    private string BuildWebsiteUrl(string target)
    {
        target = target.Trim();

        if (target.Contains("://", StringComparison.OrdinalIgnoreCase))
            return target;

        if (target.Contains(" "))
            return "https://www.google.com/search?q=" + Uri.EscapeDataString(target);

        if (!target.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            target = "www." + target;

        if (!HasKnownDomainSuffix(target))
            target += ".com";

        return "https://" + target;
    }

    private static bool HasKnownDomainSuffix(string target)
    {
        string[] knownSuffixes = { ".com", ".org", ".net", ".edu", ".gov", ".io", ".co", ".uk", ".us", ".ca", ".dev", ".ai" };

        foreach (string suffix in knownSuffixes)
        {
            if (target.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
