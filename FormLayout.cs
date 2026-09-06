using System;
using System.Windows.Forms;

namespace AutoComputer;

// This partial class keeps the form layout code separate from the event logic.
public partial class Form1
{
    private void InitializeFormLayout()
    {
        // Set the basic look of the window.
        Text = "AutoComputer";
        Size = new Size(720, 430);
        StartPosition = FormStartPosition.CenterScreen;

        // Create a title label so the window is easy to understand.
        var titleLabel = new Label
        {
            Text = "AutoComputer (Hal) - local PC control",
            AutoSize = true,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Location = new Point(20, 20)
        };

        // Explain what the user can do.
        var instructionsLabel = new Label
        {
            Text = "Say 'Hal' before your command, for example: Hal open browser, Hal open notepad, Hal show time, or Hal remember open notes => open notepad",
            AutoSize = true,
            Location = new Point(20, 60)
        };

        // This text box lets the user type a command.
        commandTextBox = new TextBox
        {
            Width = 470,
            Height = 28,
            Location = new Point(20, 100)
        };

        // This button runs the command.
        runButton = new Button
        {
            Text = "Run Command",
            Width = 120,
            Height = 30,
            Location = new Point(500, 98)
        };
        runButton.Click += RunButton_Click;

        // This button starts listening for spoken commands.
        voiceButton = new Button
        {
            Text = "Start Hal Voice Control",
            Width = 180,
            Height = 30,
            Location = new Point(20, 140)
        };
        voiceButton.Click += VoiceButton_Click;

        // This list box shows the app activity.
        logListBox = new ListBox
        {
            Width = 660,
            Height = 200,
            Location = new Point(20, 180)
        };
        logListBox.Items.Add("Hal is ready.");
        logListBox.Items.Add("Type a command and press Run Command.");

        // Add all controls to the form.
        Controls.Add(titleLabel);
        Controls.Add(instructionsLabel);
        Controls.Add(commandTextBox);
        Controls.Add(runButton);
        Controls.Add(voiceButton);
        Controls.Add(logListBox);
    }
}
