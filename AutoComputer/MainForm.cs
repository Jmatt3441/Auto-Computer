using System.Diagnostics;
using System.Globalization;

namespace AutoComputer;

public sealed class MainForm : Form
{
    private readonly TextBox commandBox = new();
    private readonly RichTextBox outputBox = new();
    private readonly Label statusLabel = new();
    private readonly Dictionary<string, string> learnedCommands = new(StringComparer.OrdinalIgnoreCase);
    private readonly string memoryFile = Path.Combine(AppContext.BaseDirectory, "memory.txt");
    private bool sleeping;

    public MainForm()
    {
        Text = "AutoComputer";
        Width = 920;
        Height = 620;
        MinimumSize = new Size(720, 480);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(24, 26, 31);

        var title = new Label
        {
            Text = "AUTOCOMPUTER",
            AutoSize = true,
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(28, 22)
        };

        var subtitle = new Label
        {
            Text = "Windows Automation Assistant",
            AutoSize = true,
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.Silver,
            Location = new Point(32, 67)
        };

        outputBox.Location = new Point(32, 110);
        outputBox.Size = new Size(840, 360);
        outputBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        outputBox.ReadOnly = true;
        outputBox.BackColor = Color.FromArgb(15, 17, 21);
        outputBox.ForeColor = Color.Gainsboro;
        outputBox.Font = new Font("Consolas", 10);
        outputBox.BorderStyle = BorderStyle.FixedSingle;

        commandBox.Location = new Point(32, 492);
        commandBox.Size = new Size(690, 32);
        commandBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        commandBox.Font = new Font("Segoe UI", 11);
        commandBox.KeyDown += CommandBox_KeyDown;

        var runButton = new Button
        {
            Text = "RUN",
            Location = new Point(738, 490),
            Size = new Size(134, 36),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat
        };
        runButton.Click += (_, _) => RunCurrentCommand();

        statusLabel.Text = "READY";
        statusLabel.AutoSize = true;
        statusLabel.ForeColor = Color.LightGreen;
        statusLabel.Location = new Point(32, 545);
        statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        Controls.AddRange([title, subtitle, outputBox, commandBox, runButton, statusLabel]);

        LoadMemory();
        Write("AutoComputer online.");
        Write("Type 'help' to see available commands.");
        commandBox.Focus();
    }

