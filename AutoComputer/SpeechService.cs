using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace AutoComputer;

public sealed class SpeechService : IDisposable
{
    private readonly SpeechSynthesizer synthesizer = new();
    private SpeechRecognitionEngine? recognizer;

    public event Action<string>? SpeechRecognized;
    public event Action<string>? StatusChanged;

    public bool IsListening { get; private set; }

    public SpeechService()
    {
        synthesizer.Rate = 0;
        synthesizer.Volume = 100;
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            synthesizer.SpeakAsyncCancelAll();
            synthesizer.SpeakAsync(text);
        }
        catch
        {
        }
    }

    public void StartListening()
    {
        if (IsListening)
            return;

        try
        {
            recognizer ??= CreateRecognizer();
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
            IsListening = true;
            StatusChanged?.Invoke("LISTENING");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke("MIC ERROR");
            throw new InvalidOperationException(
                "Voice recognition could not start. Check your Windows microphone and speech settings.",
                ex);
        }
    }

    public void StopListening()
    {
        if (!IsListening || recognizer is null)
            return;

        try
        {
            recognizer.RecognizeAsyncCancel();
        }
        catch
        {
        }

        IsListening = false;
        StatusChanged?.Invoke("READY");
    }

    private SpeechRecognitionEngine CreateRecognizer()
    {
        var engine = new SpeechRecognitionEngine();
        engine.SetInputToDefaultAudioDevice();
        engine.LoadGrammar(new DictationGrammar());

        engine.SpeechRecognized += (_, e) =>
        {
            if (e.Result.Confidence >= 0.55)
                SpeechRecognized?.Invoke(e.Result.Text);
        };

        engine.SpeechRecognitionRejected += (_, _) =>
            StatusChanged?.Invoke("DIDN'T CATCH THAT");

        return engine;
    }

    public void Dispose()
    {
        StopListening();
        recognizer?.Dispose();
        synthesizer.Dispose();
    }
}
