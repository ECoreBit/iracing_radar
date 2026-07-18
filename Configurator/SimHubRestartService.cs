using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace IRacingRadarConfigurator
{
    internal static class SimHubRestartService
    {
        public static bool IsRunning()
        {
            return IsProcessRunning("SimHubWPF");
        }

        internal static bool IsProcessRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            try { return processes.Length > 0; }
            finally
            {
                foreach (Process process in processes) process.Dispose();
            }
        }
        public static string FindExecutable(string settingsPath)
        {
            foreach (Process process in Process.GetProcessesByName("SimHubWPF"))
            {
                try
                {
                    string runningPath = process.MainModule == null ? null : process.MainModule.FileName;
                    if (!string.IsNullOrEmpty(runningPath) && File.Exists(runningPath)) return runningPath;
                }
                catch { }
                finally { process.Dispose(); }
            }

            List<string> candidates = new List<string>();
            AddCandidate(candidates, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimHubWPF.exe"));
            if (!string.IsNullOrEmpty(settingsPath))
            {
                string settingsDirectory = Path.GetDirectoryName(Path.GetFullPath(settingsPath));
                if (!string.IsNullOrEmpty(settingsDirectory))
                    AddCandidate(candidates, Path.Combine(settingsDirectory, "SimHubWPF.exe"));
            }
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(programFilesX86))
                AddCandidate(candidates, Path.Combine(programFilesX86, "SimHub", "SimHubWPF.exe"));
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(programFiles))
                AddCandidate(candidates, Path.Combine(programFiles, "SimHub", "SimHubWPF.exe"));

            foreach (string candidate in candidates)
                if (File.Exists(candidate)) return candidate;
            throw new FileNotFoundException("SimHubWPF.exe was not found.");
        }

        public static string Restart(string settingsPath)
        {
            string executable = FindExecutable(settingsPath);
            foreach (Process process in Process.GetProcessesByName("SimHubWPF"))
            {
                try
                {
                    if (process.HasExited) continue;
                    bool closeRequested = process.CloseMainWindow();
                    bool exited = closeRequested && process.WaitForExit(7000);
                    if (!exited && !process.HasExited)
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit(3000);
                        }
                        catch (InvalidOperationException) { }
                    }
                }
                finally { process.Dispose(); }
            }

            Thread.Sleep(350);
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = Path.GetDirectoryName(executable),
                UseShellExecute = true
            };
            Process launched = Process.Start(start);
            if (launched == null) throw new InvalidOperationException("SimHub could not be started.");
            launched.Dispose();
            return executable;
        }

        private static void AddCandidate(ICollection<string> candidates, string path)
        {
            if (!string.IsNullOrEmpty(path) && !candidates.Contains(path)) candidates.Add(path);
        }
    }
}