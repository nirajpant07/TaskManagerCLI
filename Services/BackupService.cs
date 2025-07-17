using TaskManagerCLI.Repositories;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TaskManagerCLI.Services
{
    public class BackupService
    {
        private readonly ITaskRepository _repository;

        public BackupService(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateDailyBackupAsync()
        {
            try
            {
                await _repository.CreateBackupAsync();
                await CleanupOldBackupsAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't throw - backup failure shouldn't crash the app
                Console.WriteLine($"Backup failed: {ex.Message}");
            }
        }

        public async Task CleanupOldBackupsAsync(int retentionDays = 30)
        {
            try
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var archivePath = Path.Combine(documentsPath, "TaskManager", "Archive");

                if (!Directory.Exists(archivePath))
                    return;

                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                foreach (var directory in Directory.GetDirectories(archivePath))
                {
                    var dirInfo = new DirectoryInfo(directory);
                    if (dirInfo.CreationTime < cutoffDate)
                    {
                        Directory.Delete(directory, true);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but continue - cleanup failure is not critical
                Console.WriteLine($"Backup cleanup failed: {ex.Message}");
            }
        }

        public async Task<bool> RestoreFromBackupAsync(DateTime backupDate)
        {
            try
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var backupFolder = Path.Combine(documentsPath, "TaskManager", "Archive", backupDate.ToString("yyyy-MM-dd"));

                if (!Directory.Exists(backupFolder))
                    return false;

                var backupFiles = Directory.GetFiles(backupFolder, "tasks_backup_*.xlsx");
                if (backupFiles.Length == 0)
                    return false;

                // Get the most recent backup from that day
                var latestBackup = backupFiles[^1];
                var currentFilePath = Path.Combine(documentsPath, "TaskManager", "tasks.xlsx");

                // Create a backup of current file before restore
                var restoreBackupPath = Path.Combine(documentsPath, "TaskManager",
                    $"tasks_before_restore_{DateTime.UtcNow:yyyy-MM-dd_HHmmss}.xlsx");

                if (File.Exists(currentFilePath))
                {
                    File.Copy(currentFilePath, restoreBackupPath);
                }

                // Restore the backup
                File.Copy(latestBackup, currentFilePath, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Restore failed: {ex.Message}");
                return false;
            }
        }

        public string[] GetAvailableBackupDates()
        {
            try
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var archivePath = Path.Combine(documentsPath, "TaskManager", "Archive");

                if (!Directory.Exists(archivePath))
                    return Array.Empty<string>();

                var directories = Directory.GetDirectories(archivePath);
                var dates = new List<string>();

                foreach (var dir in directories)
                {
                    var dirName = Path.GetFileName(dir);
                    if (DateTime.TryParse(dirName, out _))
                    {
                        dates.Add(dirName);
                    }
                }

                dates.Sort();
                return dates.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}