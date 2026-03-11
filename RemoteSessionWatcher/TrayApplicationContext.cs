using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RemoteSessionWatcher
{
    public sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly Timer _timer;
        private readonly ToolStripMenuItem _startupMenuItem;

        private AppConfig _config;
        private MarkerController _markerController;

        private bool _isRemoteActive;
        private int _activeHits;
        private int _inactiveHits;
        private string _lastMatchedRulesText = "нет";

        public TrayApplicationContext()
        {
            try
            {
                _config = ConfigManager.LoadOrCreateDefault();
            }
            catch (Exception ex)
            {
                Logger.Log("Ошибка загрузки конфига: " + ex);
                _config = AppConfig.CreateDefault();
                ConfigManager.Save(_config);
            }

            _markerController = new MarkerController(_config.MarkerExePath);

            _startupMenuItem = new ToolStripMenuItem();
            UpdateStartupMenuText();

            var openConfigFolderItem = new ToolStripMenuItem("Открыть папку конфига", null, OpenConfigFolder_Click);
            var openLogItem = new ToolStripMenuItem("Открыть лог", null, OpenLog_Click);
            var reloadConfigItem = new ToolStripMenuItem("Перезагрузить конфиг", null, ReloadConfig_Click);
            var runScheduledTaskItem = new ToolStripMenuItem("Тест: запустить задачу планировщика", null, RunScheduledTask_Click);            
            var helpItem = new ToolStripMenuItem("Как настроить", null, OpenHelp_Click);
            var exitItem = new ToolStripMenuItem("Выход", null, Exit_Click);

            _startupMenuItem.Click += StartupMenuItem_Click;

            var menu = new ContextMenuStrip();
            menu.Items.Add(openConfigFolderItem);
            menu.Items.Add(openLogItem);
            menu.Items.Add(helpItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(reloadConfigItem);
            menu.Items.Add(_startupMenuItem);
            menu.Items.Add(runScheduledTaskItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            _notifyIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Visible = true,
                Text = "Remote Session Watcher",
                ContextMenuStrip = menu
            };

            _notifyIcon.DoubleClick += OpenConfigFolder_Click;

            _timer = new Timer();
            _timer.Interval = Math.Max(500, _config.CheckIntervalMs);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            Logger.Log("Приложение запущено. Admin=" + AdminHelper.IsRunAsAdministrator());
            Logger.Log("Конфиг: " + ConfigManager.ConfigPath);

            UpdateTrayText();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                CheckRemoteState();
            }
            catch (Exception ex)
            {
                Logger.Log("Ошибка в цикле проверки: " + ex);
            }
        }

        private Icon LoadTrayIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WallpaperEngineStop.ico");

                if (File.Exists(iconPath))
                    return new Icon(iconPath);
            }
            catch (Exception ex)
            {
                Logger.Log("Не удалось загрузить иконку трея: " + ex);
            }

            return SystemIcons.Application;
        }

        private void RunScheduledTask_Click(object sender, EventArgs e)
        {
            try
            {
                StartupManager.RunNow();
                Logger.Log("Выполнен ручной запуск задачи планировщика.");
                _notifyIcon.ShowBalloonTip(2000, "Remote Session Watcher", "Задача планировщика запущена", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                Logger.Log("Ошибка ручного запуска задачи планировщика: " + ex);
                _notifyIcon.ShowBalloonTip(3000, "Remote Session Watcher", "Не удалось запустить задачу планировщика", ToolTipIcon.Error);
            }
        }

        private void CheckRemoteState()
        {
            var enabledRules = (_config.Rules ?? new List<WatchRule>())
                .Where(r => r != null && r.Enabled && !string.IsNullOrWhiteSpace(r.ProcessName))
                .ToList();

            if (enabledRules.Count == 0)
            {
                HandleRawActiveState(false, new List<WatchRule>());
                return;
            }

            var processNames = enabledRules
                .Select(r => r.ProcessName) 
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var snapshots = ProcessScanner.GetProcesses(processNames);
            
            var matchedRules = new List<WatchRule>();

            foreach (var rule in enabledRules)
            {
                if (RuleMatches(rule, snapshots))
                    matchedRules.Add(rule);
            }

            bool rawActive = matchedRules.Count > 0;
            HandleRawActiveState(rawActive, matchedRules);
        }

        private void OpenHelp_Click(object sender, EventArgs e)
        {
            try
            {
                string pdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", "HowToSetup.pdf");

                if (!File.Exists(pdfPath))
                {
                    Logger.Log("Файл инструкции не найден: " + pdfPath);
                    _notifyIcon.ShowBalloonTip(3000, "Remote Session Watcher", "Файл инструкции не найден", ToolTipIcon.Warning);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Log("Ошибка открытия инструкции: " + ex);
                _notifyIcon.ShowBalloonTip(3000, "Remote Session Watcher", "Не удалось открыть инструкцию", ToolTipIcon.Error);
            }
        } 

        private bool RuleMatches(WatchRule rule, List<ProcessSnapshot> snapshots)
        {
            var candidates = snapshots
                .Where(p => string.Equals(p.Name, rule.ProcessName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var process in candidates)
            {
                string commandLine = process.CommandLine ?? string.Empty;

                bool containsOk = true;
                if (rule.CommandLineContains != null && rule.CommandLineContains.Count > 0)
                {
                    foreach (string token in rule.CommandLineContains)
                    {
                        if (string.IsNullOrWhiteSpace(token))
                            continue;

                        if (commandLine.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            containsOk = false;
                            break;
                        }
                    }
                }

                if (!containsOk)
                    continue;

                bool notContainsOk = true;
                if (rule.CommandLineNotContains != null && rule.CommandLineNotContains.Count > 0)
                {
                    foreach (string token in rule.CommandLineNotContains)
                    {
                        if (string.IsNullOrWhiteSpace(token))
                            continue;

                        if (commandLine.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            notContainsOk = false;
                            break;
                        }
                    }
                }

                if (!notContainsOk)
                    continue;

                
                return true;
            }

            return false;
        }

        private void HandleRawActiveState(bool rawActive, List<WatchRule> matchedRules)
        {
            if (rawActive)
            {
                _activeHits++;
                _inactiveHits = 0;
            }
            else
            {
                _inactiveHits++;
                _activeHits = 0;
            }

            int requiredActivate = Math.Max(1, _config.RequiredHitsToActivate);
            int requiredDeactivate = Math.Max(1, _config.RequiredHitsToDeactivate);

            if (!_isRemoteActive && rawActive && _activeHits >= requiredActivate)
            {
                _isRemoteActive = true;
                _lastMatchedRulesText = matchedRules.Count > 0
                    ? string.Join(", ", matchedRules.Select(r => r.Name))
                    : "неизвестно";

                _markerController.EnsureStarted();
                Logger.Log("Удалённая сессия активна. Marker запущен. Правила: " + _lastMatchedRulesText);
                UpdateTrayText();
                _notifyIcon.ShowBalloonTip(
                    3000,
                    "Remote Session Watcher",
                    "Обнаружена удалённая сессия: " + _lastMatchedRulesText,
                    ToolTipIcon.Info);
            }
            else if (_isRemoteActive && !rawActive && _inactiveHits >= requiredDeactivate)
            {
                _isRemoteActive = false;
                _lastMatchedRulesText = "нет";

                _markerController.EnsureStopped();
                Logger.Log("Удалённая сессия завершена. Marker остановлен.");
                UpdateTrayText();
                _notifyIcon.ShowBalloonTip(
                    3000,
                    "Remote Session Watcher",
                    "Удалённая сессия завершена",
                    ToolTipIcon.Info);
            }
            else
            {
                UpdateTrayText();
            }
        }

        private void UpdateTrayText()
        {
            string state = _isRemoteActive ? "Активна удалёнка" : "Удалёнки нет";
            string text = "Remote Session Watcher - " + state;

            if (text.Length > 63)
                text = text.Substring(0, 63);

            _notifyIcon.Text = text;
        }

        private void OpenConfigFolder_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", "/select,\"" + ConfigManager.ConfigPath + "\"");
            }
            catch (Exception ex)
            {
                Logger.Log("Не удалось открыть config.json: " + ex);
            }
        }

        private void OpenLog_Click(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(ConfigManager.LogPath))
                {
                    File.WriteAllText(ConfigManager.LogPath, "");
                }

                Process.Start("notepad.exe", ConfigManager.LogPath);
            }
            catch (Exception ex)
            {
                Logger.Log("Не удалось открыть лог: " + ex);
            }
        }

        private void ReloadConfig_Click(object sender, EventArgs e)
        {
            try
            {
                _config = ConfigManager.LoadOrCreateDefault();
                _markerController = new MarkerController(_config.MarkerExePath);
                _timer.Interval = Math.Max(500, _config.CheckIntervalMs);

                Logger.Log("Конфиг перезагружен.");
                _notifyIcon.ShowBalloonTip(2000, "Remote Session Watcher", "Конфиг перезагружен", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                Logger.Log("Ошибка перезагрузки конфига: " + ex);
                _notifyIcon.ShowBalloonTip(3000, "Remote Session Watcher", "Ошибка перезагрузки конфига", ToolTipIcon.Error);
            }
        }

        private void StartupMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                bool currentlyEnabled = StartupManager.IsEnabled();
                StartupManager.SetEnabled(!currentlyEnabled);
                UpdateStartupMenuText();

                string message = currentlyEnabled
                    ? "Автозапуск через планировщик выключен"
                    : "Автозапуск через планировщик включён";

                Logger.Log(message);
                _notifyIcon.ShowBalloonTip(2000, "Remote Session Watcher", message, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                Logger.Log("Ошибка изменения автозапуска: " + ex);
                _notifyIcon.ShowBalloonTip(3000, "Remote Session Watcher", "Ошибка изменения автозапуска", ToolTipIcon.Error);
            }
        }

        private void UpdateStartupMenuText()
        {
            bool enabled = StartupManager.IsEnabled();
            _startupMenuItem.Text = enabled
                ? "Автозапуск через планировщик: выключить"
                : "Автозапуск через планировщик: включить";
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            ExitThread();
        }

        protected override void ExitThreadCore()
        {
            try
            {
                _timer.Stop();
                _timer.Dispose();
            }
            catch
            {
            }

            try
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            catch
            {
            }

            try
            {
                _markerController.EnsureStopped();
            }
            catch
            {
            }

            Logger.Log("Приложение завершено.");
            base.ExitThreadCore();
        }
    }
}