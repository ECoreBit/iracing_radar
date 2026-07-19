using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Forms;

namespace IRacingRadarUpdater
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args == null || args.Length < 7) return 2;
            string package = args[0];
            string targetRoot = args[1];
            int processId;
            if (!int.TryParse(args[2], out processId)) return 2;
            string configuratorPath = args[3];
            bool restartSimHub = args[4] == "1";
            string simHubPath = args[5];
            string tag = args[6];
            bool success = false;
            string error = null;

            try
            {
                WaitForProcess(processId);
                StopSimHub();
                Installer.InstallPackage(package, targetRoot);
                success = true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            if (restartSimHub && File.Exists(simHubPath)) Start(simHubPath);
            if (File.Exists(configuratorPath)) Start(configuratorPath);

            MessageBox.Show(success
                    ? "iRacing Radar " + tag + " 更新完成。\n\nUpdate completed successfully."
                    : "更新失败，旧文件已尽可能恢复。\n\nUpdate failed and the previous files were restored where possible.\n\n" + error,
                success ? "iRacing Radar" : "iRacing Radar Update",
                MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            return success ? 0 : 1;
        }

        private static void WaitForProcess(int processId)
        {
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    if (!process.WaitForExit(30000)) throw new TimeoutException("The configurator did not close in time.");
                }
            }
            catch (ArgumentException) { }
        }

        private static void StopSimHub()
        {
            foreach (Process process in Process.GetProcessesByName("SimHubWPF"))
            {
                try
                {
                    if (process.HasExited) continue;
                    bool closed = process.CloseMainWindow() && process.WaitForExit(7000);
                    if (!closed && !process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                    }
                }
                catch (InvalidOperationException) { }
                finally { process.Dispose(); }
            }
            Thread.Sleep(250);
        }

        private static void Start(string executable)
        {
            try
            {
                Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    WorkingDirectory = Path.GetDirectoryName(executable),
                    UseShellExecute = true
                });
                if (process != null) process.Dispose();
            }
            catch { }
        }
    }

    internal static class Installer
    {
        internal static void InstallPackage(string package, string targetRoot)
        {
            targetRoot = Path.GetFullPath(targetRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string work = Path.Combine(Path.GetTempPath(), "iRacingRadarInstall-" + Guid.NewGuid().ToString("N"));
            string stage = Path.Combine(work, "stage");
            string backup = Path.Combine(work, "backup");
            Directory.CreateDirectory(stage);
            Directory.CreateDirectory(backup);
            ExtractSafely(package, stage);

            List<RollbackEntry> rollback = new List<RollbackEntry>();
            try
            {
                foreach (string source in Directory.GetFiles(stage, "*", SearchOption.AllDirectories))
                {
                    string relative = source.Substring(stage.TrimEnd(Path.DirectorySeparatorChar).Length + 1);
                    if (relative.Equals("IRacingRadar.settings.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    string target = Path.GetFullPath(Path.Combine(targetRoot, relative));
                    if (!target.StartsWith(targetRoot, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Unsafe update path: " + relative);

                    string backupFile = null;
                    if (File.Exists(target))
                    {
                        backupFile = Path.Combine(backup, relative);
                        Directory.CreateDirectory(Path.GetDirectoryName(backupFile));
                        File.Copy(target, backupFile, true);
                    }
                    rollback.Add(new RollbackEntry { Target = target, Backup = backupFile });
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(source, target, true);
                }
            }
            catch
            {
                for (int i = rollback.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        if (rollback[i].Backup == null) File.Delete(rollback[i].Target);
                        else File.Copy(rollback[i].Backup, rollback[i].Target, true);
                    }
                    catch { }
                }
                throw;
            }
            finally
            {
                try { Directory.Delete(work, true); } catch { }
            }
        }

        internal static void ExtractSafely(string package, string stage)
        {
            string stageRoot = Path.GetFullPath(stage).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            using (ZipArchive archive = ZipFile.OpenRead(package))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    string relative = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    string destination = Path.GetFullPath(Path.Combine(stageRoot, relative));
                    if (!destination.StartsWith(stageRoot, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Unsafe ZIP entry: " + entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    using (Stream input = entry.Open())
                    using (FileStream output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                        input.CopyTo(output);
                }
            }
        }

        private sealed class RollbackEntry
        {
            public string Target { get; set; }
            public string Backup { get; set; }
        }
    }
}
