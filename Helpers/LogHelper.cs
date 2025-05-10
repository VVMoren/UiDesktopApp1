using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace UiDesktopApp1.Helpers
{
    public static class LogHelper
    {
        // Путь к лог-файлу (по умолчанию)
        public static string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Tools", "TokenLog.txt");

        // Метод для записи логов
        public static void WriteLog(string title, string content)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {title}:\n{content}\n\n";
                File.AppendAllText(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка записи в лог-файл: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для выбора пользовательского пути к лог-файлу
        public static void SelectLogFilePath()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Выберите путь для лог-файла",
                Filter = "Текстовые файлы (*.txt)|*.txt",
                FileName = "TokenLog.txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                logFilePath = saveFileDialog.FileName;

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось создать директорию лог-файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
