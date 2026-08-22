using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace IRacingRadarConfigurator
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ConfiguratorForm(ResolveSettingsPath(args)));
        }

        private static string ResolveSettingsPath(string[] args)
        {
            if (args != null && args.Length > 0) return Path.GetFullPath(args[0]);
            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 6 && directory != null; i++, directory = directory.Parent)
            {
                string candidate = Path.Combine(directory.FullName, "IRacingRadar.settings.ini");
                if (File.Exists(candidate)) return candidate;
            }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IRacingRadar.settings.ini");
        }
    }

    public sealed partial class ConfiguratorForm : Form
    {
        private readonly string settingsPath;
        private readonly string preferencesPath;
        private readonly ComboBox displayMode = new ComboBox();
        private readonly NumericUpDown range = new NumericUpDown();
        private readonly CheckBox dynamicRange = new CheckBox();
        private readonly NumericUpDown dynamicRangeMinimum = new NumericUpDown();
        private readonly NumericUpDown near = new NumericUpDown();
        private readonly CheckBox frontArc = new CheckBox();
        private readonly CheckBox rearArc = new CheckBox();
        private readonly CheckBox catchEstimate = new CheckBox();
        private readonly CheckBox overtakePrediction = new CheckBox();
        private readonly CheckBox hideInQualifying = new CheckBox();
        private readonly CheckBox trackBackground = new CheckBox();
        private readonly CheckBox trackAlwaysVisible = new CheckBox();
        private readonly NumericUpDown trackScale = new NumericUpDown();
        private readonly NumericUpDown referenceTrackWidth = new NumericUpDown();
        private readonly TrackBar playerMarkerScale = new TrackBar();
        private readonly Label playerMarkerScaleValue = new Label();
        private readonly NumericUpDown time = new NumericUpDown();
        private readonly NumericUpDown fade = new NumericUpDown();
        private readonly NumericUpDown fontSize = new NumericUpDown();
        private readonly NumericUpDown opacity = new NumericUpDown();
        private readonly ComboBox scenario = new ComboBox();
        private readonly FlowLayoutPanel previewOptions = new FlowLayoutPanel();
        private readonly NumericUpDown previewDistance = new NumericUpDown();
        private readonly NumericUpDown previewTime = new NumericUpDown();
        private readonly ComboBox motion = new ComboBox();
        private readonly OverlayRadarPreviewControl preview = new OverlayRadarPreviewControl();
        private readonly Label status = new Label();
        private bool loading;

        public ConfiguratorForm(string settingsPath)
            : this(settingsPath, ConfiguratorPreferences.DefaultPath)
        {
        }

        internal ConfiguratorForm(string settingsPath, string preferencesPath)
        {
            this.settingsPath = settingsPath;
            this.preferencesPath = preferencesPath;
            Text = "iRacing Radar 配置工具 / Configurator";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1120, 1010);
            Size = new Size(1240, 1085);
            BackColor = Color.FromArgb(17, 21, 28);
            ForeColor = Color.FromArgb(235, 240, 247);
            Font = new Font("Segoe UI", 9.5f);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
            BuildInterface();
            InitializeFeatures();
            LoadSettings();
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12, 40, 12, 12),
                BackColor = BackColor
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 455));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(root);

            Panel leftCard = Card();
            leftCard.Margin = new Padding(0, 0, 10, 0);
            root.Controls.Add(leftCard, 0, 0);
            FlowLayoutPanel fields = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(20, 8, 14, 8)
            };
            leftCard.Controls.Add(fields);

            fields.Controls.Add(TitleBlock("iRacing Radar", "可视化配置工具  /  VISUAL CONFIGURATOR"));
            fields.Controls.Add(Section("提示条件  /  ALERT CONDITIONS"));
            ConfigureCombo(displayMode, new object[]
            {
                new Choice("距离和时间 / Both", "Both"), new Choice("仅距离 / Distance", "Distance"),
                new Choice("仅时间 / Time", "Time"), new Choice("不显示文字 / None", "None")
            });
            fields.Controls.Add(Field("显示模式 / Display mode", "控制触发条件和前后车辆文字。", displayMode));

            ConfigureNumber(range, 5, 200, 70, 0, 1, " m");
            fields.Controls.Add(Field("距离提示范围 / Radar range", "车辆进入该距离后开始提示。", range));
            ConfigureCheck(dynamicRange, "按警示阶段动态调整范围 / Dynamic range by alert stage");
            fields.Controls.Add(dynamicRange);
            ConfigureNumber(dynamicRangeMinimum, 5, 200, 35, 0, 1, " m");
            fields.Controls.Add(Field("红色警示最小范围 / Red-zone minimum", "绿色阶段用完整范围；进入红色警示时平滑缩小到该值。", dynamicRangeMinimum));
            ConfigureNumber(time, 0.1m, 30, 0.7m, 1, 0.1m, " s");
            fields.Controls.Add(Field("相对时间提示范围 / Time gap", "仅在“仅时间”或“距离和时间”模式下参与提示判断。", time));
            ConfigureNumber(near, 1, 100, 20, 0, 1, " m");
            fields.Controls.Add(Field("红色警示距离 / Near warning", "进入该距离后由绿色过渡为红色。", near));
            ConfigureNumber(fade, 1, 50, 15, 0, 1, " %");
            fields.Controls.Add(Field("边缘渐显比例 / Fade band", "只改变提示范围边缘的透明度变化。", fade));

            fields.Controls.Add(Section("显示效果  /  APPEARANCE"));
            ConfigureCheck(frontArc, "显示前方绿色提示条 / Front green arc");
            fields.Controls.Add(frontArc);
            ConfigureCheck(rearArc, "显示后方绿色提示条 / Rear green arc");
            fields.Controls.Add(rearArc);
            ConfigureCheck(catchEstimate, "显示预计追上信息 / Catch prediction");
            fields.Controls.Add(catchEstimate);
            ConfigureCheck(overtakePrediction, "在赛道上显示预计追上点 / Show catch point on track");
            fields.Controls.Add(overtakePrediction);
            ConfigureCheck(hideInQualifying, "排位赛时隐藏雷达 / Hide during qualifying");
            fields.Controls.Add(hideInQualifying);
            ConfigureCheck(trackBackground, "\u663e\u793a\u5c40\u90e8\u8d5b\u9053\u80cc\u666f / Local track background");
            fields.Controls.Add(trackBackground);
            ConfigureCheck(trackAlwaysVisible, "\u59cb\u7ec8\u663e\u793a\u8d5b\u9053\u548c\u96f7\u8fbe / Always show track and radar");
            fields.Controls.Add(trackAlwaysVisible);
            ConfigureNumber(trackScale, 2, 12, 3.5m, 1, 0.5m, " px/m");
            fields.Controls.Add(Field("\u8d5b\u9053\u7f29\u653e\u6bd4\u4f8b / Track scale",
                "\u6570\u503c\u8d8a\u5c0f\uff0c\u53ef\u89c1\u7684\u53c2\u8003\u8d5b\u9053\u8def\u6bb5\u8d8a\u957f\uff1b\u4e0d\u6539\u53d8\u672c\u8f66\u6807\u8bb0\u5927\u5c0f\u3002 / Lower values show a longer reference path without resizing the player marker.", trackScale));
            ConfigureNumber(referenceTrackWidth, 5, 20, 10.5m, 1, 0.5m, " m");
            fields.Controls.Add(Field("\u53c2\u8003\u8d5b\u9053\u5bbd\u5ea6 / Reference track width",
                "\u7528\u4e8e\u89c6\u89c9\u6bd4\u4f8b\uff1biRacing \u4e0d\u63d0\u4f9b\u771f\u5b9e\u8d5b\u9053\u8fb9\u754c\u5bbd\u5ea6\u3002", referenceTrackWidth));
            fields.Controls.Add(PlayerMarkerScaleField());
            ConfigureNumber(fontSize, 10, 36, 22, 0, 1, " px");
            fields.Controls.Add(Field("数值字体大小 / Label size", "前后距离与相对时间文字大小。", fontSize));
            ConfigureNumber(opacity, 0, 100, 92, 0, 1, " %");
            fields.Controls.Add(Field("整体透明度 / Overlay opacity", "0 完全透明，100 完全不透明。", opacity));

            Panel buttons = new Panel { Width = 397, Height = 42, Margin = new Padding(0, 8, 0, 0) };
            Button save = ActionButton("保存设置 / Save", Color.FromArgb(36, 169, 105), 0, 145);
            Button reload = ActionButton("重新读取 / Reload", Color.FromArgb(52, 64, 82), 153, 112);
            Button defaults = ActionButton("恢复默认 / Defaults", Color.FromArgb(52, 64, 82), 273, 120);
            save.Click += delegate { SaveSettings(); };
            reload.Click += delegate { LoadSettings(); };
            defaults.Click += delegate
            {
                ApplySettings(RadarConfiguratorSettings.Defaults());
                SetStatus("已载入默认值，点击保存后生效。", "Defaults loaded. Click Save to apply.", false);
            };
            buttons.Controls.Add(save); buttons.Controls.Add(reload); buttons.Controls.Add(defaults);
            fields.Controls.Add(buttons);

            status.Width = 397;
            status.Height = 30;
            status.ForeColor = Color.FromArgb(121, 214, 164);
            status.TextAlign = ContentAlignment.MiddleLeft;
            fields.Controls.Add(status);
            Label path = new Label
            {
                Width = 397,
                Height = 40,
                ForeColor = Color.FromArgb(144, 157, 176),
                Text = "配置文件 / Settings file:\n" + settingsPath,
                AutoEllipsis = true
            };
            fields.Controls.Add(path);

            Panel rightCard = Card();
            rightCard.Margin = new Padding(0);
            root.Controls.Add(rightCard, 1, 0);
            TableLayoutPanel right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(16) };
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rightCard.Controls.Add(right);
            right.Controls.Add(TitleBlock("雷达效果预览", ""), 0, 0);

            previewOptions.Dock = DockStyle.Fill;
            previewOptions.WrapContents = true;
            previewOptions.Padding = new Padding(0, 5, 0, 5);
            ConfigureCombo(scenario, new object[]
            {
                new ScenarioChoice("后方车辆：绿色提示 / Rear car: green alert", PreviewScenario.Rear, false),
                new ScenarioChoice("后方车辆：红色警示 / Rear car: red warning", PreviewScenario.Rear, true),
                new ScenarioChoice("左侧车辆：并排 / Left car: alongside", PreviewScenario.LeftParallel, null),
                new ScenarioChoice("前方车辆：红色警示 / Front car: red warning", PreviewScenario.Front, true),
                new ScenarioChoice("前方车辆：绿色提示 / Front car: green alert", PreviewScenario.Front, false)
            });
            scenario.Width = 265;
            ConfigureNumber(previewDistance, 0, 250, 35, 0, 1, " m");
            ConfigureNumber(previewTime, 0, 30, 0.5m, 1, 0.1m, " s");
            ConfigureCombo(motion, new object[] { new Choice("正在靠近 / Closing", "Closing"), new Choice("正在远离 / Separating", "Separating") });
            right.Controls.Add(previewOptions, 0, 1);
            preview.Dock = DockStyle.Fill;
            preview.Margin = new Padding(0, 4, 0, 0);
            right.Controls.Add(preview, 0, 2);

            HookChanges();
        }

        private void HookChanges()
        {
            EventHandler changed = delegate { if (!loading) UpdatePreview(); };
            displayMode.SelectedIndexChanged += changed;
            range.ValueChanged += delegate { if (!loading) { near.Maximum = Math.Min(100, range.Value); if (near.Value > near.Maximum) near.Value = near.Maximum; dynamicRangeMinimum.Maximum = range.Value; if (dynamicRangeMinimum.Value > dynamicRangeMinimum.Maximum) dynamicRangeMinimum.Value = dynamicRangeMinimum.Maximum; UpdatePreview(); } };
            dynamicRange.CheckedChanged += delegate { if (!loading) { dynamicRangeMinimum.Enabled = dynamicRange.Checked; UpdatePreview(); } };
            dynamicRangeMinimum.ValueChanged += changed;
            near.ValueChanged += changed; time.ValueChanged += changed; fade.ValueChanged += changed;
            fontSize.ValueChanged += changed; opacity.ValueChanged += changed;
            frontArc.CheckedChanged += delegate { if (!loading) OnGreenArcSettingChanged(); };
            rearArc.CheckedChanged += delegate { if (!loading) OnGreenArcSettingChanged(); };
            catchEstimate.CheckedChanged += delegate
            {
                if (!loading)
                {
                    overtakePrediction.Enabled = catchEstimate.Checked;
                    UpdatePreview();
                }
            };
            overtakePrediction.CheckedChanged += delegate { if (!loading) UpdatePreview(); };
            hideInQualifying.CheckedChanged += delegate { if (!loading) UpdatePreview(); };
            scenario.SelectedIndexChanged += changed; previewDistance.ValueChanged += changed;
            trackBackground.CheckedChanged += delegate
            {
                if (!loading)
                {
                    trackAlwaysVisible.Enabled = trackBackground.Checked;
                    trackScale.Enabled = trackBackground.Checked;
                    referenceTrackWidth.Enabled = trackBackground.Checked;
                    UpdatePreview();
                }
            };
            trackAlwaysVisible.CheckedChanged += changed;
            trackScale.ValueChanged += changed;
            referenceTrackWidth.ValueChanged += changed;
            playerMarkerScale.ValueChanged += delegate
            {
                playerMarkerScaleValue.Text = playerMarkerScale.Value + "%";
                if (!loading) UpdatePreview();
            };
            previewTime.ValueChanged += changed; motion.SelectedIndexChanged += changed;
        }

        private void LoadSettings()
        {
            try
            {
                ApplySettings(RadarConfiguratorSettings.Load(settingsPath));
                if (File.Exists(settingsPath))
                    SetStatus("已读取当前配置。", "Current settings loaded.", false);
                else
                    SetStatus("配置文件不存在，已载入默认值。", "Settings file was not found; defaults were loaded.", false);
            }
            catch (Exception ex)
            {
                SetStatus("读取失败：" + ex.Message, "Failed to load: " + ex.Message, true);
            }
        }

        private void SaveSettings()
        {
            try
            {
                RadarConfiguratorSettings settings = ReadSettings();
                settings.Save(settingsPath);
                if (SimHubRestartService.IsRunning())
                {
                    SetStatus("保存成功。请选择何时重启 SimHub。",
                        "Saved. Choose when to restart SimHub.", false);
                    PromptForSimHubRestart();
                }
                else
                    SetStatus("保存成功。SimHub 当前未运行，设置将在下次启动时生效。",
                        "Saved. SimHub is not running; settings will apply the next time it starts.", false);
            }
            catch (Exception ex)
            {
                SetStatus("保存失败：" + ex.Message, "Failed to save: " + ex.Message, true);
                MessageBox.Show(this, (english ? "Unable to save the settings file:\n" : "无法保存配置文件：\n") + ex.Message,
                    "iRacing Radar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PromptForSimHubRestart()
        {
            using (RestartSimHubDialog dialog = new RestartSimHubDialog(english, dayMode))
            {
                dialog.ShowDialog(this);
                if (dialog.Choice == RestartSimHubChoice.RestartNow)
                {
                    try
                    {
                        SimHubRestartService.Restart(settingsPath);
                        SetStatus("SimHub 已重新启动。", "SimHub was restarted.", false);
                    }
                    catch (Exception ex)
                    {
                        SetStatus("无法自动重启 SimHub：" + ex.Message,
                            "Unable to restart SimHub: " + ex.Message, true);
                        MessageBox.Show(this,
                            (english ? "Unable to restart SimHub automatically. Please restart it manually.\n\n"
                                : "无法自动重启 SimHub，请手动重启。\n\n") + ex.Message,
                            "iRacing Radar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (dialog.Choice == RestartSimHubChoice.Later)
                    SetStatus("设置已保存，请稍后手动重启 SimHub。",
                        "Settings saved. Restart SimHub manually later.", false);
                else
                    SetStatus("设置已保存，未执行重启。",
                        "Settings saved. Restart was cancelled.", false);
            }
        }
        private void ApplySettings(RadarConfiguratorSettings settings)
        {
            loading = true;
            try
            {
                SelectValue(displayMode, settings.DisplayMode);
                range.Value = DecimalValue(range, settings.RadarRangeMeters);
                dynamicRangeMinimum.Maximum = range.Value;
                dynamicRange.Checked = settings.DynamicRadarRangeEnabled;
                dynamicRangeMinimum.Value = DecimalValue(dynamicRangeMinimum, settings.DynamicRadarRangeMinimumMeters);
                dynamicRangeMinimum.Enabled = settings.DynamicRadarRangeEnabled;
                near.Maximum = Math.Min(100, range.Value);
                near.Value = DecimalValue(near, settings.NearDistanceMeters);
                frontArc.Checked = settings.FrontGreenArcEnabled;
                rearArc.Checked = settings.RearGreenArcEnabled;
                catchEstimate.Checked = settings.CatchEstimateEnabled;
                overtakePrediction.Checked = settings.OvertakePredictionEnabled;
                overtakePrediction.Enabled = settings.CatchEstimateEnabled;
                hideInQualifying.Checked = settings.HideInQualifying;
                time.Value = DecimalValue(time, settings.TimeAlertSeconds);
                fade.Value = DecimalValue(fade, settings.RadarFadeBandPercent);
                trackBackground.Checked = settings.TrackBackgroundEnabled;
                trackAlwaysVisible.Checked = settings.TrackBackgroundAlwaysVisible;
                trackAlwaysVisible.Enabled = settings.TrackBackgroundEnabled;
                trackScale.Enabled = settings.TrackBackgroundEnabled;
                referenceTrackWidth.Enabled = settings.TrackBackgroundEnabled;
                trackScale.Value = DecimalValue(trackScale, settings.TrackScalePixelsPerMeter);
                referenceTrackWidth.Value = DecimalValue(referenceTrackWidth, settings.ReferenceTrackWidthMeters);
                playerMarkerScale.Value = Math.Max(playerMarkerScale.Minimum,
                    Math.Min(playerMarkerScale.Maximum, (int)Math.Round(settings.PlayerMarkerScalePercent)));
                playerMarkerScaleValue.Text = playerMarkerScale.Value + "%";
                fontSize.Value = DecimalValue(fontSize, settings.LabelFontSize);
                opacity.Value = DecimalValue(opacity, settings.OverlayOpacity);
                if (scenario.SelectedIndex < 0) scenario.SelectedIndex = 0;
                if (motion.SelectedIndex < 0) motion.SelectedIndex = 0;
            }
            finally { loading = false; }
            UpdatePreview();
            UpdateSceneButtonStates();
        }

        private RadarConfiguratorSettings ReadSettings()
        {
            Choice mode = displayMode.SelectedItem as Choice;
            return new RadarConfiguratorSettings
            {
                DisplayMode = mode == null ? "Both" : mode.Value,
                RadarRangeMeters = (double)range.Value,
                DynamicRadarRangeEnabled = dynamicRange.Checked,
                DynamicRadarRangeMinimumMeters = (double)dynamicRangeMinimum.Value,
                NearDistanceMeters = (double)near.Value,
                FrontGreenArcEnabled = frontArc.Checked,
                RearGreenArcEnabled = rearArc.Checked,
                CatchEstimateEnabled = catchEstimate.Checked,
                OvertakePredictionEnabled = overtakePrediction.Checked,
                HideInQualifying = hideInQualifying.Checked,
                TimeAlertSeconds = (double)time.Value,
                RadarFadeBandPercent = (double)fade.Value,
                LabelFontSize = (double)fontSize.Value,
                TrackBackgroundEnabled = trackBackground.Checked,
                TrackBackgroundAlwaysVisible = trackAlwaysVisible.Checked,
                TrackScalePixelsPerMeter = (double)trackScale.Value,
                ReferenceTrackWidthMeters = (double)referenceTrackWidth.Value,
                PlayerMarkerScalePercent = playerMarkerScale.Value,
                OverlayOpacity = (double)opacity.Value
            };
        }

        private void UpdatePreview()
        {
            RadarConfiguratorSettings current = ReadSettings();
            preview.Settings = current;
            ScenarioChoice selected = scenario.SelectedItem as ScenarioChoice;
            preview.Scenario = selected == null ? PreviewScenario.Rear : selected.Value;
            if (!demoRunning && selected != null && selected.Near.HasValue)
            {
                double meters = selected.Near.Value
                    ? Math.Max(0, current.NearDistanceMeters * 0.45)
                    : current.NearDistanceMeters + (current.RadarRangeMeters - current.NearDistanceMeters) * 0.50;
                preview.DistanceMeters = meters;
                preview.TimeSeconds = current.RadarRangeMeters <= 0 ? 0
                    : current.TimeAlertSeconds * meters / current.RadarRangeMeters;
                preview.Closing = true;
            }
            else
            {
                preview.DistanceMeters = (double)previewDistance.Value;
                preview.TimeSeconds = (double)previewTime.Value;
                Choice selectedMotion = motion.SelectedItem as Choice;
                preview.Closing = selectedMotion == null || selectedMotion.Value == "Closing";
            }
            preview.English = english;
            preview.Invalidate();
        }

        private static Panel Card()
        {
            return new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 29, 38),
                BorderColor = Color.FromArgb(47, 58, 74),
                CornerRadius = 18,
                Padding = new Padding(1)
            };
        }

        private static Control TitleBlock(string title, string subtitle)
        {
            Panel panel = new Panel { Width = 397, Height = 44, Margin = new Padding(0, 0, 0, 2) };
            panel.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(245, 248, 252), AutoSize = true, Location = new Point(0, 0) });
            panel.Controls.Add(new Label { Text = subtitle, Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(69, 208, 139), AutoSize = true, Location = new Point(2, 29) });
            return panel;
        }

        private static Label Section(string text)
        {
            return new Label { Text = text, Width = 397, Height = 21, Padding = new Padding(0, 4, 0, 0), Margin = new Padding(0, 2, 0, 2), ForeColor = Color.FromArgb(115, 202, 158), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        }

        private static Panel Field(string title, string description, Control editor)
        {
            Panel panel = new Panel { Width = 397, Height = 49, Margin = new Padding(0, 0, 0, 1) };
            panel.Controls.Add(new Label { Text = title, Location = new Point(0, 1), Size = new Size(238, 20), ForeColor = Color.FromArgb(230, 236, 244) });
            panel.Controls.Add(new Label { Text = description, Location = new Point(0, 21), Size = new Size(242, 24), ForeColor = Color.FromArgb(136, 149, 168), Font = new Font("Segoe UI", 8.25f) });
            editor.Location = new Point(247, 5); editor.Width = 145;
            panel.Controls.Add(editor);
            return panel;
        }

        private Panel PlayerMarkerScaleField()
        {
            Panel panel = new Panel { Width = 397, Height = 44, Margin = new Padding(0, 0, 0, 2) };
            Label title = new Label
            {
                Text = "本车图标大小 / Player icon size",
                Location = new Point(0, 10),
                Size = new Size(215, 22),
                ForeColor = Color.FromArgb(230, 236, 244)
            };
            playerMarkerScale.Minimum = 50;
            playerMarkerScale.Maximum = 200;
            playerMarkerScale.Value = 100;
            playerMarkerScale.TickFrequency = 25;
            playerMarkerScale.TickStyle = TickStyle.None;
            playerMarkerScale.AutoSize = false;
            playerMarkerScale.Location = new Point(218, 5);
            playerMarkerScale.Size = new Size(130, 32);
            playerMarkerScale.BackColor = Color.FromArgb(24, 29, 38);
            playerMarkerScaleValue.Text = "100%";
            playerMarkerScaleValue.Location = new Point(348, 10);
            playerMarkerScaleValue.Size = new Size(48, 22);
            playerMarkerScaleValue.TextAlign = ContentAlignment.MiddleRight;
            playerMarkerScaleValue.ForeColor = Color.FromArgb(230, 236, 244);
            panel.Controls.Add(title);
            panel.Controls.Add(playerMarkerScale);
            panel.Controls.Add(playerMarkerScaleValue);
            return panel;
        }

        private static Panel MiniField(string title, Control editor)
        {
            Panel panel = new Panel { Width = editor.Width + 12, Height = 60, Margin = new Padding(0, 0, 10, 0) };
            panel.Controls.Add(new Label { Text = title, Location = new Point(0, 0), AutoSize = true, ForeColor = Color.FromArgb(145, 158, 177), Font = new Font("Segoe UI", 8) });
            editor.Location = new Point(0, 22);
            panel.Controls.Add(editor);
            return panel;
        }

        private static Button ActionButton(string text, Color color, int left, int width)
        {
            Button button = new Button { Text = text, Left = left, Top = 2, Width = width, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 } };
            ApplyRoundedRegion(button, 10);
            return button;
        }

        private static void ConfigureCombo(ComboBox combo, object[] values)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = Color.FromArgb(39, 47, 60);
            combo.ForeColor = Color.FromArgb(240, 244, 249);
            combo.Items.AddRange(values);
            combo.SelectedIndex = values.Length > 0 ? 0 : -1;
            combo.Height = 30;
        }

        private static void ConfigureNumber(NumericUpDown number, decimal minimum, decimal maximum, decimal value, int decimals, decimal increment, string suffix)
        {
            number.Minimum = minimum; number.Maximum = maximum; number.Value = value;
            number.DecimalPlaces = decimals; number.Increment = increment; number.ThousandsSeparator = false;
            number.BackColor = Color.FromArgb(39, 47, 60); number.ForeColor = Color.FromArgb(240, 244, 249);
            number.BorderStyle = BorderStyle.FixedSingle; number.Height = 30;
        }

        private static void ConfigureCheck(CheckBox check, string text)
        {
            check.Text = text; check.Width = 397; check.Height = 26; check.Margin = new Padding(0, 0, 0, 1);
            check.ForeColor = Color.FromArgb(230, 236, 244); check.FlatStyle = FlatStyle.Flat;
        }

        private static void SelectValue(ComboBox combo, string value)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                Choice choice = combo.Items[i] as Choice;
                if (choice != null && choice.Value == value) { combo.SelectedIndex = i; return; }
            }
            combo.SelectedIndex = 0;
        }

        private static decimal DecimalValue(NumericUpDown control, double value)
        {
            return Math.Max(control.Minimum, Math.Min(control.Maximum, (decimal)value));
        }

        private class Choice
        {
            public static bool UseEnglish;
            public readonly string Chinese;
            public readonly string English;
            public readonly string Value;
            public Choice(string text, string value)
            {
                SplitChoiceText(text, out Chinese, out English);
                Value = value;
            }
            public override string ToString() { return UseEnglish ? English : Chinese; }
        }

        private sealed class ScenarioChoice
        {
            public static bool UseEnglish;
            public readonly string Chinese;
            public readonly string English;
            public readonly PreviewScenario Value;
            public readonly bool? Near;
            public ScenarioChoice(string text, PreviewScenario value, bool? near)
            {
                SplitChoiceText(text, out Chinese, out English);
                Value = value;
                Near = near;
            }
            public override string ToString() { return UseEnglish ? English : Chinese; }
        }

        private static void SplitChoiceText(string text, out string chinese, out string english)
        {
            int separator = text.IndexOf(" / ", StringComparison.Ordinal);
            if (separator < 0) { chinese = text; english = text; return; }
            chinese = text.Substring(0, separator).Trim();
            english = text.Substring(separator + 3).Trim();
        }
    }
}
