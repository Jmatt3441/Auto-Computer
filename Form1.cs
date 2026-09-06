using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace AutoComputer;

// This is the main form for the AutoComputer app.
// The UI setup lives here, while the behavior is split into other files.
public partial class Form1 : Form
{
    // These fields hold the controls we will show on the window.
    private TextBox commandTextBox = null!;
    private Button runButton = null!;
    private Button voiceButton = null!;
    private ListBox logListBox = null!;
    private System.Speech.Recognition.SpeechRecognitionEngine? speechRecognizer;
    private System.Speech.Synthesis.SpeechSynthesizer? speechSynthesizer;
    private readonly Dictionary<string, string> learnedCommands = new(StringComparer.OrdinalIgnoreCase);
    private readonly string memoryFilePath;
    private readonly string assistantMemoryPath;
    private readonly AssistantMemory assistantMemory;
    private bool isListeningEnabled = true;
    private string? lastTarget;
    private string? lastCommand;

    public Form1()
    {
        // This is the standard WinForms setup call.
        InitializeComponent();

        // Store the location where learned commands and conversational memory are saved.
        string localFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoComputer");

        memoryFilePath = Path.Combine(localFolder, "commands.txt");
        assistantMemoryPath = Path.Combine(localFolder, "assistant-memory.json");
        assistantMemory = AssistantMemory.Load(assistantMemoryPath);

        // Build the user interface from the separate layout helper.
        InitializeFormLayout();

        // Prepare the voice output and memory before starting the recognizer.
        InitializeSpeechSynthesizer();
        LoadMemory();
        InitializeVoiceRecognition();

        string greeting = string.IsNullOrWhiteSpace(assistantMemory.UserName)
            ? $"{assistantMemory.AssistantName} is ready."
            : $"Hello {assistantMemory.UserName}. {assistantMemory.AssistantName} is ready.";

        AddLog(greeting, false);
        Speak(greeting);
    }

    // This event runs when the user clicks the button to start listening for voice commands.
    private void VoiceButton_Click(object? sender, EventArgs e)
    {
        if (speechRecognizer == null)
        {
            AddLog("Voice recognition is not available on this machine.", true);
            return;
        }

        try
        {
            speechRecognizer.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Multiple);
            AddLog("Listening for voice commands...", true);
        }
        catch (Exception ex)
        {
            AddLog($"Could not start listening: {ex.Message}", true);
        }
    }

    // This event runs when the user clicks the Run Command button.
    private void RunButton_Click(object? sender, EventArgs e)
    {
        string command = commandTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(command))
        {
            AddLog("Please type a command first.", true);
            return;
        }

        AddLog($"Command received: {command}", true);
        ExecuteCommand(command);
    }
}
