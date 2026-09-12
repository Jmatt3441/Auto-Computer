using System;
using System.Globalization;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace AutoComputer;

public partial class Form1
{
    private const float MinimumVoiceConfidence = 0.55f;

    private void InitializeSpeechSynthesizer()
    {
        try
        {
            speechSynthesizer = new SpeechSynthesizer();
            speechSynthesizer.SetOutputToDefaultAudioDevice();
            speechSynthesizer.Volume = 100;
            speechSynthesizer.Rate = 0;
        }
        catch { speechSynthesizer = null; }
    }

    private void Speak(string message)
    {
        if (speechSynthesizer == null || string.IsNullOrWhiteSpace(message)) return;
        try
        {
            speechSynthesizer.SpeakAsyncCancelAll();
            speechSynthesizer.SpeakAsync(message);
        }
        catch { }
    }

    private void AddLog(string message, bool speak = false)
    {
        try
        {
            if (logListBox.InvokeRequired)
            {
                logListBox.Invoke(new Action(() => AddLog(message, speak)));
                return;
            }
            logListBox.Items.Add($"{DateTime.Now:HH:mm:ss} - {message}");
            if (logListBox.Items.Count > 50) logListBox.Items.RemoveAt(0);
            if (logListBox.Items.Count > 0) logListBox.TopIndex = logListBox.Items.Count - 1;
            if (speak) Speak(message);
        }
        catch { }
    }

    private void InitializeVoiceRecognition()
    {
        if (speechRecognizer != null)
        {
            AddLog("HAL voice control is already running.", false);
            return;
        }

        try
        {
            speechRecognizer = new SpeechRecognitionEngine(new CultureInfo("en-US"));
            speechRecognizer.InitialSilenceTimeout = TimeSpan.FromSeconds(4);
            speechRecognizer.BabbleTimeout = TimeSpan.FromSeconds(2);
            speechRecognizer.EndSilenceTimeout = TimeSpan.FromMilliseconds(600);
            speechRecognizer.EndSilenceTimeoutAmbiguous = TimeSpan.FromMilliseconds(900);
            speechRecognizer.MaxAlternates = 5;
            UpdateGrammar();
            speechRecognizer.SpeechRecognized += SpeechRecognizer_SpeechRecognized;
            speechRecognizer.SpeechRecognitionRejected += SpeechRecognizer_SpeechRecognitionRejected;
            speechRecognizer.SetInputToDefaultAudioDevice();
            speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
            AddLog("HAL voice recognition is online.", false);
        }
        catch (Exception ex)
        {
            AddLog($"Voice recognition failed: {ex.Message}", false);
        }
    }

    private void UpdateGrammar(bool wakeOnly = false)
    {
        if (speechRecognizer == null) return;
        speechRecognizer.UnloadAllGrammars();

        if (wakeOnly)
        {
            Choices wakeCommands = new Choices("Hal wake up", "Hal wake", "Computer wake up", "Computer wake");
            speechRecognizer.LoadGrammar(new Grammar(new GrammarBuilder(wakeCommands)));
            return;
        }

        Choices commands = new Choices();
        commands.Add(new[]
        {
            "Hal open browser", "Computer open browser",
            "Hal close browser", "Computer close browser",
            "Hal close the browser", "Computer close the browser",
            "Hal close web browser", "Computer close web browser",
            "Hal close the web browser", "Computer close the web browser",
            "Hal open calculator", "Computer open calculator",
            "Hal close calculator", "Computer close calculator",
            "Hal open notepad", "Computer open notepad",
            "Hal close notepad", "Computer close notepad",
            "Hal open word", "Computer open word",
            "Hal close word", "Computer close word",
            "Hal show time", "Computer show time",
            "Hal what time is it", "Computer what time is it",
            "Hal go to sleep", "Computer go to sleep",
            "Hal sleep", "Computer sleep",
            "Hal wake up", "Computer wake up",
            "Hal wake", "Computer wake",
            "Hal close it", "Computer close it",
            "Hal open it again", "Computer open it again",
            "Hal who are you", "Computer who are you",
            "Hal how are you", "Computer how are you",
            "Hal what is my name", "Computer what is my name",
            "Hal what do you remember", "Computer what do you remember",
            "Hal thank you", "Computer thank you",
            "Hal thanks", "Computer thanks"
        });

        if (learnedCommands.Any())
        {
            foreach (string trigger in learnedCommands.Keys)
            {
                commands.Add($"Hal {trigger}");
                commands.Add($"Computer {trigger}");
            }
        }

        GrammarBuilder websiteGrammar = new GrammarBuilder();
        websiteGrammar.Append(new Choices("Hal go to", "Computer go to", "Hal goto", "Computer goto"));
        websiteGrammar.AppendDictation();
        speechRecognizer.LoadGrammar(new Grammar(websiteGrammar));

        GrammarBuilder searchGrammar = new GrammarBuilder();
        searchGrammar.Append(new Choices("Hal search for", "Computer search for", "Hal search the web for", "Computer search the web for", "Hal search web for", "Computer search web for"));
        searchGrammar.AppendDictation();
        speechRecognizer.LoadGrammar(new Grammar(searchGrammar));

        GrammarBuilder commandBuilder = new GrammarBuilder(commands);
        commandBuilder.Culture = new CultureInfo("en-US");
        speechRecognizer.LoadGrammar(new Grammar(commandBuilder));
    }

    private void SpeechRecognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result == null) return;
        string spokenText = e.Result.Text.Trim();
        float confidence = e.Result.Confidence;
        if (confidence < MinimumVoiceConfidence) return;

        string? command = RemoveWakeWord(spokenText);
        if (command == null) return;

        if (!isListeningEnabled)
        {
            if (command.Equals("wake up", StringComparison.OrdinalIgnoreCase) || command.Equals("wake", StringComparison.OrdinalIgnoreCase)) WakeUp();
            return;
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            AddLog($"Wake word detected ({confidence:P0})", false);
            Speak("Yes?");
            return;
        }

        AddLog($"Heard: {spokenText} [{confidence:P0}]", false);
        ExecuteCommand(command);
    }

    private void SpeechRecognizer_SpeechRecognitionRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
    {
        // Ignore uncertain/background speech.
    }

    private static string? RemoveWakeWord(string spokenText)
    {
        if (spokenText.Equals("Hal", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        if (spokenText.StartsWith("Hal ", StringComparison.OrdinalIgnoreCase)) return spokenText.Substring(4).Trim();
        if (spokenText.Equals("Computer", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        if (spokenText.StartsWith("Computer ", StringComparison.OrdinalIgnoreCase)) return spokenText.Substring("Computer ".Length).Trim();
        return null;
    }

    private void EnterSleepMode()
    {
        if (speechRecognizer == null) return;
        isListeningEnabled = false;
        AddLog("HAL entering standby mode.", false);
        SpeakSleepResponse();
        try
        {
            speechRecognizer.RecognizeAsyncCancel();
            UpdateGrammar(wakeOnly: true);
            speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
        }
        catch { }
    }

    private void WakeUp()
    {
        if (speechRecognizer == null) return;
        isListeningEnabled = true;
        try
        {
            speechRecognizer.RecognizeAsyncCancel();
            UpdateGrammar();
            speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
            AddLog("HAL is awake.", false);
            Speak(string.IsNullOrWhiteSpace(assistantMemory.UserName) ? "I am awake and ready." : $"I am awake, {assistantMemory.UserName}. What do you need?");
        }
        catch (Exception ex)
        {
            AddLog($"Could not resume voice recognition: {ex.Message}", false);
        }
    }
}
