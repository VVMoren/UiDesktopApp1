
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using UiDesktopApp1.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel
        {
            get;
        }

        public ObservableCollection<BlockLogModel> LogData { get; set; } = new();

        // ✅ Для фильтрации
        private List<BlockLogModel> AllLogData = new();
        public string FilterText { get; set; } = "";

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        private async void BtnGridLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePath = @"E:\загрузки\нсп_3\НСП-reboot\Телюков\КОРНИЛОВ\GridLog.txt";

                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"Файл не найден:\n{filePath}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var rawText = await File.ReadAllTextAsync(filePath, Encoding.GetEncoding("windows-1251"));
                var lines = rawText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                var groupedBlocks = new Dictionary<string, BlockLogModel>(); // orderId -> BlockLogModel
                string currentOrderId = null;

                foreach (var line in lines)
                {
                    // 1. Сохраняем последний orderId из строки запроса
                    var requestMatch = Regex.Match(line, @"orderId=([a-fA-F0-9\-]{36})");
                    if (requestMatch.Success)
                    {
                        currentOrderId = requestMatch.Groups[1].Value;
                    }

                    // 2. Обрабатываем строку ответа с codes
                    var responseMatch = Regex.Match(line,
                    @"(?<date>\d{2}\.\d{2}\.\d{4}\s+\d{2}:\d{2}:\d{2})\s+<--\s+(?<json>\{[^}]*""codes""\s*:\s*\[[^\]]*\][^}]*\})");

                    if (responseMatch.Success && !string.IsNullOrEmpty(currentOrderId))
                    {
                        try
                        {
                            var date = responseMatch.Groups["date"].Value.Trim();
                            var json = responseMatch.Groups["json"].Value;

                            var cleanedJson = Regex.Replace(json, @"([{\[,])\s*(\w+)\s*:", "$1\"$2\":");

                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };

                            var partialBlock = JsonSerializer.Deserialize<BlockLogModel>(cleanedJson, options);
                            if (partialBlock?.Codes != null && partialBlock.Codes.Any())
                            {
                                partialBlock.Timestamp = date;
                                partialBlock.OrderId = currentOrderId;
                                partialBlock.Codes = partialBlock.Codes
                                .Select(c => c.Replace(@"\u001D", ""))
                                .Select(c => c.Replace("\u001D", ""))
                                .ToList();

                                // добавляем или объединяем по orderId
                                if (!groupedBlocks.ContainsKey(currentOrderId))
                                {
                                    groupedBlocks[currentOrderId] = new BlockLogModel
                                    {
                                        OrderId = currentOrderId,
                                        OmsId = partialBlock.OmsId,
                                        Timestamp = date,
                                        Codes = new List<string>()
                                    };
                                }

                                groupedBlocks[currentOrderId].Codes.AddRange(partialBlock.Codes);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ошибка при разборе JSON: " + ex.Message);
                        }
                    }
                }

                // Обновление UI
                var result = groupedBlocks.Values
                .OrderByDescending(b => ParseDateTimeSafe(b.Timestamp))
                .ToList();

                AllLogData = result;

                LogData.Clear();
                foreach (var b in result)
                    LogData.Add(b);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке GridLog:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





        private DateTime ParseDateTimeSafe(string timestamp)
        {
            return DateTime.TryParse(timestamp, out var dt) ? dt : DateTime.MinValue;
        }

        // ✅ Обработчик инкрементного поиска
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterText = (sender as TextBox)?.Text?.Trim() ?? "";

            var filtered = string.IsNullOrWhiteSpace(FilterText)
            ? AllLogData
            : AllLogData.Where(b =>
            b.Codes.Any(c => c.Contains(FilterText, StringComparison.OrdinalIgnoreCase))).ToList();

            LogData.Clear();
            foreach (var b in filtered.OrderByDescending(b => ParseDateTimeSafe(b.Timestamp)))
                LogData.Add(b);
        }
    }

    public class BlockLogModel
    {
        public string OrderId
        {
            get; set;
        }
        public string OmsId
        {
            get; set;
        }
        public string Timestamp
        {
            get; set;
        }
        public List<string> Codes { get; set; } = new();

        public int CodeCount => Codes?.Count ?? 0;

        public bool IsChecked
        {
            get; set;
        } // ← если используется в Expander.CheckBox
    }


}
