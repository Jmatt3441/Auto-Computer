using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace AutoComputer;

public partial class Form1
{
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    private void OpenBrowser()
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://www.microsoft.com") { UseShellExecute = true });
            lastTarget = "browser";
            AddLog("Opened browser.", true);
            SpeakActionMessage("open", "the browser");
        }
        catch (Exception ex)
        {
            AddLog($"Could not open browser: {ex.Message}", true);
        }
    }

    private void OpenApp(string appName)
    {
        try
        {
            if (IsCalculatorName(appName))
            {
                if (IsCalculatorOpen())
                {
                    AddLog("Calculator is already open.", true);
                    SwitchToCalculator();
                    return;
                }

                Process.Start(new ProcessStartInfo("calc.exe") { UseShellExecute = true });
                lastTarget = "calculator";
                AddLog("Opened calculator.", true);
                SpeakActionMessage("open", "calculator");
                return;
            }

            if (IsAppRunning(appName))
            {
                AddLog($"{appName} is already running.", true);
                SwitchToApp(appName);
                return;
            }

            Process.Start(new ProcessStartInfo(appName) { UseShellExecute = true });
            lastTarget = appName;
            AddLog($"Opened {appName}.", true);
            SpeakActionMessage("open", appName);
        }
        catch (Exception ex)
        {
            AddLog($"Could not open {appName}: {ex.Message}", true);
        }
    }

    private void CloseBrowser()
    {
        try
        {
            bool foundBrowser = false;
            string[] browserProcesses = { "chrome", "firefox", "msedge" };

            foreach (string processName in browserProcesses)
            {
                foreach (Process process in Process.GetProcessesByName(processName))
                {
                    foundBrowser = true;
                    try
                    {
                        if (!process.CloseMainWindow()) process.Kill();
                    }
                    catch { }
                }
            }

            if (!foundBrowser)
            {
                AddLog("No supported browser is currently open.", true);
                return;
            }

            lastTarget = "browser";
            AddLog("Closed browser.", true);
            SpeakActionMessage("close", "the browser");
        }
        catch (Exception ex)
        {
            AddLog($"Could not close browser: {ex.Message}", true);
        }
    }

    private void CloseApp(string appName)
    {
        try
        {
            if (IsCalculatorName(appName))
            {
                CloseCalculator();
                return;
            }

            string processName = NormalizeAppName(appName);
            Process[] processes = Process.GetProcessesByName(processName);

            if (processes.Length == 0)
            {
                AddLog($"{appName} is not running.", true);
                return;
            }

            bool closedAny = false;
            foreach (Process process in processes)
            {
                try
                {
                    if (!process.CloseMainWindow()) process.Kill();
                    closedAny = true;
                }
                catch (Exception ex)
                {
                    AddLog($"Could not close {appName}: {ex.Message}", true);
                }
            }

            if (!closedAny) return;
            lastTarget = appName;
            AddLog($"Closed {appName}.", true);
            SpeakActionMessage("close", appName);
        }
        catch (Exception ex)
        {
            AddLog($"Could not close {appName}: {ex.Message}", true);
        }
    }

    private void CloseCalculator()
    {
        try
        {
            foreach (Process process in Process.GetProcessesByName("ApplicationFrameHost"))
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero &&
                        process.MainWindowTitle.Contains("Calculator", StringComparison.OrdinalIgnoreCase))
                    {
                        process.CloseMainWindow();
                        lastTarget = "calculator";
                        AddLog("Closed calculator.", true);
                        SpeakActionMessage("close", "calculator");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"Could not close calculator: {ex.Message}", true);
                    return;
                }
            }

            foreach (string processName in new[] { "CalculatorApp", "Calculator" })
            {
                foreach (Process process in Process.GetProcessesByName(processName))
                {
                    try
                    {
                        if (process.MainWindowHandle == IntPtr.Zero) continue;
                        if (!process.CloseMainWindow()) process.Kill();
                        lastTarget = "calculator";
                        AddLog("Closed calculator.", true);
                        SpeakActionMessage("close", "calculator");
                        return;
                    }
                    catch { }
                }
            }

            AddLog("Calculator is not open.", true);
        }
        catch (Exception ex)
        {
            AddLog($"Could not close calculator: {ex.Message}", true);
        }
    }

    private bool IsAppRunning(string appName)
    {
        if (IsCalculatorName(appName)) return IsCalculatorOpen();
        return Process.GetProcessesByName(NormalizeAppName(appName)).Length > 0;
    }

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

    private static bool IsCalculatorName(string appName)
    {
        string normalized = appName.Trim().ToLowerInvariant();
        return normalized is "calculator" or "calc" or "calc.exe" or "windows calculator";
    }

    private bool IsCalculatorOpen() => GetCalculatorWindowHandle() != IntPtr.Zero;

    private IntPtr GetCalculatorWindowHandle()
    {
        foreach (Process process in Process.GetProcessesByName("ApplicationFrameHost"))
        {
            try
            {
                if (process.MainWindowHandle != IntPtr.Zero &&
                    process.MainWindowTitle.Contains("Calculator", StringComparison.OrdinalIgnoreCase))
                    return process.MainWindowHandle;
            }
            catch { }
        }

        foreach (string processName in new[] { "CalculatorApp", "Calculator" })
        {
            foreach (Process process in Process.GetProcessesByName(processName))
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero) return process.MainWindowHandle;
                }
                catch { }
            }
        }
        return IntPtr.Zero;
    }

    private void SwitchToCalculator()
    {
        IntPtr calculatorWindow = GetCalculatorWindowHandle();
        if (calculatorWindow == IntPtr.Zero)
        {
            AddLog("Calculator is not open.", true);
            return;
        }
        ShowWindowAsync(calculatorWindow, SW_RESTORE);
        SetForegroundWindow(calculatorWindow);
    }

    private void SwitchToApp(string appName)
    {
        if (IsCalculatorName(appName))
        {
            SwitchToCalculator();
            return;
        }

        Process[] processes = Process.GetProcessesByName(NormalizeAppName(appName));
        if (processes.Length == 0)
        {
            AddLog($"{appName} is not running.", true);
            return;
        }

        foreach (Process process in processes)
        {
            try
            {
                if (process.MainWindowHandle == IntPtr.Zero) continue;
                ShowWindowAsync(process.MainWindowHandle, SW_RESTORE);
                SetForegroundWindow(process.MainWindowHandle);
                lastTarget = appName;
                AddLog($"Switched to {appName}.", true);
                SpeakActionMessage("switch", appName);
                return;
            }
            catch { }
        }
        AddLog($"{appName} is running, but HAL could not find its window.", true);
    }

    private void WriteNoteFile(string content)
    {
        try
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "AutoComputerNote.txt");
            File.WriteAllText(filePath, content);
            AddLog($"Created note file: {filePath}", true);
        }
        catch (Exception ex)
        {
            AddLog($"Could not create note: {ex.Message}", true);
        }
    }

    private void SearchWeb(string query)
    {
        try
        {
            string url = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            lastTarget = "browser";
            AddLog($"Searching the web for: {query}", true);
            SpeakActionMessage("open", $"search results for {query}");
        }
        catch (Exception ex)
        {
            AddLog($"Could not search the web: {ex.Message}", true);
        }
    }

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
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            lastTarget = target;
            AddLog($"Opening website: {target}", true);
            SpeakActionMessage("open", target);
        }
        catch (Exception ex)
        {
            AddLog($"Could not open website {rawTarget}: {ex.Message}", true);
        }
    }

    private string BuildWebsiteUrl(string target)
    {
        target = target.Trim();
        if (target.Contains("://", StringComparison.OrdinalIgnoreCase)) return target;
        if (target.Contains(" ")) return "https://www.google.com/search?q=" + Uri.EscapeDataString(target);
        if (!target.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) target = "www." + target;
        if (!HasKnownDomainSuffix(target)) target += ".com";
        return "https://" + target;
    }

    private static bool HasKnownDomainSuffix(string target)
    {
        string[] knownSuffixes = { ".com", ".org", ".net", ".edu", ".gov", ".io", ".co", ".uk", ".us", ".ca", ".dev", ".ai" };
        foreach (string suffix in knownSuffixes)
            if (target.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
