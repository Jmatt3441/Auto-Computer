using System.Diagnostics;
using System.Globalization;

namespace AutoComputer;

public sealed class MainForm : Form
{
    private readonly TextBox commandBox = new();
    private readonly RichTextBox outputBox = new();
    private readonly Label statusLabel = new();
    private readonly Label voiceLabel = new();
    private readonly Button listenButton = new();
    private readonly Button speakButton = new();

    private readonly string memoryPath =
        Path.Combine(AppContext.BaseDirectory, "assistant-memory.json");

    private readonly AssistantMemory memory;
    private readonly SpeechService speech = new();
    private readonly Random random = new();

    private bool sleeping;
    private bool voiceReplies = true;
    private string? lastTarget;
    private string? lastCommand;

    public MainForm()
    {
        memory = AssistantMemory.Load(memoryPath);
        MigrateLegacyMemory();

        Text = "AutoComputer";
        Width = 980;
        Height = 690;
        MinimumSize = new Size(760, 540);
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
            Text = "Windows Automation Companion",
            AutoSize = true,
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.Silver,
            Location = new Point(32, 67)
        };

        voiceLabel.Text = "Wake phrase: “Computer”";
        voiceLabel.AutoSize = true;
        voiceLabel.Font = new Font("Segoe UI", 9);
        voiceLabel.ForeColor = Color.DarkGray;
        voiceLabel.Location = new Point(700, 70);
        voiceLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        outputBox.Location = new Point(32, 110);
        outputBox.Size = new Size(900, 400);
        outputBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        outputBox.ReadOnly = true;
        outputBox.BackColor = Color.FromArgb(15, 17, 21);
        outputBox.ForeColor = Color.Gainsboro;
        outputBox.Font = new Font("Consolas", 10);
        outputBox.BorderStyle = BorderStyle.FixedSingle;

        commandBox.Location = new Point(32, 530);
        commandBox.Size = new Size(620, 32);
        commandBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        commandBox.Font = new Font("Segoe UI", 11);
        commandBox.KeyDown += CommandBox_KeyDown;

        var runButton = new Button
        {
            Text = "RUN",
            Location = new Point(666, 528),
            Size = new Size(82, 36),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat
        };
        runButton.Click += (_, _) => RunCurrentCommand();

        listenButton.Text = "LISTEN";
        listenButton.Location = new Point(758, 528);
        listenButton.Size = new Size(82, 36);
        listenButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        listenButton.FlatStyle = FlatStyle.Flat;
        listenButton.Click += (_, _) => ToggleListening();

        speakButton.Text = "VOICE ON";
        speakButton.Location = new Point(850, 528);
        speakButton.Size = new Size(82, 36);
        speakButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        speakButton.FlatStyle = FlatStyle.Flat;
        speakButton.Click += (_, _) =>
        {
            voiceReplies = !voiceReplies;
            speakButton.Text = voiceReplies ? "VOICE ON" : "VOICE OFF";
            Write(voiceReplies ? "Spoken responses enabled." : "Spoken responses muted.");
        };

        statusLabel.Text = "READY";
        statusLabel.AutoSize = true;
        statusLabel.ForeColor = Color.LightGreen;
        statusLabel.Location = new Point(32, 590);
        statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        Controls.AddRange(
        [
            title, subtitle, voiceLabel, outputBox, commandBox,
            runButton, listenButton, speakButton, statusLabel
        ]);

        speech.SpeechRecognized += text =>
        {
            if (!IsHandleCreated) return;
            BeginInvoke(() => HandleVoiceInput(text));
        };

        speech.StatusChanged += status =>
        {
            if (!IsHandleCreated) return;
            BeginInvoke(() =>
            {
                if (!sleeping)
                    SetStatus(status, status == "LISTENING" ? Color.DeepSkyBlue : Color.LightGreen);
            });
        };

