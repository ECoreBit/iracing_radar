using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IRacingRadarConfigurator
{
    public sealed partial class ConfiguratorForm
    {
        private readonly System.Windows.Forms.Timer demoTimer = new System.Windows.Forms.Timer();
        private readonly Stopwatch demoClock = new Stopwatch();
        private readonly Dictionary<Control, LocalizedText> localizedControls = new Dictionary<Control, LocalizedText>();
        private MenuStrip menu;
        private ToolStripMenuItem languageMenu;
        private ToolStripMenuItem chineseMenuItem;
        private ToolStripMenuItem englishMenuItem;
        private ToolStripMenuItem themeMenu;
        private ToolStripMenuItem dayMenuItem;
        private ToolStripMenuItem nightMenuItem;
        private Button demoButton;
        private readonly Dictionary<Button, ScenarioChoice> sceneButtons = new Dictionary<Button, ScenarioChoice>();
        private bool english;
        private bool demoRunning;
        private bool dayMode;
        private bool applyingPreferences;
        private readonly Dictionary<Control, Color> nightBackColors = new Dictionary<Control, Color>();
        private readonly Dictionary<Control, Color> nightForeColors = new Dictionary<Control, Color>();

        private void InitializeFeatures()
        {
            BuildLanguageMenu();
            BuildSceneButtons();
            BuildDemoButton();
            PrepareLocalization(this);
            CaptureNightColors(this);
            ConfiguratorPreferences preferences = ConfiguratorPreferences.Load(preferencesPath);
            applyingPreferences = true;
            SetLanguage(preferences.English);
            SetTheme(preferences.DayMode);
            applyingPreferences = false;
            demoTimer.Interval = 33;
            demoTimer.Tick += delegate { UpdateDemoAnimation(); };
        }

        private void BuildLanguageMenu()
        {
            menu = new MenuStrip
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(28, 34, 44),
                ForeColor = Color.FromArgb(238, 243, 249),
                Renderer = new ThemeMenuRenderer(false)
            };
            languageMenu = new ToolStripMenuItem("语言");
            chineseMenuItem = new ToolStripMenuItem("中文") { Checked = true };
            englishMenuItem = new ToolStripMenuItem("English");
            chineseMenuItem.Click += delegate { SetLanguage(false); };
            englishMenuItem.Click += delegate { SetLanguage(true); };
            languageMenu.DropDownItems.Add(chineseMenuItem);
            languageMenu.DropDownItems.Add(englishMenuItem);
            themeMenu = new ToolStripMenuItem("主题");
            dayMenuItem = new ToolStripMenuItem("白天模式");
            nightMenuItem = new ToolStripMenuItem("夜间模式") { Checked = true };
            dayMenuItem.Click += delegate { SetTheme(true); };
            nightMenuItem.Click += delegate { SetTheme(false); };
            themeMenu.DropDownItems.Add(dayMenuItem);
            themeMenu.DropDownItems.Add(nightMenuItem);
            menu.Items.Add(languageMenu);
            menu.Items.Add(themeMenu);
            MainMenuStrip = menu;
            Controls.Add(menu);
            PerformLayout();
            menu.BringToFront();
        }

        private void CaptureNightColors(Control root)
        {
            if (!nightBackColors.ContainsKey(root))
            {
                nightBackColors[root] = root.BackColor;
                nightForeColors[root] = root.ForeColor;
            }
            foreach (Control child in root.Controls) CaptureNightColors(child);
        }

        private void SetTheme(bool useDayMode)
        {
            dayMode = useDayMode;
            foreach (KeyValuePair<Control, Color> pair in nightBackColors)
            {
                Control control = pair.Key;
                control.BackColor = useDayMode ? DayBackground(pair.Value) : pair.Value;
                Color originalFore;
                if (nightForeColors.TryGetValue(control, out originalFore))
                    control.ForeColor = useDayMode ? DayForeground(originalFore, pair.Value) : originalFore;
            }

            menu.BackColor = useDayMode ? Color.FromArgb(244, 247, 251) : Color.FromArgb(28, 34, 44);
            menu.ForeColor = useDayMode ? Color.FromArgb(28, 37, 51) : Color.FromArgb(238, 243, 249);
            menu.Renderer = new ThemeMenuRenderer(useDayMode);
            languageMenu.ForeColor = menu.ForeColor;
            themeMenu.ForeColor = menu.ForeColor;
            chineseMenuItem.ForeColor = useDayMode ? Color.FromArgb(28, 37, 51) : Color.FromArgb(238, 243, 249);
            englishMenuItem.ForeColor = chineseMenuItem.ForeColor;
            dayMenuItem.ForeColor = chineseMenuItem.ForeColor;
            nightMenuItem.ForeColor = chineseMenuItem.ForeColor;
            dayMenuItem.Checked = useDayMode;
            nightMenuItem.Checked = !useDayMode;
            preview.DayMode = useDayMode;
            foreach (Control control in nightBackColors.Keys)
            {
                RoundedPanel rounded = control as RoundedPanel;
                if (rounded != null)
                    rounded.BorderColor = useDayMode
                        ? Color.FromArgb(205, 215, 228)
                        : Color.FromArgb(50, 62, 79);
            }
            UpdateSceneButtonStates();
            UpdateDemoButtonText();
            Invalidate(true);
            SaveInterfacePreferences();
        }

        private static Color DayBackground(Color color)
        {
            if (color.ToArgb() == Color.FromArgb(17, 21, 28).ToArgb()) return Color.FromArgb(235, 240, 247);
            if (color.ToArgb() == Color.FromArgb(24, 29, 38).ToArgb()) return Color.White;
            if (color.ToArgb() == Color.FromArgb(39, 47, 60).ToArgb()) return Color.FromArgb(241, 245, 250);
            if (color.ToArgb() == Color.FromArgb(52, 64, 82).ToArgb()) return Color.FromArgb(221, 228, 237);
            if (color.ToArgb() == Color.FromArgb(28, 34, 44).ToArgb()) return Color.FromArgb(244, 247, 251);
            return color;
        }

        private static Color DayForeground(Color color, Color originalBackground)
        {
            if (originalBackground.ToArgb() == Color.FromArgb(52, 64, 82).ToArgb())
                return Color.FromArgb(31, 42, 58);
            if (color.ToArgb() == Color.FromArgb(245, 248, 252).ToArgb()) return Color.FromArgb(23, 32, 48);
            if (color.ToArgb() == Color.FromArgb(235, 240, 247).ToArgb()) return Color.FromArgb(31, 42, 58);
            if (color.ToArgb() == Color.FromArgb(230, 236, 244).ToArgb()) return Color.FromArgb(37, 49, 70);
            if (color.ToArgb() == Color.FromArgb(240, 244, 249).ToArgb()) return Color.FromArgb(23, 32, 48);
            if (color.ToArgb() == Color.FromArgb(136, 149, 168).ToArgb()) return Color.FromArgb(94, 109, 132);
            if (color.ToArgb() == Color.FromArgb(144, 157, 176).ToArgb()) return Color.FromArgb(91, 105, 125);
            if (color.ToArgb() == Color.FromArgb(115, 202, 158).ToArgb()) return Color.FromArgb(24, 132, 88);
            if (color.ToArgb() == Color.FromArgb(69, 208, 139).ToArgb()) return Color.FromArgb(24, 132, 88);
            return color;
        }
        private void BuildSceneButtons()
        {
            foreach (object item in scenario.Items)
            {
                ScenarioChoice choice = item as ScenarioChoice;
                if (choice == null) continue;
                Button button = new Button
                {
                    Width = 102,
                    Height = 32,
                    Margin = new Padding(0, 6, 6, 0),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.White,
                    UseVisualStyleBackColor = false,
                    Cursor = Cursors.Hand
                };
                button.FlatAppearance.BorderSize = 0;
                ApplyRoundedRegion(button, 11);
                ScenarioChoice captured = choice;
                button.Click += delegate
                {
                    scenario.SelectedItem = captured;
                    UpdateSceneButtonStates();
                    UpdatePreview();
                };
                sceneButtons[button] = choice;
                previewOptions.Controls.Add(button);
            }
            scenario.SelectedIndexChanged += delegate { UpdateSceneButtonStates(); };
            UpdateSceneButtonTexts();
            UpdateSceneButtonStates();
        }

        private void BuildDemoButton()
        {
            previewOptions.WrapContents = true;
            TableLayoutPanel table = previewOptions.Parent as TableLayoutPanel;
            if (table != null && table.RowStyles.Count > 1) table.RowStyles[1].Height = 82;
            demoButton = new Button
            {
                Width = 118,
                Height = 32,
                Margin = new Padding(0, 6, 0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(38, 153, 102),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            demoButton.FlatAppearance.BorderSize = 0;
            ApplyRoundedRegion(demoButton, 11);
            demoButton.Tag = new LocalizedText("动态演示", "Play demo");
            demoButton.Click += delegate { ToggleDemo(); };
            previewOptions.Controls.Add(demoButton);
        }

        private void UpdateSceneButtonTexts()
        {
            foreach (KeyValuePair<Button, ScenarioChoice> pair in sceneButtons)
            {
                ScenarioChoice choice = pair.Value;
                string chinese;
                string englishText;
                if (choice.Value == PreviewScenario.LeftParallel)
                {
                    chinese = "左侧并排";
                    englishText = "Left side";
                }
                else
                {
                    bool rear = choice.Value == PreviewScenario.Rear;
                    bool near = choice.Near.HasValue && choice.Near.Value;
                    chinese = (rear ? "后方" : "前方") + (near ? "红色" : "绿色");
                    englishText = (rear ? "Rear " : "Front ") + (near ? "red" : "green");
                }
                pair.Key.Text = english ? englishText : chinese;
            }
        }

        private void UpdateSceneButtonStates()
        {
            ScenarioChoice selected = scenario.SelectedItem as ScenarioChoice;
            foreach (KeyValuePair<Button, ScenarioChoice> pair in sceneButtons)
            {
                bool active = ReferenceEquals(pair.Value, selected);
                bool available = IsScenarioChoiceAvailable(pair.Value);
                pair.Key.Enabled = !demoRunning && available;
                pair.Key.Cursor = pair.Key.Enabled ? Cursors.Hand : Cursors.Default;
                if (!available)
                {
                    Color disabledBack = dayMode ? Color.FromArgb(243, 245, 248) : Color.FromArgb(34, 40, 50);
                    Color disabledText = dayMode ? Color.FromArgb(177, 184, 194) : Color.FromArgb(92, 103, 119);
                    pair.Key.BackColor = disabledBack;
                    pair.Key.ForeColor = disabledText;
                    pair.Key.FlatAppearance.MouseOverBackColor = disabledBack;
                    pair.Key.FlatAppearance.MouseDownBackColor = disabledBack;
                    continue;
                }
                if (active)
                {
                    bool side = pair.Value.Value == PreviewScenario.LeftParallel;
                    bool near = pair.Value.Near.HasValue && pair.Value.Near.Value;
                    pair.Key.BackColor = side ? Color.FromArgb(213, 126, 45) :
                        near ? Color.FromArgb(211, 51, 70) : Color.FromArgb(36, 169, 105);
                    pair.Key.ForeColor = Color.White;
                    pair.Key.FlatAppearance.BorderColor = Color.FromArgb(230, 240, 247);
                }
                else
                {
                    pair.Key.BackColor = dayMode ? Color.FromArgb(221, 228, 237) : Color.FromArgb(52, 64, 82);
                    pair.Key.ForeColor = dayMode ? Color.FromArgb(31, 42, 58) : Color.FromArgb(225, 232, 241);
                    pair.Key.FlatAppearance.BorderColor = dayMode ? Color.FromArgb(190, 200, 213) : Color.FromArgb(72, 88, 109);
                }
                pair.Key.FlatAppearance.MouseOverBackColor = ControlPaint.Light(pair.Key.BackColor, 0.05f);
                pair.Key.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(pair.Key.BackColor, 0.05f);
            }
        }        private void ToggleDemo()
        {
            demoRunning = !demoRunning;
            scenario.Enabled = !demoRunning;
            preview.AnimateTransitions = demoRunning;
            preview.TransitionElapsedSeconds = demoTimer.Interval / 1000.0;
            if (demoRunning)
            {
                demoClock.Restart();
                demoTimer.Start();
                UpdateDemoAnimation();
            }
            else
            {
                demoTimer.Stop();
                demoClock.Stop();
            }
            UpdateDemoButtonText();
            UpdateSceneButtonStates();
            if (!demoRunning) UpdatePreview();
        }

        private void UpdateDemoAnimation()
        {
            if (!demoRunning) return;
            RadarConfiguratorSettings current = ReadSettings();
            double outsideDistance = Math.Max(current.RadarRangeMeters * 1.16, current.NearDistanceMeters + 8);
            double nearBoundary = Math.Max(2.1, Math.Min(current.NearDistanceMeters, outsideDistance - 0.1));
            double rearGreenDuration = current.RearGreenArcEnabled ? 2.8 : 0;
            double rearRedDuration = 2.0;
            double sideDuration = 2.2;
            double frontRedDuration = 2.0;
            double frontGreenDuration = current.FrontGreenArcEnabled ? 2.8 : 0;
            double idleDuration = 0.8;
            double duration = rearGreenDuration + rearRedDuration + sideDuration + frontRedDuration + frontGreenDuration + idleDuration;
            double elapsed = demoClock.Elapsed.TotalSeconds % duration;
            double distance;
            double timeGap;
            PreviewScenario state;
            bool closing;

            if (elapsed < rearGreenDuration)
            {
                double t = Smooth(elapsed / rearGreenDuration);
                state = PreviewScenario.Rear;
                distance = Lerp(outsideDistance, nearBoundary, t);
                closing = true;
            }
            else if ((elapsed -= rearGreenDuration) < rearRedDuration)
            {
                double t = Smooth(elapsed / rearRedDuration);
                state = PreviewScenario.Rear;
                distance = Lerp(nearBoundary, 2.0, t);
                closing = true;
            }
            else if ((elapsed -= rearRedDuration) < sideDuration)
            {
                double relative = RadarOverlayMath.OvertakeRelative(elapsed / sideDuration);
                if (relative > 0.15)
                {
                    state = PreviewScenario.LeftBehind;
                    distance = relative;
                }
                else if (relative < -0.15)
                {
                    state = PreviewScenario.LeftAhead;
                    distance = -relative;
                }
                else
                {
                    state = PreviewScenario.LeftParallel;
                    distance = 0;
                }
                closing = true;
            }
            else if ((elapsed -= sideDuration) < frontRedDuration)
            {
                double t = Smooth(elapsed / frontRedDuration);
                state = PreviewScenario.Front;
                distance = Lerp(2.0, nearBoundary, t);
                closing = false;
            }
            else if ((elapsed -= frontRedDuration) < frontGreenDuration)
            {
                double t = Smooth(elapsed / frontGreenDuration);
                state = PreviewScenario.Front;
                distance = Lerp(nearBoundary, outsideDistance, t);
                closing = false;
            }
            else
            {
                state = PreviewScenario.NoCars;
                distance = outsideDistance;
                closing = false;
            }

            double timeOutside = Math.Max(current.TimeAlertSeconds * 1.18, 0.2);
            timeGap = outsideDistance <= 0 ? 0 : Math.Min(30, distance / outsideDistance * timeOutside);
            loading = true;
            try
            {
                SelectScenario(state, distance <= current.NearDistanceMeters);
                previewDistance.Value = ClampDecimal(previewDistance, distance);
                previewTime.Value = ClampDecimal(previewTime, timeGap);
                motion.SelectedIndex = closing ? 0 : 1;
            }
            finally { loading = false; }
            UpdatePreview();
            preview.Scenario = state;
            preview.Invalidate();
        }

        private void OnGreenArcSettingChanged()
        {
            EnsureSelectedScenarioAvailable();
            UpdateSceneButtonStates();
            UpdatePreview();
        }

        private bool IsScenarioChoiceAvailable(ScenarioChoice choice)
        {
            if (choice == null || !choice.Near.HasValue || choice.Near.Value) return true;
            if (choice.Value == PreviewScenario.Front) return frontArc.Checked;
            if (choice.Value == PreviewScenario.Rear) return rearArc.Checked;
            return true;
        }

        private void EnsureSelectedScenarioAvailable()
        {
            ScenarioChoice selected = scenario.SelectedItem as ScenarioChoice;
            if (IsScenarioChoiceAvailable(selected)) return;
            for (int i = 0; i < scenario.Items.Count; i++)
            {
                ScenarioChoice candidate = scenario.Items[i] as ScenarioChoice;
                if (candidate != null && candidate.Value == selected.Value &&
                    candidate.Near.HasValue && candidate.Near.Value)
                {
                    scenario.SelectedIndex = i;
                    return;
                }
            }
        }

        private static void ApplyRoundedRegion(Control control, int radius)
        {
            EventHandler update = delegate
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                using (GraphicsPath path = RoundedPath(new Rectangle(0, 0, control.Width, control.Height), radius))
                {
                    control.Region = new Region(path);
                }
            };
            control.SizeChanged += update;
            update(control, EventArgs.Empty);
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(2, radius * 2);
            Rectangle arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private sealed class RoundedPanel : Panel
        {
            public int CornerRadius { get; set; }
            public Color BorderColor { get; set; }

            public RoundedPanel()
            {
                DoubleBuffered = true;
                CornerRadius = 16;
                BorderColor = Color.Transparent;
                Resize += delegate { UpdateShape(); };
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                UpdateShape();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle borderBounds = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
                using (GraphicsPath path = RoundedPath(borderBounds, Math.Max(2, CornerRadius - 1)))
                using (Pen pen = new Pen(BorderColor, 1))
                    e.Graphics.DrawPath(pen, path);
            }

            private void UpdateShape()
            {
                if (Width <= 0 || Height <= 0) return;
                using (GraphicsPath path = RoundedPath(new Rectangle(0, 0, Width, Height), CornerRadius))
                {
                    Region = new Region(path);
                }
            }
        }
        private void SelectScenario(PreviewScenario value, bool near)
        {
            for (int i = 0; i < scenario.Items.Count; i++)
            {
                ScenarioChoice choice = scenario.Items[i] as ScenarioChoice;
                bool side = value == PreviewScenario.LeftBehind ||
                    value == PreviewScenario.LeftParallel || value == PreviewScenario.LeftAhead;
                bool positionMatches = choice != null &&
                    (side ? choice.Value == PreviewScenario.LeftParallel : choice.Value == value);
                if (positionMatches &&
                    (side ? !choice.Near.HasValue : choice.Near.HasValue && choice.Near.Value == near))
                {
                    scenario.SelectedIndex = i;
                    return;
                }
            }
        }

        private void SetLanguage(bool useEnglish)
        {
            english = useEnglish;
            Choice.UseEnglish = useEnglish;
            ScenarioChoice.UseEnglish = useEnglish;
            foreach (KeyValuePair<Control, LocalizedText> pair in localizedControls)
                pair.Key.Text = useEnglish ? pair.Value.English : pair.Value.Chinese;
            Text = useEnglish ? "iRacing Radar Configurator" : "iRacing Radar 配置工具";
            languageMenu.Text = useEnglish ? "Language" : "语言";
            themeMenu.Text = useEnglish ? "Theme" : "主题";
            dayMenuItem.Text = useEnglish ? "Day mode" : "白天模式";
            nightMenuItem.Text = useEnglish ? "Night mode" : "夜间模式";
            chineseMenuItem.Checked = !useEnglish;
            englishMenuItem.Checked = useEnglish;
            RefreshCombo(displayMode);
            RefreshCombo(scenario);
            RefreshCombo(motion);
            preview.English = useEnglish;
            UpdateDemoButtonText();
            UpdateSceneButtonTexts();
            UpdateSceneButtonStates();
            status.Text = useEnglish ? "Language changed. Unsaved settings are preserved." : "语言已切换，未保存的设置会保留。";
            preview.Invalidate();
            SaveInterfacePreferences();
        }

        private void SaveInterfacePreferences()
        {
            if (applyingPreferences) return;
            try
            {
                new ConfiguratorPreferences { English = english, DayMode = dayMode }.Save(preferencesPath);
            }
            catch { }
        }

        private void PrepareLocalization(Control root)
        {
            foreach (Control control in root.Controls)
            {
                LocalizedText pair = control.Tag as LocalizedText;
                if (pair == null) pair = CreateTranslation(control.Text);
                if (pair != null)
                {
                    localizedControls[control] = pair;
                    control.Tag = pair;
                }
                PrepareLocalization(control);
            }
        }

        private LocalizedText CreateTranslation(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            if (text.StartsWith("配置文件 / Settings file:\n", StringComparison.Ordinal))
            {
                string path = text.Substring(text.IndexOf('\n') + 1);
                return new LocalizedText("配置文件：\n" + path, "Settings file:\n" + path);
            }
            int separator = text.IndexOf(" / ", StringComparison.Ordinal);
            if (separator >= 0)
                return new LocalizedText(text.Substring(0, separator).Trim(), text.Substring(separator + 3).Trim());
            string translated;
            if (Translations.TryGetValue(text, out translated)) return new LocalizedText(text, translated);
            return null;
        }

        private void UpdateDemoButtonText()
        {
            if (demoButton == null) return;
            if (demoRunning)
                demoButton.Text = english ? "Stop demo" : "停止动态演示";
            else
                demoButton.Text = english ? "Play demo" : "播放动态演示";
        }

        private static void RefreshCombo(ComboBox combo)
        {
            object selected = combo.SelectedItem;
            object[] items = new object[combo.Items.Count];
            combo.Items.CopyTo(items, 0);
            combo.BeginUpdate();
            combo.Items.Clear();
            combo.Items.AddRange(items);
            combo.SelectedItem = selected;
            if (combo.SelectedIndex < 0 && combo.Items.Count > 0) combo.SelectedIndex = 0;
            combo.EndUpdate();
        }

        private void SetStatus(string chinese, string englishText, bool error)
        {
            status.ForeColor = error
                ? (dayMode ? Color.FromArgb(190, 42, 57) : Color.FromArgb(255, 117, 126))
                : (dayMode ? Color.FromArgb(24, 132, 88) : Color.FromArgb(121, 214, 164));
            status.Text = english ? englishText : chinese;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            demoTimer.Stop();
            demoTimer.Dispose();
            base.OnFormClosed(e);
        }

        private static decimal ClampDecimal(NumericUpDown control, double value)
        {
            return Math.Max(control.Minimum, Math.Min(control.Maximum, (decimal)value));
        }

        private static double Lerp(double from, double to, double amount)
        {
            return from + (to - from) * Math.Max(0, Math.Min(1, amount));
        }

        private static double Smooth(double value)
        {
            value = Math.Max(0, Math.Min(1, value));
            return value * value * (3 - 2 * value);
        }

        private sealed class LocalizedText
        {
            public readonly string Chinese;
            public readonly string English;
            public LocalizedText(string chinese, string english) { Chinese = chinese; English = english; }
        }

        private sealed class ThemeMenuRenderer : ToolStripProfessionalRenderer
        {
            private readonly bool day;
            public ThemeMenuRenderer(bool dayMode) : base(new ThemeColorTable(dayMode)) { day = dayMode; }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = day ? Color.FromArgb(28, 37, 51) : Color.FromArgb(238, 243, 249);
                base.OnRenderItemText(e);
            }
        }

        private sealed class ThemeColorTable : ProfessionalColorTable
        {
            private readonly bool day;
            public ThemeColorTable(bool dayMode) { day = dayMode; }
            private Color Surface { get { return day ? Color.FromArgb(244, 247, 251) : Color.FromArgb(28, 34, 44); } }
            private Color DropDown { get { return day ? Color.White : Color.FromArgb(31, 38, 49); } }
            private Color Selected { get { return day ? Color.FromArgb(222, 230, 240) : Color.FromArgb(48, 59, 75); } }

            public override Color MenuStripGradientBegin { get { return Surface; } }
            public override Color MenuStripGradientEnd { get { return Surface; } }
            public override Color ToolStripGradientBegin { get { return Surface; } }
            public override Color ToolStripGradientMiddle { get { return Surface; } }
            public override Color ToolStripGradientEnd { get { return Surface; } }
            public override Color MenuItemSelected { get { return Selected; } }
            public override Color MenuItemSelectedGradientBegin { get { return Selected; } }
            public override Color MenuItemSelectedGradientEnd { get { return Selected; } }
            public override Color MenuItemPressedGradientBegin { get { return Selected; } }
            public override Color MenuItemPressedGradientMiddle { get { return Selected; } }
            public override Color MenuItemPressedGradientEnd { get { return Selected; } }
            public override Color MenuItemBorder { get { return day ? Color.FromArgb(185, 197, 213) : Color.FromArgb(72, 88, 109); } }
            public override Color ToolStripDropDownBackground { get { return DropDown; } }
            public override Color ImageMarginGradientBegin { get { return DropDown; } }
            public override Color ImageMarginGradientMiddle { get { return DropDown; } }
            public override Color ImageMarginGradientEnd { get { return DropDown; } }
        }
        private static readonly Dictionary<string, string> Translations = new Dictionary<string, string>
        {
            { "控制触发条件和前后车辆文字。", "Controls alert conditions and front/rear labels." },
            { "车辆进入该距离后开始提示。", "The alert starts when a car enters this distance." },
            { "仅在“仅时间”或“距离和时间”模式下参与提示判断。", "Used by Time and Both modes." },
            { "进入该距离后由绿色过渡为红色。", "The alert transitions from green to red inside this distance." },
            { "只改变提示范围边缘的透明度变化。", "Controls opacity near the edge of the alert range." },
            { "前后距离与相对时间文字大小。", "Font size for front/rear distance and time labels." },
            { "0 完全透明，100 完全不透明。", "0 is invisible; 100 is fully opaque." },
            { "雷达效果预览", "Radar preview" },
        };
    }
}
