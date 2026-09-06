using System;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace AutoComputer;

// This file handles speech recognition, speech output, and log display.
public partial class Form1
{
    // This prepares the speech output system so Hal can talk back to the user.
    private void InitializeSpeechSynthesizer()
    {
        try
        {
            speechSynthesizer = new SpeechSynthesizer();
            speechSynthesizer.SetOutputToDefaultAudioDevice();
        }
        catch
        {
            speechSynthesizer = null;
        }
    }

    // This sets up the microphone-based voice recognizer and connects it to the command handler.
    private void InitializeVoiceRecognition()
    {
        try
        {
            speechRecognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            UpdateGrammar();
            speechRecognizer.SpeechRecognized += SpeechRecognizer_SpeechRecognized;
            speechRecognizer.SetInputToDefaultAudioDevice();
            AddLog("Voice recognition ready.", true);
        }
        catch (Exception ex)
        {
            AddLog($"Voice recognition setup failed: {ex.Message}", true);
        }
    }

    // This stops listening for normal commands and leaves Hal ready to wake on a wake phrase.
    private void EnterSleepMode()
    {
        isListeningEnabled = false;

        try
        {
            UpdateGrammar(wakeOnly: true);
            speechRecognizer?.RecognizeAsync(RecognizeMode.Multiple);
        }
        catch
        {
        }

        AddLog("Hal is going to sleep.", false);
        SpeakSleepResponse();
    }

    // This wakes Hal up and starts listening again.
    private void WakeUp()
    {
        if (speechRecognizer == null)
        {
            AddLog("Voice recognition is not available on this machine.", true);
            return;
        }

        isListeningEnabled = true;

        try
        {
            UpdateGrammar();
            speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
            AddLog("Hal is awake again.", true);
            Speak("I am awake and ready.");
        }
        catch (Exception ex)
        {
            AddLog($"Could not resume listening: {ex.Message}", true);
        }
    }

    // This rebuilds the speech grammar so the recognizer knows about built-in and learned commands.
    private void UpdateGrammar(bool wakeOnly = false)
    {
        if (speechRecognizer == null)
            return;

        speechRecognizer.UnloadAllGrammars();

        var commands = new Choices();

        if (wakeOnly)
        {
            commands.Add(new[]
            {
                "Hal wake up",
                "Hal wake",
                "Computer wake up",
                "Computer wake"
            });
        }
        else
        {
            commands.Add(new[]
            {
                "Hal open browser",
                "Computer open browser",
                "Hal close browser",
                "Hal close web browser",
                "Hal close the browser",
                "close browser",

                "Hal close notepad",

                "Hal close calculator",
                "Hal close the calculator",
                "Hal close calc",
                "Hal exit calculator",

                "Hal close word",
                "Hal search the web for cats",
                "Hal search web for cats",
                "Hal open notepad",
                "Hal open calculator",
                "Hal open word",
                "Hal show time",
                "Hal go to sleep",
                "Hal sleep",
                "Hal wake up",
                "Hal wake",
                "Hal write file hello world",
                "Hal remember open notes => open notepad",
                "Computer open calculator",
                "Computer open notepad",
                "Computer close calculator",
                "Computer close notepad",
                "Computer show time",
                "Computer go to sleep",
                "Computer sleep",
                "Computer wake up",
                "Computer wake",
                "Computer who are you",
                "Computer how are you",
                "Computer what is my name",
                "Computer what do you remember",
                "Computer close it",
                "Computer open it again"
            });

            // These are example spoken phrases that Hal should recognize. The user can also teach Hal new commands, which are added to the grammar dynamically.
            // Add one or more voice examples so the speech recognizer learns
            // the new "goto" website commands.
            commands.Add(new[]
            {
                "Hal go to google",
                "Hal go to facebook",
                "Hal go to youtube",
                "Hal go to twitter",
                "Hal go to github",
                "Hal go to stackoverflow"
            });

            if (learnedCommands.Any())
            {
                foreach (var trigger in learnedCommands.Keys)
                {
                    commands.Add($"Hal {trigger}");
                    commands.Add($"Computer {trigger}");
                }
            }
        }

        var builder = new GrammarBuilder(commands);
        var grammar = new Grammar(builder);
        speechRecognizer.LoadGrammar(grammar);

        // Dictation allows natural phrases after the wake word instead of
        // requiring every possible sentence to be hard-coded.
        if (!wakeOnly)
        {
            try
            {
                speechRecognizer.LoadGrammar(new DictationGrammar());
            }
            catch
            {
            }
        }

        //Allows Hal to recognize variable calculator commands
        var calculatorBuilder = new GrammarBuilder();

        calculatorBuilder.Append("Hal");

        var calculatorActions = new Choices(
            "type",
            "input",
            "enter",
            "press",
            "send"
        );

        calculatorBuilder.Append(calculatorActions);

        // Allows number and math phrases after the command.
        calculatorBuilder.AppendDictation();

        var calculatorGrammar = new Grammar(calculatorActions);
        speechRecognizer.LoadGrammar(calculatorGrammar);
    }

    // This method runs whenever the speech recognizer hears a phrase.
    private void SpeechRecognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result != null)
        {
            string spokenCommand = e.Result.Text;
            AddLog($"Heard: {spokenCommand}", true);

            if (!isListeningEnabled)
            {
                string? commandWithoutWakeWord = null;

                if (spokenCommand.StartsWith("Hal ", StringComparison.OrdinalIgnoreCase))
                    commandWithoutWakeWord = spokenCommand.Substring(4).Trim();
                else if (spokenCommand.StartsWith("Computer ", StringComparison.OrdinalIgnoreCase))
                    commandWithoutWakeWord = spokenCommand.Substring("Computer ".Length).Trim();

                if (commandWithoutWakeWord != null &&
                    (commandWithoutWakeWord.Equals("wake up", StringComparison.OrdinalIgnoreCase) ||
                     commandWithoutWakeWord.Equals("wake", StringComparison.OrdinalIgnoreCase)))
                {
                    WakeUp();
                    return;
                }

                AddLog("Hal is asleep. Say 'Hal wake up' or 'Computer wake up' to resume listening.", true);
                return;
            }

            if (spokenCommand.StartsWith("Hal ", StringComparison.OrdinalIgnoreCase))
            {
                string commandWithoutWakeWord = spokenCommand.Substring(4).Trim();
                ExecuteCommand(commandWithoutWakeWord);
            }
            else if (spokenCommand.StartsWith("Computer ", StringComparison.OrdinalIgnoreCase))
            {
                string commandWithoutWakeWord = spokenCommand.Substring("Computer ".Length).Trim();
                ExecuteCommand(commandWithoutWakeWord);
            }
            else
            {
                AddLog("Say 'Hal' or 'Computer' first to activate voice control.", true);
            }
        }
    }

    // This adds a message to the visible log window and optionally speaks it aloud.
    private void AddLog(string message, bool speak = false)
    {
        logListBox.Items.Add($"{DateTime.Now:HH:mm:ss} - {message}");

        if (logListBox.Items.Count > 20)
        {
            logListBox.Items.RemoveAt(0);
        }

        if (speak)
        {
            Speak(message);
        }
    }

    // This sends a text message to the speech synthesizer so Hal can talk.
    private void Speak(string message)
    {
        if (speechSynthesizer == null)
            return;

        try
        {
            speechSynthesizer.SpeakAsync(message);
        }
        catch
        {
        }
    }
}