        Shown += (_, _) =>
        {
            var greeting = GetGreeting();
            Respond(greeting, speak: false);
            Write("Type 'help' for commands, or press LISTEN and say: “Computer, open calculator.”");
            commandBox.Focus();
        };
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        memory.Save(memoryPath);
        speech.Dispose();
        base.OnFormClosed(e);
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
        ExecuteCommand(command, fromVoice: false);
    }

    private void ToggleListening()
    {
        try
        {
            if (speech.IsListening)
            {
                speech.StopListening();
                listenButton.Text = "LISTEN";
                Respond("Microphone listening stopped.", speak: false);
            }
            else
            {
                speech.StartListening();
                listenButton.Text = "STOP MIC";
                Respond("I'm listening. Say “Computer” before a voice command.", speak: false);
            }
        }
        catch (Exception ex)
        {
            Respond(ex.Message, speak: false);
            listenButton.Text = "LISTEN";
        }
    }

    private void HandleVoiceInput(string recognized)
    {
        Write($"[heard] {recognized}");

        var text = recognized.Trim();
        var lower = text.ToLowerInvariant();

        string? command = null;

        if (lower.StartsWith("computer"))
            command = text["computer".Length..].TrimStart(',', ' ', ':');
        else if (!string.IsNullOrWhiteSpace(memory.AssistantName) &&
                 lower.StartsWith(memory.AssistantName.ToLowerInvariant()))
            command = text[memory.AssistantName.Length..].TrimStart(',', ' ', ':');

        if (string.IsNullOrWhiteSpace(command))
            return;

        Write($"> {command}");
        ExecuteCommand(command, fromVoice: true);
    }

    private void ExecuteCommand(string raw, bool fromVoice)
    {
        var command = raw.Trim();
        if (command.Length == 0) return;

        var lower = command.ToLowerInvariant();
        lastCommand = command;

        if (sleeping && lower is not ("wake" or "wake up" or "are you there"))
        {
            Respond("I'm sleeping. Say “Computer, wake up.”");
            return;
        }

        if (memory.LearnedCommands.TryGetValue(command, out var learned))
        {
            Respond(Pick("Got it.", "On it.", "Using the shortcut you taught me."), speak: fromVoice);
            ExecuteCommand(learned, fromVoice);
            return;
        }

        if (lower is "wake" or "wake up" or "are you there")
        {
            sleeping = false;
            SetStatus(speech.IsListening ? "LISTENING" : "READY",
                speech.IsListening ? Color.DeepSkyBlue : Color.LightGreen);
            Respond(Pick("I'm here.", "I'm awake.", "Right here. What do you need?"));
            return;
        }

        if (lower is "sleep" or "go to sleep" or "stand by")
        {
            sleeping = true;
            SetStatus("SLEEPING", Color.Gold);
            Respond(Pick("Standing by.", "Going quiet. Wake me when you need me.", "All right. I'll be here."));
            return;
        }

        if (lower == "help" || lower is "what can you do" or "what can you do?")
        {
            ShowHelp();
            return;
        }

        if (TryConversation(command, lower))
            return;

        if (lower.StartsWith("remember "))
        {
            Remember(command[9..]);
            return;
        }

        if (lower is "memory" or "show memory" or "what do you remember")
        {
            ShowMemory();
            return;
        }

        if (lower.StartsWith("forget "))
        {
            Forget(command[7..].Trim());
            return;
        }

        if (lower.StartsWith("search "))
        {
            SearchWeb(command[7..].Trim());
            return;
        }

        if (lower.StartsWith("look up "))
        {
            SearchWeb(command[8..].Trim());
            return;
        }

        if (lower.StartsWith("google "))
        {
            SearchWeb(command[7..].Trim());
            return;
        }

        if (lower.StartsWith("calculate "))
        {
            Calculate(command[10..].Trim());
            return;
        }

        if (lower is "time" or "what time is it" or "what's the time")
        {
            Respond($"It is {DateTime.Now:t}.");
            return;
        }

        if (lower is "date" or "what is the date" or "what's the date")
        {
            Respond($"Today is {DateTime.Now:D}.");
            return;
        }

        if (lower is "battery" or "battery level" or "how much battery do i have")
        {
            ReportBattery();
            return;
        }

        if (lower is "close it" or "close that")
        {
            if (string.IsNullOrWhiteSpace(lastTarget))
                Respond("I don't have a recent app or target to close.");
            else
                CloseTarget(lastTarget);
            return;
        }

        if (lower is "open it again" or "open that again")
        {
            if (string.IsNullOrWhiteSpace(lastTarget))
                Respond("I don't have a recent target to reopen.");
            else
                OpenTarget(lastTarget);
            return;
        }

        if (lower.StartsWith("open "))
        {
            OpenTarget(command[5..].Trim());
            return;
        }

        if (lower.StartsWith("launch "))
        {
            OpenTarget(command[7..].Trim());
            return;
        }

        if (lower.StartsWith("close "))
        {
            CloseTarget(command[6..].Trim());
            return;
        }

        Respond(Pick(
            "I don't know that one yet. You can teach me with: remember trigger => action",
            "That's outside my current command set, but you can teach me a shortcut for it.",
            "I haven't learned that yet. Try 'help', or teach me a custom command."));
    }

    private bool TryConversation(string command, string lower)
    {
        if (lower.StartsWith("my name is "))
        {
            var name = command["my name is ".Length..].Trim();
            if (name.Length == 0) return true;

            memory.UserName = name;
            memory.Save(memoryPath);
            Respond($"Got it. I'll remember that your name is {name}.");
            return true;
        }

        if (lower is "what is my name" or "what's my name" or "do you know my name")
        {
            Respond(string.IsNullOrWhiteSpace(memory.UserName)
                ? "You haven't told me your name yet. Say: my name is James."
                : $"Your name is {memory.UserName}.");
            return true;
        }

        if (lower.StartsWith("your name is "))
        {
            var name = command["your name is ".Length..].Trim();
            if (name.Length == 0) return true;

            memory.AssistantName = name;
            memory.Save(memoryPath);
            Respond($"All right. You can call me {name}. For voice commands, say “{name}” first.");
            return true;
        }

        if (lower is "what is your name" or "what's your name" or "who are you")
        {
            Respond($"I'm {memory.AssistantName}, your Windows automation companion.");
            return true;
        }

        if (lower.StartsWith("remember that "))
        {
            var note = command["remember that ".Length..].Trim();
            if (note.Length > 0)
            {
                memory.Notes.Add(note);
                memory.Save(memoryPath);
                Respond("I'll remember that.");
            }
            return true;
        }

        if (lower is "how are you" or "how are you?")
        {
            Respond(Pick(
                "Running smoothly and ready to work.",
                "All systems look good from here.",
                "I'm good. More importantly, I'm ready."));
            return true;
        }

        if (lower is "hello" or "hi" or "hey")
        {
            Respond(string.IsNullOrWhiteSpace(memory.UserName)
                ? Pick("Hey.", "Hello.", "I'm here.")
                : Pick($"Hey, {memory.UserName}.", $"Hello, {memory.UserName}.", $"I'm here, {memory.UserName}."));
            return true;
        }

        if (lower is "thank you" or "thanks" or "thanks computer")
        {
            Respond(Pick("Anytime.", "You got it.", "Of course."));
            return true;
        }

        if (lower is "good morning")
        {
            Respond(string.IsNullOrWhiteSpace(memory.UserName)
                ? "Good morning."
                : $"Good morning, {memory.UserName}.");
            return true;
        }

        if (lower is "good night")
        {
            Respond("Good night. I'll be here when you get back.");
            return true;
        }

        if (lower is "what did i just say" or "what was my last command")
        {
            Respond(lastCommand is null
                ? "You haven't given me a command yet."
                : $"Your last command was: {lastCommand}.");
            return true;
        }

        return false;
    }

    private void ShowHelp()
    {
        Write("""
VOICE
  Press LISTEN, then say: Computer, open calculator
  Computer, search for Detroit weather
  Computer, wake up

CONVERSATION
  hello
  how are you
  my name is <name>
  what's my name
  your name is <assistant name>
  who are you
  remember that <note>
  what do you remember

AUTOMATION
  open / launch calculator | notepad | paint | chrome | edge
  open downloads | documents | desktop | github
  open <website>
  close <app>
  close it
  open it again
  search / look up / google <query>
  calculate <expression>
  battery
  time
  date

LEARNING
  remember <trigger> => <action>
  show memory
  forget <trigger>

STATE
  sleep / stand by
  wake up
""");
        Respond("Those are my current capabilities. And yes, we're still adding more.", speak: false);
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
            ["powershell"] = "powershell.exe",
            ["settings"] = "ms-settings:"
        };

        try
        {
            if (key == "downloads")
                StartShell(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
            else if (key == "documents")
                StartShell(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            else if (key == "desktop")
                StartShell(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            else if (key == "github")
                StartShell("https://github.com");
            else if (known.TryGetValue(key, out var executable))
                Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
            else if (Uri.TryCreate(target, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
                StartShell(target);
            else if (target.Contains('.'))
                StartShell("https://" + target);
            else
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });

            lastTarget = target;
            Respond(Pick($"Opening {target}.", $"On it. Opening {target}.", $"{target} coming up."));
        }
        catch (Exception ex)
        {
            Respond($"I couldn't open {target}. {ex.Message}", speak: false);
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
            if (processes.Length == 0)
            {
                Respond($"I couldn't find a running {target} process.");
                return;
            }

            foreach (var process in processes)
            {
                process.Kill();
                process.Dispose();
            }

            lastTarget = target;
            Respond(Pick($"Closed {target}.", $"{target} is closed.", "Done."));
        }
        catch (Exception ex)
        {
            Respond($"I couldn't close {target}. {ex.Message}", speak: false);
        }
    }

    private void SearchWeb(string query)
    {
        if (query.Length == 0)
        {
            Respond("Tell me what you want me to search for.");
            return;
        }

        StartShell("https://www.google.com/search?q=" + Uri.EscapeDataString(query));
        lastTarget = "browser";
        Respond($"Searching for {query}.");
    }

    private void Calculate(string expression)
    {
        try
        {
            var table = new System.Data.DataTable();
            var result = table.Compute(expression, string.Empty);
            Respond($"{expression} equals {Convert.ToString(result, CultureInfo.InvariantCulture)}.");
        }
        catch
        {
            Respond("I couldn't calculate that expression.");
        }
    }

    private void ReportBattery()
    {
        var power = SystemInformation.PowerStatus;
        if (power.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
        {
            Respond("This computer doesn't report a battery.");
            return;
        }

        var percent = Math.Round(power.BatteryLifePercent * 100);
        var charging = power.PowerLineStatus == PowerLineStatus.Online ? " and it's plugged in" : "";
        Respond($"Battery is at {percent} percent{charging}.");
    }

    private void Remember(string definition)
    {
        if (definition.StartsWith("that ", StringComparison.OrdinalIgnoreCase))
        {
            var note = definition[5..].Trim();
            if (note.Length > 0)
            {
                memory.Notes.Add(note);
                memory.Save(memoryPath);
                Respond("I'll remember that.");
            }
            return;
        }

        var parts = definition.Split("=>", 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
        {
            Respond("Use: remember trigger => action. Or say: remember that followed by a note.");
            return;
        }

        memory.LearnedCommands[parts[0]] = parts[1];
        memory.Save(memoryPath);
        Respond($"Learned. When you say '{parts[0]}', I'll run '{parts[1]}'.");
    }

    private void Forget(string trigger)
    {
        if (memory.LearnedCommands.Remove(trigger))
        {
            memory.Save(memoryPath);
            Respond($"Forgot the command '{trigger}'.");
            return;
        }

        var note = memory.Notes.FirstOrDefault(
            x => x.Contains(trigger, StringComparison.OrdinalIgnoreCase));

        if (note is not null)
        {
            memory.Notes.Remove(note);
            memory.Save(memoryPath);
            Respond("I removed that memory.");
            return;
        }

        Respond($"I couldn't find anything stored under '{trigger}'.");
    }

    private void ShowMemory()
    {
        if (!string.IsNullOrWhiteSpace(memory.UserName))
            Write($"User name: {memory.UserName}");

        Write($"Assistant name: {memory.AssistantName}");

        if (memory.Notes.Count > 0)
        {
            Write("Notes:");
            foreach (var note in memory.Notes)
                Write($"  • {note}");
        }

        if (memory.LearnedCommands.Count > 0)
        {
            Write("Learned commands:");
            foreach (var item in memory.LearnedCommands.OrderBy(x => x.Key))
                Write($"  {item.Key} => {item.Value}");
        }

        if (memory.Notes.Count == 0 &&
            memory.LearnedCommands.Count == 0 &&
            string.IsNullOrWhiteSpace(memory.UserName))
        {
            Write("I don't have much stored yet.");
        }

        Respond("That's what I currently remember.", speak: false);
    }

    private void MigrateLegacyMemory()
    {
        var legacyPath = Path.Combine(AppContext.BaseDirectory, "memory.txt");
        if (!File.Exists(legacyPath) || memory.LearnedCommands.Count > 0)
            return;

        foreach (var line in File.ReadAllLines(legacyPath))
        {
            var parts = line.Split("=>", 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && parts[0].Length > 0)
                memory.LearnedCommands[parts[0]] = parts[1];
        }

        memory.Save(memoryPath);
    }

    private string GetGreeting()
    {
        var hour = DateTime.Now.Hour;
        var timeGreeting = hour < 12 ? "Good morning"
            : hour < 18 ? "Good afternoon"
            : "Good evening";

        if (string.IsNullOrWhiteSpace(memory.UserName))
            return $"{timeGreeting}. AutoComputer is online.";

        return $"{timeGreeting}, {memory.UserName}. AutoComputer is online.";
    }

    private string Pick(params string[] choices) =>
        choices[random.Next(choices.Length)];

    private void Respond(string message, bool speak = true)
    {
        Write(memory.AssistantName + ": " + message);

        if (speak && voiceReplies)
            speech.Speak(message);
    }

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
