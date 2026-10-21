using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace HoverDrop.App.Services
{
    public enum UpdateCheckResult { NotInstalled, UpToDate, Ready, Error }

    public sealed class UpdateService
    {
        public static UpdateService Instance { get; } = new();

        // Репозиторий, из релизов которого берутся обновления (поправь, если он называется иначе)
        private const string RepoUrl = "https://github.com/TimofeyDanilyuk/HoverDrop";

        private readonly UpdateManager _mgr = new(new GithubSource(RepoUrl, null, false));
        private readonly SemaphoreSlim _lock = new(1, 1);
        private UpdateInfo? _ready;

        // Обновление скачано и готово к установке
        public event Action<string>? UpdateReady;

        public string CurrentVersion => _mgr.IsInstalled ? _mgr.CurrentVersion?.ToString() ?? "?" : "dev";
        public string? ReadyVersion => _ready?.TargetFullRelease.Version.ToString();

        public async Task<UpdateCheckResult> CheckAsync()
        {
            // При запуске из VS / dotnet run приложение не установлено, обновлять нечего
            if (!_mgr.IsInstalled) return UpdateCheckResult.NotInstalled;

            await _lock.WaitAsync();
            try
            {
                if (_ready != null) return UpdateCheckResult.Ready;

                var update = await _mgr.CheckForUpdatesAsync();
                if (update == null) return UpdateCheckResult.UpToDate;

                await _mgr.DownloadUpdatesAsync(update);
                _ready = update;
                UpdateReady?.Invoke(ReadyVersion!);
                return UpdateCheckResult.Ready;
            }
            catch
            {
                return UpdateCheckResult.Error;
            }
            finally
            {
                _lock.Release();
            }
        }

        public void InstallAndRestart()
        {
            if (_ready == null) return;
            // Velopack дождётся завершения процесса, поставит обновление и запустит новую версию
            _mgr.WaitExitThenApplyUpdates(_ready.TargetFullRelease);
            Application.Current.Shutdown(); // OnExit уберёт значок из трея
        }
    }
}