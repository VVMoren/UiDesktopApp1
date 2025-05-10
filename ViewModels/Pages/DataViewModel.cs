using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiDesktopApp1.Models;
using UiDesktopApp1.Services;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        private readonly MarkirovkaApiService _apiService;

        [ObservableProperty]
        private IEnumerable<DataColor> _colors;

        [ObservableProperty]
        private ObservableCollection<RequestedCisItem> _requestedCisList = new();

        private const string ApiUrl = "https://markirovka.crpt.ru/api/v3/true-api/cises/info";

        public DataViewModel(MarkirovkaApiService apiService)
        {
            _apiService = apiService;
        }

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            var random = new Random();
            var colorCollection = new List<DataColor>();

            for (int i = 0; i < 8192; i++)
                colorCollection.Add(
                    new DataColor
                    {
                        Color = new SolidColorBrush(
                            Color.FromArgb(
                                (byte)200,
                                (byte)random.Next(0, 250),
                                (byte)random.Next(0, 250),
                                (byte)random.Next(0, 250)
                            )
                        )
                    }
                );

            Colors = colorCollection;
            RequestedCisList = new ObservableCollection<RequestedCisItem>();
            _isInitialized = true;
        }

        [RelayCommand]
        public async Task LoadFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Файл не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            var cisList = lines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct()
                .ToList();

            RequestedCisList.Clear();
            foreach (var cis in cisList)
            {
                RequestedCisList.Add(new RequestedCisItem
                {
                    RequestedCis = cis,
                    ProductName = "N/D",
                    Status = "N/D"
                });
            }

            if (RequestedCisList.Count == 0)
            {
                MessageBox.Show("Файл пуст или не содержит корректных кодов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Автоматически получаем актуальный статус без запроса
            await FetchCisInfoBatchedAsync();
        }



        private void UpdateTable(List<ApiResponse> responseList)
        {
            foreach (var response in responseList)
            {
                var item = RequestedCisList.FirstOrDefault(c =>
                    c.RequestedCis?.StartsWith(response.CisInfo.RequestedCis) == true);

                if (item != null)
                {
                    item.ProductName = response.CisInfo.ProductName;
                    item.Status = GetStatusDescription(response.CisInfo.Status);
                }
            }

            // Обновляем UI
            OnPropertyChanged(nameof(RequestedCisList));
        }


        public async Task FetchCisInfoBatchedAsync()
        {
            string token = _apiService.ReadTokenFromFile();
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Введите токен!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            int batchSize = 1000;
            int totalBatches = (int)Math.Ceiling((double)RequestedCisList.Count / batchSize);

            var responseList = new List<ApiResponse>();

            for (int i = 0; i < totalBatches; i++)
            {
                var batch = RequestedCisList
                    .Skip(i * batchSize)
                    .Take(batchSize)
                    .Select(item => item.RequestedCis?.Trim() ?? "")
                    .Select(cis => cis.Length >= 25 ? cis.Substring(0, 25) : cis)
                    .Where(cis => !string.IsNullOrEmpty(cis) && cis.Length == 25)
                    .Select(cis => $"\"{cis.Replace("\"", "\\\"")}\"")
                    .ToList();

                if (batch.Count == 0)
                {
                    MessageBox.Show("Некорректные данные в файле!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string requestBody = "[\n    " + string.Join(",\n    ", batch) + "\n]";
                try
                {
                    HttpContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(ApiUrl, content);
                    string responseData = await response.Content.ReadAsStringAsync();

                    LogToFile("=== API REQUEST ===");
                    LogToFile(requestBody);
                    LogToFile("=== API RESPONSE ===");
                    LogToFile(responseData);
                    LogToFile("====================");

                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var responseJson = JsonConvert.DeserializeObject<List<ApiResponse>>(responseData);

                            if (responseJson != null)
                            {
                                responseList.AddRange(responseJson); // 🔹 Добавляем в общий список
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка парсинга JSON: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            LogToFile("=== PARSE ERROR ===");
                            LogToFile(ex.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка запроса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    LogToFile("=== EXCEPTION ===");
                    LogToFile(ex.ToString());
                }
            }
            UpdateTable(responseList);
        }


        private string GetStatusDescription(string status)
        {
            return status switch
            {
                "EMITTED" => "Эмитирован",
                "APPLIED" => "Нанесён",
                "INTRODUCED" => "В обороте",
                "WRITTEN_OFF" => "Списан",
                "WITHDRAWN" => "Выбыл",
                _ => "Неизвестно"
            };
        }

        private void LogToFile(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            }
            catch
            {
                // Игнорируем ошибки логирования, чтобы не прерывать выполнение основного кода
            }
        }

        private readonly string _logPath = @"L:\source\repos\UiDesktopApp1\Log.test.txt";

        public bool HasSelectedAppliedItems => 
            RequestedCisList.Any(item => item.IsSelected && item.Status == "Нанесён");

        public class ApiResponse
        {
            public CisInfo CisInfo { get; set; }
        }

        public class CisInfoResponse
        {
            public string RequestedCis { get; set; }
            public string ProductName { get; set; }
            public string Status { get; set; }
        }
    }


}
