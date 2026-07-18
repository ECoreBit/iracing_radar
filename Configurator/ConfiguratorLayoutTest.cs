using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace IRacingRadarConfigurator
{
    internal static class ConfiguratorLayoutTest
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string root = args.Length > 0 ? args[0] : AppDomain.CurrentDomain.BaseDirectory;
            string preferencesPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "IRacingRadar-layout-" + Guid.NewGuid().ToString("N") + ".ini");
            using (ConfiguratorForm form = new ConfiguratorForm(
                System.IO.Path.Combine(root, "IRacingRadar.settings.ini"), preferencesPath))
            {
                form.CreateControl();
                form.PerformLayout();
                MenuStrip menu = (MenuStrip)GetField(form, "menu");
                if (menu.Items.Count != 2 || menu.Items[0].Text != "语言" || menu.Items[1].Text != "主题")
                    throw new InvalidOperationException("Language/theme menus are missing.");
                if (Contrast(menu.BackColor, menu.ForeColor) < 4.5)
                    throw new InvalidOperationException("Night menu text contrast is too low.");

                IDictionary buttons = (IDictionary)GetField(form, "sceneButtons");
                if (buttons.Count != 5) throw new InvalidOperationException("Expected five direct scene buttons.");
                foreach (DictionaryEntry entry in buttons)
                {
                    Button sceneButton = (Button)entry.Key;
                    if (sceneButton.FlatAppearance.BorderSize != 0)
                        throw new InvalidOperationException("Scene buttons must not draw a white selection border.");
                }
                FlowLayoutPanel options = (FlowLayoutPanel)GetField(form, "previewOptions");
                if (options.Controls.Count != 6)
                    throw new InvalidOperationException("Preview should contain five scene buttons and one demo button.");

                CheckBox frontArc = (CheckBox)GetField(form, "frontArc");
                CheckBox rearArc = (CheckBox)GetField(form, "rearArc");
                ComboBox scenario = (ComboBox)GetField(form, "scenario");
                Button frontGreen = FindSceneButton(buttons, PreviewScenario.Front, false);
                Button frontRed = FindSceneButton(buttons, PreviewScenario.Front, true);
                Button rearGreen = FindSceneButton(buttons, PreviewScenario.Rear, false);
                Button rearRed = FindSceneButton(buttons, PreviewScenario.Rear, true);
                if (frontGreen.Region == null || rearGreen.Region == null)
                    throw new InvalidOperationException("Scene buttons are not rounded.");

                frontArc.Checked = true;
                rearArc.Checked = true;
                scenario.SelectedItem = FindSceneChoice(buttons, PreviewScenario.Front, false);
                frontArc.Checked = false;
                Application.DoEvents();
                if (frontGreen.Enabled || !frontRed.Enabled || !SelectedScenarioIs(scenario, PreviewScenario.Front, true))
                {
                    Console.Error.WriteLine("FAIL front sync: green={0}, red={1}, selected={2}", frontGreen.Enabled, frontRed.Enabled, scenario.SelectedItem);
                    return 1;
                }
                frontArc.Checked = true;

                scenario.SelectedItem = FindSceneChoice(buttons, PreviewScenario.Rear, false);
                rearArc.Checked = false;
                Application.DoEvents();
                if (rearGreen.Enabled || !rearRed.Enabled || !SelectedScenarioIs(scenario, PreviewScenario.Rear, true))
                {
                    Console.Error.WriteLine("FAIL rear sync: green={0}, red={1}, selected={2}", rearGreen.Enabled, rearRed.Enabled, scenario.SelectedItem);
                    return 1;
                }
                rearArc.Checked = true;

                foreach (Control control in AllControls(form))
                {
                    FlowLayoutPanel flow = control as FlowLayoutPanel;
                    if (flow != null && flow.AutoScroll)
                        throw new InvalidOperationException("Native light scrollbars must not be enabled.");
                    if (flow != null && flow.Controls.Count > 10)
                    {
                        flow.PerformLayout();
                        int contentBottom = flow.Padding.Top;
                        foreach (Control child in flow.Controls)
                            contentBottom = Math.Max(contentBottom, child.Bottom + child.Margin.Bottom);
                        if (contentBottom + flow.Padding.Bottom > flow.ClientSize.Height)
                            throw new InvalidOperationException("Settings content is clipped after removing the scrollbar.");
                    }
                }

                Invoke(form, "SetTheme", true);
                if (Contrast(menu.BackColor, menu.ForeColor) < 4.5)
                    throw new InvalidOperationException("Day menu text contrast is too low.");
                OverlayRadarPreviewControl preview = (OverlayRadarPreviewControl)GetField(form, "preview");
                if (!preview.DayMode) throw new InvalidOperationException("Preview did not enter day mode.");
                int roundedCards = 0;
                foreach (Control control in AllControls(form))
                {
                    if (control.GetType().Name != "RoundedPanel") continue;
                    roundedCards++;
                    Color border = (Color)control.GetType().GetProperty("BorderColor").GetValue(control, null);
                    if (border.R < 180 || border.G < 180 || border.B < 180)
                        throw new InvalidOperationException("Day-mode card border is still too dark.");
                    if (control.Region == null || control.Region.IsVisible(0, 0) ||
                        !control.Region.IsVisible(control.Width / 2, 1))
                        throw new InvalidOperationException("Rounded card clipping is inconsistent.");
                }
                if (roundedCards != 2) throw new InvalidOperationException("Expected two rounded configuration cards.");

                Invoke(form, "SetLanguage", true);
                if (menu.Items[0].Text != "Language" || menu.Items[1].Text != "Theme")
                    throw new InvalidOperationException("English menu labels did not update.");

                using (RestartSimHubDialog dialog = new RestartSimHubDialog(false, true))
                {
                    dialog.CreateControl();
                    if (dialog.Choice != RestartSimHubChoice.Cancel ||
                        !HasButton(dialog, "现在重启") || !HasButton(dialog, "稍后手动重启") || !HasButton(dialog, "取消"))
                        throw new InvalidOperationException("Chinese restart prompt is incomplete.");
                }
                using (RestartSimHubDialog dialog = new RestartSimHubDialog(true, false))
                {
                    dialog.CreateControl();
                    if (!HasButton(dialog, "Restart now") || !HasButton(dialog, "Restart later") || !HasButton(dialog, "Cancel"))
                        throw new InvalidOperationException("English restart prompt is incomplete.");
                }
                string simHub = SimHubRestartService.FindExecutable(
                    System.IO.Path.Combine(root, "IRacingRadar.settings.ini"));
                if (!System.IO.File.Exists(simHub) || !simHub.EndsWith("SimHubWPF.exe", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("SimHub executable discovery failed.");
            }
            using (ConfiguratorForm restored = new ConfiguratorForm(
                System.IO.Path.Combine(root, "IRacingRadar.settings.ini"), preferencesPath))
            {
                restored.CreateControl();
                MenuStrip restoredMenu = (MenuStrip)GetField(restored, "menu");
                OverlayRadarPreviewControl restoredPreview = (OverlayRadarPreviewControl)GetField(restored, "preview");
                if (restoredMenu.Items[0].Text != "Language" || !restoredPreview.DayMode)
                    throw new InvalidOperationException("Language or theme preference was not restored.");
            }
            if (System.IO.File.Exists(preferencesPath)) System.IO.File.Delete(preferencesPath);
            Console.WriteLine("PASS configurator layout, buttons, menu contrast, themes, and preference restore");
            return 0;
        }

        private static object FindSceneChoice(IDictionary buttons, PreviewScenario scenario, bool near)
        {
            foreach (DictionaryEntry entry in buttons)
            {
                object choice = entry.Value;
                PreviewScenario value = (PreviewScenario)GetField(choice, "Value");
                bool? choiceNear = (bool?)GetField(choice, "Near");
                if (value == scenario && choiceNear.HasValue && choiceNear.Value == near)
                    return choice;
            }
            throw new InvalidOperationException("Scene choice was not found.");
        }
        private static Button FindSceneButton(IDictionary buttons, PreviewScenario scenario, bool near)
        {
            foreach (DictionaryEntry entry in buttons)
            {
                object choice = entry.Value;
                PreviewScenario value = (PreviewScenario)GetField(choice, "Value");
                bool? choiceNear = (bool?)GetField(choice, "Near");
                if (value == scenario && choiceNear.HasValue && choiceNear.Value == near)
                    return (Button)entry.Key;
            }
            throw new InvalidOperationException("Scene button was not found.");
        }

        private static bool SelectedScenarioIs(ComboBox combo, PreviewScenario scenario, bool near)
        {
            object choice = combo.SelectedItem;
            return choice != null && (PreviewScenario)GetField(choice, "Value") == scenario &&
                (bool?)GetField(choice, "Near") == near;
        }
        private static bool HasButton(Control root, string text)
        {
            foreach (Control control in AllControls(root))
            {
                Button button = control as Button;
                if (button != null && button.Text == text) return true;
            }
            return false;
        }
        private static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(target);
        }

        private static void Invoke(object target, string name, object value)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(target, new[] { value });
        }

        private static System.Collections.Generic.IEnumerable<Control> AllControls(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (Control descendant in AllControls(child)) yield return descendant;
            }
        }

        private static double Contrast(Color first, Color second)
        {
            double a = Luminance(first);
            double b = Luminance(second);
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        private static double Luminance(Color color)
        {
            return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        }

        private static double Channel(byte value)
        {
            double channel = value / 255.0;
            return channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }
    }
}
