using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UiDesktopApp1.Helpers;
using UiDesktopApp1.Services;

namespace UiDesktopApp1.Views.Pages
{
    public partial class UPDPage : Page
    {
        private readonly MarkirovkaApiService _apiService = new();

        public ObservableCollection<ReceiptItem> ReceiptsList { get; set; } = new();
        public ObservableCollection<ProductItem> ProductsList { get; set; } = new();

        public UPDPage()
        {
            InitializeComponent();
            ReceiptsDataGrid.ItemsSource = ReceiptsList;
            ProductsDataGrid.ItemsSource = ProductsList;
        }

        private async void SearchReceipts_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button searchButton)) return;

            try
            {
                SetUiState(false, searchButton);
                var (pg, limit) = GetSearchParameters();
                var receipts = await _apiService.SearchReceiptsV4Async(pg, limit);

                ProcessSearchResults(receipts);
            }
            catch (HttpRequestException httpEx)
            {
                HandleError("Ошибка подключения к серверу", httpEx.Message, "Ошибка сети", httpEx);
            }
            catch (Newtonsoft.Json.JsonException jsonEx)
            {
                HandleError("Ошибка обработки данных", "Неверный формат данных от сервера", "Ошибка данных", jsonEx);
            }
            catch (Exception ex)
            {
                HandleError("Непредвиденная ошибка", ex.Message, "Ошибка", ex);
            }
            finally
            {
                SetUiState(true, searchButton);
            }
        }

        private (string pg, int limit) GetSearchParameters()
        {
            return (
                pg: "ncp",
                limit: 1000
            );
        }


        private void ProcessSearchResults(JArray receipts)
        {
            ReceiptsList.Clear();

            if (receipts == null || !receipts.Any())
            {
                ShowInfoMessage("Чеки не найдены", "Информация");
                return;
            }

            foreach (var receipt in receipts)
            {
                try
                {
                    var did = receipt["did"]?.ToString();
                    if (string.IsNullOrEmpty(did))
                    {
                        LogHelper.WriteLog("Пропуск чека", "Отсутствует did");
                        continue;
                    }

                    // Используем receivedAt вместо docDate
                    var receivedAt = receipt["receivedAt"]?.ToString();
                    if (string.IsNullOrEmpty(receivedAt))
                    {
                        LogHelper.WriteLog("Ошибка даты", "Отсутствует receivedAt");
                        continue;
                    }

                    ReceiptsList.Add(new ReceiptItem
                    {
                        Did = did,
                        ReceivedAt = receivedAt,
                        DocumentType = GetDocumentTypeDisplayName(receipt["type"]?.ToString()),
                        Status = GetStatusDisplayName(receipt["status"]?.ToString())
                    });


                }
                catch (Exception ex)
                {
                    LogHelper.WriteLog("Ошибка обработки чека", ex.ToString());
                }
            }

            if (!ReceiptsList.Any())
            {
                ShowInfoMessage("Нет чеков с корректными данными", "Информация");
            }
        }

        private void SetUiState(bool isEnabled, Button button)
        {
            Mouse.OverrideCursor = isEnabled ? null : Cursors.Wait;
            button.IsEnabled = isEnabled;
        }

        private void HandleError(string logMessage, string userMessage, string title, Exception ex)
        {
            LogHelper.WriteLog(logMessage, ex.ToString());
            MessageBox.Show(userMessage, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowInfoMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetDocumentTypeDisplayName(string apiType) => apiType switch
        {
            "RECEIPT" => "Чек",
            "RECEIPT_RETURN" => "Чек возврата",
            _ => apiType
        };

        private string GetStatusDisplayName(string apiStatus) => apiStatus switch
        {
            "CHECKED_OK" => "Обработан",
            "CHECKED_NOT_OK" => "Ошибка обработки",
            "IN_PROGRESS" => "В обработке",
            _ => apiStatus
        };

        private async void ReceiptsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReceiptsDataGrid.SelectedItem is not ReceiptItem selectedReceipt)
                return;

            try
            {
                var receiptInfoArray = await _apiService.GetReceiptInfoV4Async(selectedReceipt.Did, "ncp", true, true);
                if (receiptInfoArray.Count > 0)
                {
                    var receiptInfo = receiptInfoArray[0] as JObject;
                    LoadReceiptProducts(receiptInfo);
                }
                else
                {
                    MessageBox.Show("Чек не содержит данных.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке чека: {ex.Message}");
            }
        }

        private async void LoadReceiptProducts(JObject receiptInfo)
        {
            ProductsList.Clear();

            var body = receiptInfo["body"]?["receipt"];
            if (body == null) return;

            var items = body["items"] as JArray;
            if (items == null) return;

            // Получаем список КИ (productId) и очищаем их
            var cisList = items
                .Select(i => i["productId"]?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(CleanCis)
                .Distinct()
                .ToList();

            LogHelper.WriteLog("Коды productId для запроса /cises/info", string.Join("\n", cisList));

            var cisInfoList = await _apiService.GetCisesInfoAsync(cisList);

            foreach (var item in items)
            {
                var rawProductCode = item["productId"]?.ToString();
                var cleanProductCode = CleanCis(rawProductCode); // ← очищаем

                var info = cisInfoList.FirstOrDefault(c => CleanCis(c.CisWithoutBrackets) == cleanProductCode);


                ProductsList.Add(new ProductItem
                {
                    codeString = item["codeString"]?.ToString(),
                    ProductName = info?.ProductName ?? "—",
                    Quantity = decimal.Parse(item["quantity"]?.ToString() ?? "0")
                });
            }

        }




        // Метод очистки спецсимволов из КИ
        private string CleanCis(string cis) =>
            cis?.Replace("\u001D", "").Replace("\r", "").Replace("\n", "").Trim();


        public class ReceiptItem
        {
            public string Did
            {
                get; set;
            }
            public string DidShort => Did?.Replace("9960440300120936482", "") ?? "";
            public string ReceivedAt
            {
                get; set;
            }   // Заменили Date на ReceivedAt
            public string DocumentType
            {
                get; set;
            }
            public string Status
            {
                get; set;
            }
        }

        public class ProductItem
        {
            public string fiscalDocumentNumber
            {
                get; set;
            }
            public string codeString
            {
                get; set;
            }
            public string unitCount
            {
                get; set;
            }
            public string ProductName
            {
                get; set;
            }
            public decimal Quantity
            {
                get; set;
            }
            public decimal Price
            {
                get; set;
            }
            public decimal Sum
            {
                get; set;
            }
            public string NdsRate
            {
                get; set;
            }
        }



    }
}
