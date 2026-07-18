using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IRacingRadarConfigurator
{
    internal enum RestartSimHubChoice
    {
        RestartNow,
        Later,
        Cancel
    }

    internal sealed class RestartSimHubDialog : Form
    {
        public RestartSimHubChoice Choice { get; private set; }

        public RestartSimHubDialog(bool english, bool dayMode)
        {
            Choice = RestartSimHubChoice.Cancel;
            Text = english ? "Restart SimHub" : "重启 SimHub";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(560, 220);
            Font = new Font("Segoe UI", 9.5f);
            BackColor = dayMode ? Color.FromArgb(244, 247, 251) : Color.FromArgb(24, 29, 38);
            ForeColor = dayMode ? Color.FromArgb(27, 38, 55) : Color.FromArgb(238, 243, 249);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;

            Label title = new Label
            {
                Text = english ? "Settings saved" : "设置已保存",
                Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = ForeColor,
                Location = new Point(28, 24), Size = new Size(500, 34)
            };
            Label message = new Label
            {
                Text = english
                    ? "Restart SimHub now to apply the new radar settings, or restart it manually later."
                    : "需要重启 SimHub 才能应用新的雷达设置。你可以现在重启，也可以稍后手动重启。",
                ForeColor = dayMode ? Color.FromArgb(82, 96, 118) : Color.FromArgb(157, 169, 187),
                Location = new Point(30, 67), Size = new Size(500, 48)
            };

            Button restart = CreateButton(english ? "Restart now" : "现在重启", Color.FromArgb(32, 169, 105), 28, 148);
            Button later = CreateButton(english ? "Restart later" : "稍后手动重启",
                dayMode ? Color.FromArgb(220, 227, 237) : Color.FromArgb(52, 64, 82), 188, 164);
            Button cancel = CreateButton(english ? "Cancel" : "取消",
                dayMode ? Color.FromArgb(220, 227, 237) : Color.FromArgb(52, 64, 82), 364, 148);
            if (dayMode)
            {
                later.ForeColor = Color.FromArgb(35, 48, 67);
                cancel.ForeColor = Color.FromArgb(35, 48, 67);
            }
            restart.Click += delegate { Choice = RestartSimHubChoice.RestartNow; DialogResult = DialogResult.OK; Close(); };
            later.Click += delegate { Choice = RestartSimHubChoice.Later; DialogResult = DialogResult.OK; Close(); };
            cancel.Click += delegate { Choice = RestartSimHubChoice.Cancel; DialogResult = DialogResult.Cancel; Close(); };
            AcceptButton = restart;
            CancelButton = cancel;
            Controls.Add(title); Controls.Add(message); Controls.Add(restart); Controls.Add(later); Controls.Add(cancel);
        }

        private static Button CreateButton(string text, Color background, int left, int width)
        {
            Button button = new Button
            {
                Text = text, BackColor = background, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Location = new Point(left, 151), Size = new Size(width, 42)
            };
            button.FlatAppearance.BorderSize = 0;
            EventHandler round = delegate
            {
                if (button.Width <= 0 || button.Height <= 0) return;
                using (GraphicsPath path = RoundedPath(button.ClientRectangle, 12))
                    button.Region = new Region(path);
            };
            button.SizeChanged += round;
            round(button, EventArgs.Empty);
            return button;
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}