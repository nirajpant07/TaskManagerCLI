using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskManager.CLI.Utilities
{
    public static class WindowsPopupHelper
    {
        public static bool ShowEndWorkdayDialog()
        {
            var result = MessageBox.Show(
                "Do you want to end the workday?",
                "End Workday",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            return result == DialogResult.Yes;
        }

        public static string? ShowTaskActionDialog(string taskDescription)
        {
            using (var form = new Form())
            {
                form.Text = "Active Task Detected";
                form.Width = 400;
                form.Height = 180;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.TopMost = true;

                var label = new Label()
                {
                    Text = $"Task is still active:\n{taskDescription}\nWhat do you want to do?",
                    AutoSize = false,
                    Width = 360,
                    Height = 50,
                    Top = 10,
                    Left = 10
                };
                var btnComplete = new Button() { Text = "Complete", Left = 40, Width = 90, Top = 70, DialogResult = DialogResult.Yes };
                var btnPause = new Button() { Text = "Pause for Next Day", Left = 150, Width = 120, Top = 70, DialogResult = DialogResult.No };
                var btnSkip = new Button() { Text = "Skip", Left = 290, Width = 60, Top = 70, DialogResult = DialogResult.Cancel };

                btnComplete.Click += (s, e) => { form.DialogResult = DialogResult.Yes; form.Close(); };
                btnPause.Click += (s, e) => { form.DialogResult = DialogResult.No; form.Close(); };
                btnSkip.Click += (s, e) => { form.DialogResult = DialogResult.Cancel; form.Close(); };

                form.Controls.Add(label);
                form.Controls.Add(btnComplete);
                form.Controls.Add(btnPause);
                form.Controls.Add(btnSkip);

                var result = form.ShowDialog();
                if (result == DialogResult.Yes) return "complete";
                if (result == DialogResult.No) return "pause";
                return "skip";
            }
        }

        public static void ShowGoodbyeMessageWithTimer(int seconds = 5)
        {
            using (var form = new Form())
            {
                form.Text = "Goodbye!";
                form.Width = 300;
                form.Height = 120;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.TopMost = true;

                var label = new Label()
                {
                    Text = $"Goodbye! The application will close in {seconds} seconds...",
                    AutoSize = false,
                    Width = 260,
                    Height = 40,
                    Top = 20,
                    Left = 20
                };
                form.Controls.Add(label);

                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 1000;
                int remaining = seconds;
                timer.Tick += (s, e) =>
                {
                    remaining--;
                    label.Text = $"Goodbye! The application will close in {remaining} seconds...";
                    if (remaining <= 0)
                    {
                        timer.Stop();
                        form.Close();
                    }
                };
                timer.Start();
                form.ShowDialog();
            }
        }
    }
} 