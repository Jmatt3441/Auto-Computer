# AutoComputer

AutoComputer is a C#/.NET Windows automation companion built by James Matthews. It combines desktop automation, conversational command handling, persistent memory, and Windows speech services into an assistant that can listen, respond, remember, and act on the computer.

## Phase 2: Companion Mode

AutoComputer now behaves less like a command launcher and more like a persistent desktop assistant.

### Voice interaction

Press **LISTEN** and use the wake phrase:

```text
Computer, open calculator
Computer, search C++ SFML tutorials
Computer, what time is it
Computer, wake up
```

Voice commands require the wake phrase so AutoComputer can listen continuously without treating normal room conversation as commands.

### Spoken responses

AutoComputer can speak its responses using Windows speech synthesis. Spoken replies can be turned on or off from the interface.

### Persistent memory

AutoComputer stores personal assistant data locally in:

```text
assistant-memory.json
```

That file is ignored by Git and is not uploaded to GitHub.

Examples:

```text
my name is James
what's my name
your name is Kit
remember that my class starts at 6 PM
what do you remember
```

### Learned commands

Create your own command shortcuts:

```text
remember school => open https://www.devry.edu
remember coding => open github
```

AutoComputer remembers them between sessions.

### Conversational context

AutoComputer keeps limited short-term context during the current session.

Example:

```text
open calculator
close it
open it again
```

The words **it** and **that** can refer to the most recently opened or controlled target.

## Current Capabilities

- Voice recognition through the default Windows microphone
- Wake-phrase command activation
- Speech synthesis
- Personalized greetings
- Persistent user and assistant names
- Persistent notes and custom commands
- Context-aware follow-up commands
- Launch and close Windows applications
- Open websites and common folders
- Google/web searches
- Calculations
- Battery status
- Date and time
- Sleep / standby and wake states
- Varied conversational responses
- Command history display

## Example Commands

```text
hello
how are you
my name is James
what's my name
who are you

open calculator
open notepad
launch chrome
open downloads
open github
close it
open it again

search C++ tutorials
look up Detroit history
google SFML 3 documentation
calculate 125 * 8

battery
what time is it
what's the date

remember that I prefer Chrome
remember school => open https://www.devry.edu
show memory
forget school

stand by
wake up
```

## Technology

- C#
- .NET 8
- Windows Forms
- System.Speech
- Windows speech recognition
- Windows speech synthesis
- System.Diagnostics
- Windows shell integration
- JSON persistence
- File I/O
- Object-oriented programming
- GitHub Actions

## Build

Requirements:

- Windows 10/11
- Visual Studio 2022 or newer
- .NET 8 SDK
- A Windows speech recognition language installed for voice input

Open `AutoComputer.sln` in Visual Studio, or run:

```powershell
dotnet restore AutoComputer.sln
dotnet build AutoComputer.sln
dotnet run --project AutoComputer/AutoComputer.csproj
```

## Privacy

AutoComputer's personal memory is stored locally on the user's machine. The generated `assistant-memory.json` file is excluded from Git by default.

The current version does not send that stored assistant memory to a remote service.

## Architecture

```text
User
  │
  ├── Keyboard command
  │
  └── Microphone
        │
        ▼
   Wake phrase
        │
        ▼
 Command / conversation parser
        │
        ├── Assistant memory
        ├── Session context
        ├── Windows process control
        ├── Web / shell actions
        └── System information
        │
        ▼
 Text response + spoken response
```

## Where This Is Going

The long-term goal is to make AutoComputer feel like a genuine local computer companion while keeping the distinction clear: it is software with memory, context, automation, and conversational behavior—not a conscious system.

Future phases can add:

- richer natural-language intent detection
- reminders and schedules
- local application discovery
- media controls
- volume and display control
- weather and calendar integration
- optional AI/LLM conversation layer
- plugin-style skills
- user-defined routines such as “start my school setup”
- system monitoring and proactive notifications
- animated assistant interface

## Developer

**James Matthews**
