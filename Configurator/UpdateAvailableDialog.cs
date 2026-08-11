using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace IRacingRadarConfigurator
{
    internal sealed class UpdateAvailableDialog : Form
    {
        internal UpdateAvailableDialog(AvailableRelease release, bool english, bool dayMode)
        {
            string tag = release == null ? string.Empty : release.Tag;
            string notes = SelectLanguage(release == null ? string.Empty : release.ReleaseNotes, english);
            if (string.IsNullOrWhiteSpace(notes))
                notes = english ? "No release notes were provided." : "此版本没有提供更新日志。";

            Color background = dayMode ? Color.FromArgb(244, 247, 251) : Color.FromArgb(24, 29, 38);
            Color panel = dayMode ? Color.White : Color.FromArgb(32, 39, 50);
            Color foreground = dayMode ? Color.FromArgb(28, 37, 51) : Color.FromArgb(238, 243, 249);
            Color secondary = dayMode ? Color.FromArgb(91, 105, 125) : Color.FromArgb(150, 163, 181);

            Text = english ? "iRacing Radar update" : "iRacing Radar 发现新版本";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(650, 500);
            BackColor = background;
            ForeColor = foreground;
            Font = new Font("Segoe UI", 9.5f);

            Label heading = new Label
            {
                Text = english ? "New version available: " + tag : "发现新版本：" + tag,
                Location = new Point(24, 20),
                Size = new Size(602, 32),
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = foreground
            };
            Label notesLabel = new Label
            {
                Text = english ? "Release notes" : "更新日志",
                Location = new Point(26, 67),
                Size = new Size(150, 23),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = foreground
            };
            Panel notesCard = new Panel
            {
                Location = new Point(24, 93),
                Size = new Size(602, 325),
                BackColor = panel,
                Padding = new Padding(18, 14, 12, 14),
                AutoScroll = true
            };
            FlowLayoutPanel notesContent = new FlowLayoutPanel
            {
                Location = new Point(18, 14),
                Width = 548,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = panel,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            PopulateReleaseNotes(notesContent, notes, foreground, secondary, panel);
            notesCard.Controls.Add(notesContent);
            Button install = new Button
            {
                Text = english ? "Download and install" : "下载并安装",
                DialogResult = DialogResult.OK,
                Location = new Point(380, 442),
                Size = new Size(150, 36),
                BackColor = Color.FromArgb(36, 169, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            install.FlatAppearance.BorderSize = 0;
            Button later = new Button
            {
                Text = english ? "Later" : "稍后",
                DialogResult = DialogResult.Cancel,
                Location = new Point(538, 442),
                Size = new Size(88, 36),
                BackColor = dayMode ? Color.FromArgb(221, 228, 237) : Color.FromArgb(52, 64, 82),
                ForeColor = foreground,
                FlatStyle = FlatStyle.Flat
            };
            later.FlatAppearance.BorderSize = 0;

            Controls.Add(heading);
            Controls.Add(notesLabel);
            Controls.Add(notesCard);
            Controls.Add(install);
            Controls.Add(later);
            AcceptButton = install;
            CancelButton = later;
        }

        private static void PopulateReleaseNotes(FlowLayoutPanel content, string notes,
            Color foreground, Color secondary, Color background)
        {
            string normalized = (notes ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            foreach (string sourceLine in normalized.Split('\n'))
            {
                string line = sourceLine.Trim();
                if (line.Length == 0)
                {
                    content.Controls.Add(new Panel { Size = new Size(540, 7), BackColor = background, Margin = Padding.Empty });
                    continue;
                }
                if (line == "---" || line == "***" || line == "___") continue;

                int headingLevel = 0;
                while (headingLevel < line.Length && line[headingLevel] == '#') headingLevel++;
                bool heading = headingLevel > 0 && headingLevel < line.Length && char.IsWhiteSpace(line[headingLevel]);
                if (heading) line = line.Substring(headingLevel).Trim();

                bool bullet = !heading && (line.StartsWith("- ", StringComparison.Ordinal) ||
                    line.StartsWith("* ", StringComparison.Ordinal));
                if (bullet) line = "•  " + line.Substring(2).Trim();
                line = CleanMarkdown(line);

                Label label = new Label
                {
                    AutoSize = true,
                    MaximumSize = new Size(540, 0),
                    MinimumSize = new Size(540, 0),
                    Text = line,
                    ForeColor = heading ? foreground : (bullet ? foreground : secondary),
                    BackColor = background,
                    Font = heading
                        ? new Font("Segoe UI", headingLevel <= 2 ? 11.5f : 10.5f, FontStyle.Bold)
                        : new Font("Segoe UI", 9.5f, FontStyle.Regular),
                    Margin = heading ? new Padding(0, 4, 0, 5) : new Padding(0, 1, 0, 4),
                    UseCompatibleTextRendering = true
                };
                content.Controls.Add(label);
            }
        }

        private static string SelectLanguage(string notes, bool english)
        {
            if (string.IsNullOrWhiteSpace(notes)) return string.Empty;
            string[] lines = notes.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int chinese = FindLanguageHeading(lines, false);
            int englishHeading = FindLanguageHeading(lines, true);
            if (chinese < 0 && englishHeading < 0) return KeepChangeNotes(notes);

            int start = english ? englishHeading : chinese;
            if (start < 0) return notes;
            int end = lines.Length;
            int other = english ? chinese : englishHeading;
            if (other > start) end = other;

            StringBuilder selected = new StringBuilder();
            for (int i = start + 1; i < end; i++) selected.AppendLine(lines[i]);
            string value = selected.ToString().Trim();
            return KeepChangeNotes(value.Length == 0 ? notes : value);
        }

        private static string KeepChangeNotes(string notes)
        {
            string[] lines = (notes ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            StringBuilder result = new StringBuilder();
            foreach (string sourceLine in lines)
            {
                string line = sourceLine.Trim();
                string heading = line.StartsWith("#", StringComparison.Ordinal)
                    ? line.TrimStart('#').Trim() : string.Empty;
                if (heading.Equals("安装", StringComparison.OrdinalIgnoreCase) ||
                    heading.Equals("Installation", StringComparison.OrdinalIgnoreCase) ||
                    heading.Equals("下载", StringComparison.OrdinalIgnoreCase) ||
                    heading.Equals("Download", StringComparison.OrdinalIgnoreCase)) break;
                if (line.StartsWith("SHA-256:", StringComparison.OrdinalIgnoreCase) ||
                    line == "---" || line == "***" || line == "___") continue;
                result.AppendLine(sourceLine);
            }
            return result.ToString().Trim();
        }

        private static int FindLanguageHeading(string[] lines, bool english)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("#", StringComparison.Ordinal)) continue;
                string heading = line.TrimStart('#').Trim();
                if (english && heading.Equals("English", StringComparison.OrdinalIgnoreCase)) return i;
                if (!english && (heading.Contains("中文") || heading.Equals("Chinese", StringComparison.OrdinalIgnoreCase))) return i;
            }
            return -1;
        }

        private static string CleanMarkdown(string value)
        {
            return (value ?? string.Empty).Replace("**", string.Empty).Replace("__", string.Empty)
                .Replace("`", string.Empty);
        }
    }
}
