# AutoComputer

AutoComputer is a C#/.NET Windows automation assistant created by James Matthews. It provides a command-driven interface for controlling common Windows tasks from one application.

## Features

- Launch and close Windows applications
- Open websites and perform web searches
- Open folders and common Windows tools
- Perform calculations
- Persistent custom learned commands
- Sleep/wake assistant mode
- Command history and status display
- Extensible command architecture

## Example Commands

```text
open calculator
open notepad
open chrome
open downloads
open github
search C++ SFML tutorials
calculate 125 * 8
what time is it
what is the date
remember school => open https://www.devry.edu
sleep
wake up
```

Learned commands are stored locally in `memory.txt`, allowing AutoComputer to remember custom shortcuts between sessions.

## Technology

- C#
- .NET 8
- Windows Forms
- System.Diagnostics
- Windows shell integration
- File I/O
- Object-oriented programming
- Git / GitHub

## Build

Requirements:

- Windows 10/11
- Visual Studio 2022 or newer
- .NET 8 SDK

Open `AutoComputer.sln` in Visual Studio and run the project, or use:

```powershell
dotnet build AutoComputer.sln
dotnet run --project AutoComputer/AutoComputer.csproj
```

## Portfolio Purpose

AutoComputer demonstrates practical desktop software development, command parsing, Windows automation, persistent application state, process management, and an extensible assistant-style architecture.

## Roadmap

Planned improvements include richer natural-language processing, expanded system controls, voice input/output, configurable commands, application discovery, and a more advanced dashboard.

## Developer

**James Matthews**
