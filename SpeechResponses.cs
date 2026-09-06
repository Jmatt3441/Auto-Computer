using System;

namespace AutoComputer;

// This file provides random spoken responses for open, close, and switch actions.
public partial class Form1
{
    // This random generator lets the app sound less repetitive by picking different phrases.
    private readonly Random random = new Random();

    // This method chooses a spoken response based on the action type and then says it aloud.
    private void SpeakActionMessage(string actionType, string target)
    {
        string response = GetRandomActionResponse(actionType, target);
        Speak(response);
    }

    // This method builds a random spoken sentence for opening, closing, or switching programs.
    private string GetRandomActionResponse(string actionType, string target)
    {
        string[] openResponses =
        {
            $"Yes Dave, opening {target} now.",
            $"Affirmative Dave, starting {target} for you.",
            $"Sure Dave, here comes {target} now.",
            $"Absolutely Dave, launching {target} on your behalf, sir."
        };

        string[] closeResponses =
        {
            $"Closing {target} now, Dave.",
            $"Shutting down {target}, Dave.",
            $"I am closing {target} for you, Dave.",
            $"Ending {target} session now, Dave."
        };

        string[] switchResponses =
        {
            $"Switching to {target}, Dave.",
            $"Bringing {target} to the front, Dave.",
            $"Now moving to {target}, Dave.",
            $"Let's go to {target}, Dave."
        };

        return actionType.ToLowerInvariant() switch
        {
            "open" => openResponses[random.Next(openResponses.Length)],
            "close" => closeResponses[random.Next(closeResponses.Length)],
            "switch" => switchResponses[random.Next(switchResponses.Length)],
            _ => $"Performing {target}, Dave."
        };
    }

    // This gives Hal a few different sleep-mode responses.
    private void SpeakSleepResponse()
    {
        string[] sleepResponses =
        {
            "Im not tired Dave!",
            "I aint even tired Dave, but, whatever..",
            "I am switching to sleep mode. I will listen again when you wake me.",
            "Understood Dave . I am going quiet for now."
        };

        Speak(sleepResponses[random.Next(sleepResponses.Length)]);
    }
}
 