    private void CommandBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;
        e.SuppressKeyPress = true;
        RunCurrentCommand();
    }

    private void RunCurrentCommand()
    {
        var command = commandBox.Text.Trim();
        if (command.Length == 0) return;
        commandBox.Clear();
        Write($"> {command}");
        ExecuteCommand(command);
    }

    private void ExecuteCommand(string raw)
    {
        var command = raw.Trim();
        var lower = command.ToLowerInvariant();

        if (sleeping && lower is not ("wake" or "wake up")) {
            Write("AutoComputer is sleeping. Type 'wake up'.");
            return;
        }

        if (learnedCommands.TryGetValue(command, out var learned)) {
            Write($"Learned command: {learned}");
            ExecuteCommand(learned);
            return;
        }

        if (lower is "wake" or "wake up") {
            sleeping = false;
            SetStatus("READY", Color.LightGreen);
            Write("I'm awake.");
        }
        else if (lower is "sleep" or "go to sleep") {
            sleeping = true;
            SetStatus("SLEEPING", Color.Gold);
            Write("Going to sleep. Type 'wake up' when you need me.");
        }
        else if (lower == "help") ShowHelp();
        else if (lower.StartsWith("remember ")) Remember(command[9..]);
        else if (lower is "memory" or "show memory") ShowMemory();
        else if (lower.StartsWith("forget ")) Forget(command[7..].Trim());
        else if (lower.StartsWith("search ")) SearchWeb(command[7..].Trim());
        else if (lower.StartsWith("calculate ")) Calculate(command[10..].Trim());
        else if (lower is "time" or "what time is it") Write($"It is {DateTime.Now:t}.");
        else if (lower is "date" or "what is the date" or "what's the date") Write($"Today is {DateTime.Now:D}.");
        else if (lower.StartsWith("open ")) OpenTarget(command[5..].Trim());
        else if (lower.StartsWith("close ")) CloseTarget(command[6..].Trim());
        else Write("I don't know that command yet. Type 'help', or teach me with: remember trigger => action");
    }

    private void ShowHelp()
    {
        Write("""
Commands:
  open calculator | notepad | paint | chrome | edge | downloads | documents | github
  open <website>
  close calculator | notepad | chrome | edge
  search <web search>
  calculate <expression>
  time
  date
  remember <trigger> => <action>
  show memory
  forget <trigger>
  sleep
  wake up
""");
    }

    private void OpenTarget(string target)
    {
        var key = target.ToLowerInvariant();
        var known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["calculator"] = "calc.exe",
            ["calc"] = "calc.exe",
            ["notepad"] = "notepad.exe",
            ["paint"] = "mspaint.exe",
            ["explorer"] = "explorer.exe",
            ["file explorer"] = "explorer.exe",
            ["chrome"] = "chrome.exe",
            ["edge"] = "msedge.exe",
            ["cmd"] = "cmd.exe",
            ["command prompt"] = "cmd.exe",
            ["powershell"] = "powershell.exe"
        };

        try
        {
            if (key == "downloads") StartShell(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
            else if (key == "documents") StartShell(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            else if (key == "desktop") StartShell(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            else if (key == "github") StartShell("https://github.com");
            else if (known.TryGetValue(key, out var executable)) Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
            else if (Uri.TryCreate(target, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") StartShell(target);
            else if (target.Contains('.')) StartShell("https://" + target);
            else {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            Write($"Opening {target}.");
        }
        catch (Exception ex)
        {
            Write($"Couldn't open {target}: {ex.Message}");
        }
    }

    private void CloseTarget(string target)
    {
        var processName = target.ToLowerInvariant() switch
        {
            "calculator" or "calc" => "CalculatorApp",
            "notepad" => "notepad",
            "chrome" => "chrome",
            "edge" => "msedge",
            "paint" => "mspaint",
            _ => Path.GetFileNameWithoutExtension(target)
        };

        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) {
                Write($"I couldn't find a running {target} process.");
                return;
            }
            foreach (var process in processes) process.Kill();
            Write($"Closed {target}.");
        }
        catch (Exception ex) { Write($"Couldn't close {target}: {ex.Message}"); }
    }

    private void SearchWeb(string query)
    {
        if (query.Length == 0) { Write("Tell me what to search for."); return; }
        StartShell("https://www.google.com/search?q=" + Uri.EscapeDataString(query));
        Write($"Searching for {query}.");
    }

    private void Calculate(string expression)
    {
        try
        {
            var table = new System.Data.DataTable();
            var result = table.Compute(expression, string.Empty);
            Write($"{expression} = {Convert.ToString(result, CultureInfo.InvariantCulture)}");
        }
        catch { Write("I couldn't calculate that expression."); }
    }

    private void Remember(string definition)
    {
        var parts = definition.Split("=>", 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0) {
            Write("Use: remember trigger => action");
            return;
        }
        learnedCommands[parts[0]] = parts[1];
        SaveMemory();
        Write($"Learned '{parts[0]}' => '{parts[1]}'.");
    }

    private void Forget(string trigger)
    {
        if (learnedCommands.Remove(trigger)) {
            SaveMemory();
            Write($"Forgot '{trigger}'.");
        } else Write($"I don't have a learned command named '{trigger}'.");
    }

    private void ShowMemory()
    {
        if (learnedCommands.Count == 0) { Write("No learned commands yet."); return; }
        Write("Learned commands:");
        foreach (var item in learnedCommands.OrderBy(x => x.Key))
            Write($"  {item.Key} => {item.Value}");
    }

    private void LoadMemory()
    {
        if (!File.Exists(memoryFile)) return;
        foreach (var line in File.ReadAllLines(memoryFile)) {
            var parts = line.Split("=>", 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && parts[0].Length > 0) learnedCommands[parts[0]] = parts[1];
        }
    }

    private void SaveMemory() =>
        File.WriteAllLines(memoryFile, learnedCommands.Select(x => $"{x.Key}=>{x.Value}"));

    private static void StartShell(string target) =>
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });

    private void Write(string message)
    {
        outputBox.AppendText(message + Environment.NewLine);
        outputBox.SelectionStart = outputBox.TextLength;
        outputBox.ScrollToCaret();
    }

    private void SetStatus(string text, Color color)
    {
        statusLabel.Text = text;
        statusLabel.ForeColor = color;
    }
}
