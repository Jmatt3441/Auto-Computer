using System;
using System.IO;
using System.Linq;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace AutoComputer;

// This file handles command parsing, custom command learning, and memory loading/saving.
public partial class Form1
{
    // This loads previously saved custom commands from the computer so Hal remembers them later.
    private void LoadMemory()
    {
        try
        {
            string folder = Path.GetDirectoryName(memoryFilePath) ?? string.Empty;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            if (!File.Exists(memoryFilePath))
            {
                AddLog("No remembered commands found yet.", false);
                return;
            }

            foreach (var line in File.ReadAllLines(memoryFilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("=>"))
                    continue;

                var parts = line.Split(new[] { "=>" }, 2, StringSplitOptions.None);
                var trigger = parts[0].Trim();
                var action = parts[1].Trim();
                if (!string.IsNullOrEmpty(trigger) && !string.IsNullOrEmpty(action))
                {
                    learnedCommands[trigger] = action;
                }
            }

            if (learnedCommands.Any())
            {
                AddLog($"Loaded {learnedCommands.Count} remembered command(s).", false);
            }
        }

        catch (Exception ex)
        {
            AddLog($"Failed to load memory: {ex.Message}", true);
        }
    }

        private void CloseCalculator() //Closes the Windows Calculator application if it is open, and logs the action.
        {
            try
        {
            IntPtr calculatorWindow = GetCalculatorWindowHandle();
            if (calculatorWindow == IntPtr.Zero)
            {
                AddLog("Calculator is not open.", true);
                return;
            }
            
            PostMessage(calculatorWindow, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

            AddLog("closed Calculator.", true);
            SpeakActionMessage("close", "calculator");
        }
        catch (Exception ex)
        {
            AddLog($"Failed to close Calculator: {ex.Message}", true);
        }
    }

    // This writes any learned commands back to disk so they survive the next time the app starts.
    private void SaveMemory()
    {
        try
        {
            string folder = Path.GetDirectoryName(memoryFilePath) ?? string.Empty;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            File.WriteAllLines(memoryFilePath, learnedCommands.Select(kvp => $"{kvp.Key}=>{kvp.Value}"));
        }
        catch (Exception ex)
        {
            AddLog($"Could not save memory: {ex.Message}", true);
        }
    }

    // This method checks the command text and decides which action should happen next.
    private void ExecuteCommand(string command)
    {
        string lowerCommand = command.ToLowerInvariant();
        lastCommand = command;

        if (TryHandleConversation(command, lowerCommand))
            return;

        if (lowerCommand is "close it" or "close that")
        {
            if (string.IsNullOrWhiteSpace(lastTarget))
            {
                AddLog("I do not have a recent target to close.", true);
            }
            else if (lastTarget.Equals("browser", StringComparison.OrdinalIgnoreCase))
            {
                CloseBrowser();
            }
            else
            {
                CloseApp(lastTarget);
            }

            return;
        }

        if (lowerCommand is "open it again" or "open that again")
        {
            if (string.IsNullOrWhiteSpace(lastTarget))
            {
                AddLog("I do not have a recent target to reopen.", true);
            }
            else if (lastTarget.Equals("browser", StringComparison.OrdinalIgnoreCase))
            {
                OpenBrowser();
            }
            else if (lastTarget.Contains(".") && !lastTarget.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                OpenWebsite(lastTarget);
            }
            else
            {
                OpenApp(lastTarget);
            }

            return;
        }

        if (lowerCommand.StartsWith("remember "))
        {
            LearnCommand(command.Substring("remember ".Length).Trim());
            return;
        }

        if (lowerCommand.Contains("go to sleep") || lowerCommand.Contains("sleep"))
        {
            EnterSleepMode();
            return;
        }

        if (lowerCommand.Contains("wake up") || lowerCommand.Contains("wake"))
        {
            WakeUp();
            return;
        }

        if (learnedCommands.TryGetValue(lowerCommand, out var learnedAction))
        {
            AddLog($"Executing remembered command: {lowerCommand}", true);
            ExecuteCommand(learnedAction);
            return;
        }

        if (lowerCommand.Contains("open browser"))
        {
            OpenBrowser();
            return;
        }
        if (lowerCommand.Contains("close browser") || lowerCommand.Contains("close web browser") || lowerCommand.Contains("close the browser"))
        {
            CloseBrowser();
            return;
        }

        if (lowerCommand.Contains("close calculator") || lowerCommand.Contains("close calc") || lowerCommand.Contains("close the calculator") || lowerCommand.Contains("exit calculator"))
        {
            CloseCalculator();
            return;
        }

        if (lowerCommand.Contains("open word") || lowerCommand.Contains("open microsoft word") || lowerCommand.Contains("open winword"))
        {
            OpenApp("winword.exe");
            return;
        }

        if (lowerCommand.Contains("close chrome") || lowerCommand.Contains("close google chrome"))
        {
            CloseApp("chrome");
            return;
        }

        if (lowerCommand.Contains("close edge") || lowerCommand.Contains("close microsoft edge") || lowerCommand.Contains("close msedge"))
        {
            CloseApp("msedge");
            return;
        }

        if (lowerCommand.Contains("close firefox") || lowerCommand.Contains("close mozilla firefox"))
        {
            CloseApp("firefox");
            return;
        }

        if (lowerCommand.StartsWith("close "))
        {
            string target = command.Substring("close ".Length).Trim();
            if (!string.IsNullOrWhiteSpace(target))
            {
                CloseApp(target);
                return;
            }
        }

        if (lowerCommand.Contains("open notepad"))
        {
            OpenApp("notepad.exe");
            return;
        }

        if (lowerCommand.Contains("switch to notepad"))
        {
            SwitchToApp("notepad.exe");
            return;
        }

        if (lowerCommand.Contains("open calculator") || lowerCommand.Contains("open calc"))
        {
            OpenApp("calc.exe");
            return;
        }

        if (lowerCommand.Contains("write file"))
        {
            string text = command.Replace("write file", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            WriteNoteFile(text);
            return;
        }

        if (lowerCommand.Contains("show time"))
        {
            var timeMessage = DateTime.Now.ToLongTimeString();
            AddLog(timeMessage, true);
            return;
        }

        if (lowerCommand.Contains("search the web for "))
        {
            string query = command.Substring(
                lowerCommand.IndexOf("search the web for ", StringComparison.OrdinalIgnoreCase) + "search the web for ".Length).Trim();

            if (!string.IsNullOrWhiteSpace(query))
            {
                SearchWeb(query);
                return;
            }
        }

        if (lowerCommand.Contains("search web for "))
        {
            string query = command.Substring(
                lowerCommand.IndexOf("search web for ", StringComparison.OrdinalIgnoreCase) + "search web for ".Length).Trim();

            if (!string.IsNullOrWhiteSpace(query))
            {
                SearchWeb(query);
                return;
            }
        }
        // If the user asks Hal to go to a website, extract the website name and send it to the browser-opening helper.
        if (lowerCommand.Contains("goto "))
        {
            int index = lowerCommand.IndexOf("goto ", StringComparison.OrdinalIgnoreCase) + "goto ".Length;
            string site = command.Substring(index).Trim();

            if (!string.IsNullOrWhiteSpace(site))
            {
                OpenWebsite(site);
                return;
            }
        }

        if (lowerCommand.Contains("go to "))
        {
            int index = lowerCommand.IndexOf("go to ", StringComparison.OrdinalIgnoreCase) + "go to ".Length;
            string site = command.Substring(index).Trim();

            if (!string.IsNullOrWhiteSpace(site))
            {
                OpenWebsite(site);
                return;
            }
        }

        if (lowerCommand.StartsWith("search for "))
        {
            string query = command.Substring("search for ".Length).Trim();

            if (!string.IsNullOrWhiteSpace(query))
            {
                SearchWeb(query);
                return;
            }
        }

        if (lowerCommand.StartsWith("search "))
        {
            string query = command.Substring("search ".Length).Trim();

            if (!string.IsNullOrWhiteSpace(query))
            {
                SearchWeb(query);
                return;
            }
        }

        if (TrySendCalculatorInput(lowerCommand, command, out string response))
        {
            AddLog(response, true);
            return;
        }

        AddLog("Command not recognized yet. Use: remember <phrase> => <action>", true);
        }
        


    // Handles conversational commands that are not direct PC actions.
    private bool TryHandleConversation(string command, string lowerCommand)
    {
        if (lowerCommand.StartsWith("my name is "))
        {
            string name = command.Substring("my name is ".Length).Trim();

            if (!string.IsNullOrWhiteSpace(name))
            {
                assistantMemory.UserName = name;
                assistantMemory.Save(assistantMemoryPath);
                AddLog($"I will remember that your name is {name}.", true);
            }

            return true;
        }

        if (lowerCommand is "what is my name" or "what's my name" or "do you know my name")
        {
            if (string.IsNullOrWhiteSpace(assistantMemory.UserName))
                AddLog("You have not told me your name yet.", true);
            else
                AddLog($"Your name is {assistantMemory.UserName}.", true);

            return true;
        }

        if (lowerCommand.StartsWith("your name is "))
        {
            string name = command.Substring("your name is ".Length).Trim();

            if (!string.IsNullOrWhiteSpace(name))
            {
                assistantMemory.AssistantName = name;
                assistantMemory.Save(assistantMemoryPath);
                AddLog($"Understood. You can call me {name}.", true);
            }

            return true;
        }

        if (lowerCommand is "who are you" or "what is your name" or "what's your name")
        {
            AddLog($"I am {assistantMemory.AssistantName}, your AutoComputer assistant.", true);
            return true;
        }

        if (lowerCommand.StartsWith("remember that "))
        {
            string note = command.Substring("remember that ".Length).Trim();

            if (!string.IsNullOrWhiteSpace(note))
            {
                assistantMemory.Notes.Add(note);
                assistantMemory.Save(assistantMemoryPath);
                AddLog("I will remember that.", true);
            }

            return true;
        }

        if (lowerCommand is "what do you remember" or "show memories" or "show my memories")
        {
            if (assistantMemory.Notes.Count == 0)
            {
                AddLog("I do not have any personal notes stored yet.", true);
            }
            else
            {
                AddLog($"I remember {assistantMemory.Notes.Count} personal note(s).", false);

                foreach (string note in assistantMemory.Notes)
                    AddLog($"Memory: {note}", false);

                Speak("I displayed what I remember in the log.");
            }

            return true;
        }

        if (lowerCommand is "hello" or "hi" or "hey")
        {
            string response = string.IsNullOrWhiteSpace(assistantMemory.UserName)
                ? $"Hello. {assistantMemory.AssistantName} is ready."
                : $"Hello {assistantMemory.UserName}. What can I do for you?";

            AddLog(response, true);
            return true;
        }

        if (lowerCommand is "how are you" or "how are you?")
        {
            AddLog("All systems are running normally. I am ready when you are.", true);
            return true;
        }

        if (lowerCommand is "thank you" or "thanks")
        {
            AddLog("Anytime.", true);
            return true;
        }

        if (lowerCommand is "what was my last command" or "what did i just say")
        {
            AddLog(string.IsNullOrWhiteSpace(lastCommand)
                ? "You have not given me a command yet."
                : $"Your last command was: {lastCommand}", true);
            return true;
        }

        return false;
    }

    private static readonly string[] CalculatorInputPrefixes =
    {
        "type",
        "input",
        "enter",
        "press",
        "send"
    };

    private bool TrySendCalculatorInput(string lowerCommand, string originalCommand, out string response)
    {
        response = string.Empty;

        bool usesCalculator = lowerCommand.Contains("calculator") || lowerCommand.Contains("calc");
        bool hasInputVerb = CalculatorInputPrefixes.Any(prefix => lowerCommand.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase));

        if (!usesCalculator && !hasInputVerb)
        return false;

        if (!IsCalculatorOpen())
        {
            response = "Calculator is not open. Please open it first.";
            return true;
        }

        if (!TryExtractCalculatorInput(originalCommand, out var input))
        {
            response = "Could not extract input for calculator.";
            return true;
        }

        var calculatorWindow = GetCalculatorWindowHandle();
        if (calculatorWindow == IntPtr.Zero)
        {
            response = "Could not find the calculator window.";
            return true;
        }

        ShowWindowAsync(calculatorWindow, SW_RESTORE);
        SetForegroundWindow(calculatorWindow);
        System.Threading.Thread.Sleep(300);

        try
        {
            System.Windows.Forms.SendKeys.SendWait(input);
            response = $"Input '{input}' into Calculator.";
        }
        catch (Exception ex)
        {
            response = $"Failed to send input to Calculator: {ex.Message}";
        }

        return true;

    }

    private bool TryExtractCalculatorInput(string command, out string input)
    {
        input = string.Empty;
        if (string.IsNullOrWhiteSpace(command))
        return false;

        string trimmed = command.Trim();
        if (trimmed.StartsWith("Hal ", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed.Substring(4).Trim();

        string lower = trimmed.ToLowerInvariant();

        foreach (var marker in new[] {" in calculator", "into calculator", "in the calculator", "into the calculator", "to calculator", "calculator"})
        {
            int  index = lower.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                trimmed = trimmed.Substring(0, index).Trim();
                lower = trimmed.ToLowerInvariant(); 
                break;
        }
    }

    foreach (var prefix in CalculatorInputPrefixes)
    {
        if (lower.StartsWith(prefix))
        {
            trimmed = trimmed.Substring(prefix.Length).Trim();
            lower = trimmed.ToLowerInvariant();
            break;
        }
    }

    if (string.IsNullOrWhiteSpace(trimmed))
        return false;

    string normalized = lower
        .Replace("multiplied by", "*")
        .Replace("multiply by", "*")
        .Replace("times", "*")
        .Replace("x", "*")
        .Replace("plus", "+")
        .Replace("minus", "-")
        .Replace("divide by", "/")
        .Replace("divided by", "/")
        .Replace("over", "/")
        .Replace("point", ".")
        .Replace("dot", ".")
        .Replace("open parenthesis", "(")
        .Replace("close parenthesis", ")")
        .Replace("equals", "=")
        .Replace("equal", "=");

    normalized = Regex.Replace(
        normalized,
        @"[^0-9\.\+\-\*\/\(\)\=]",
        string.Empty);
        normalized = normalized
        .Replace("+", "{+}")
        .Replace("(", "{(}")
        .Replace(")", "{)}")
        .Replace("=", "{ENTER}");

    if (string.IsNullOrWhiteSpace( normalized))
        return false;

    input = normalized;
    return true;

    }

    private const uint WM_CLOSE = 0x0010;

    [DllImport("user32.dll")]
    private static extern bool PostMessage(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        IntPtr lParam
    );



    // This teaches Hal a new custom command and saves it for later use.
    private void LearnCommand(string instruction)
    {
        if (string.IsNullOrWhiteSpace(instruction) || !instruction.Contains("=>"))
        {
            AddLog("To teach me, use: remember trigger => action", true);
            return;
        }

        var parts = instruction.Split(new[] { "=>" }, 2, StringSplitOptions.None);
        var trigger = parts[0].Trim();
        var action = parts[1].Trim();

        if (string.IsNullOrEmpty(trigger) || string.IsNullOrEmpty(action))
        {
            AddLog("Please provide both a trigger and an action.", true);
            return;
        }

        learnedCommands[trigger] = action;
        SaveMemory();
        UpdateGrammar();
        AddLog($"Learned command '{trigger}'", true);
    }
}
