using Atol.Drivers10.Fptr;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UiDesktopApp1.Helpers;
using UiDesktopApp1.Models;
using UiDesktopApp1.Services;
using static UiDesktopApp1.Views.Pages.SettingsPage;

namespace UiDesktopApp1.Views.Pages
{
    public partial class SalesPage : Page
    {
        private CancellationTokenSource cancellationTokenSource;
        private readonly IFptr fptr = KktService.Fptr; // ✅ экземпляр драйвера

        // ✅ Список результатов для dataGrid
        public ObservableCollection<SaleResultRow> SaleResults { get; set; } = new();

        public SalesPage()
        {
            InitializeComponent();
            DataContext = this; // ✅ для биндинга SaleResults
            dataGridSales.ItemsSource = AppData.ItemsForSale; // Подключаем данные
        }

        // ПРОДАЖА
        //private async void btnSell_Click(object sender, RoutedEventArgs e)
        //{
        //if (dataGridSales.Items.Count == 0)
        //{
        //MessageBox.Show("Нет данных для продажи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        //return;
        //}

        //SetControlsEnabled(false);

        //int batchSize;
        //if (!int.TryParse(txtBatchSizeL.Text, out batchSize) || batchSize <= 0)
        //{
        //batchSize = 1;
        //}

        //List<RequestedCisItem> itemsToProcess = dataGridSales.Items
        //.OfType<RequestedCisItem>()
        //.Where(item => !string.IsNullOrWhiteSpace(item.RequestedCis))
        //.ToList();

        //if (itemsToProcess.Count == 0)
        //{
        //MessageBox.Show("Не удалось обработать данные.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        //LogHelper.WriteLog("Обработка данных для продажи", "Ошибка: Не удалось обработать данные");
        //return;
        //}

        //Dispatcher.Invoke(() =>
        //{
        //progressBar.Visibility = Visibility.Visible;
        //progressText.Visibility = Visibility.Visible;
        //btnCancel.Visibility = Visibility.Visible;

        //progressBar.Value = 0;
        //progressBar.Maximum = (double)itemsToProcess.Count;
        //progressText.Text = $"0 / {itemsToProcess.Count}";

        //SaleResults.Clear();
        //});

        //cancellationTokenSource = new CancellationTokenSource();
        //CancellationToken token = cancellationTokenSource.Token;

        //await Task.Run(async () =>
        //{
        //try
        //{
        //Directory.CreateDirectory(Path.GetDirectoryName(LogHelper.logFilePath));

        //for (int i = 0; i < itemsToProcess.Count; i += batchSize)
        //{
        //if (token.IsCancellationRequested)
        //{
        //Dispatcher.Invoke(() =>
        //MessageBox.Show("Операция была отменена!", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information)
        //);
        //break;
        //}

        //var batch = itemsToProcess.Skip(i).Take(batchSize).ToList();

        //var jsonData = new
        //{
        //type = "sell",
        //validateMarkingCodes = true,
        //operatorInfo = new
        //{
        //name = "Голубец В. В.",
        //vatin = "771683739093"
        //},
        //electronically = true,
        //items = batch.Select(row => new
        //{
        //type = "position",
        //name = "ОЭС",
        //price = 0.01,
        //quantity = 1.0,
        //measurementUnit = "piece",
        //amount = 0.01,
        //infoDiscountAmount = 0.0,
        //tax = new
        //{
        //type = "none"
        //},
        //paymentObject = "commodityWithMarking",
        //paymentMethod = "fullPayment",
        //imcParams = new
        //{
        //imcType = "imcUnrecognized",
        //imc = row.Value,
        //itemEstimatedStatus = "itemPieceSold",
        //imcModeProcessing = 0
        //}
        //}).ToList(),
        //payments = new[] { new { sum = batch.Count * 0.01, type = "electronically" } },
        //taxes = new object[] { },
        //total = batch.Count * 0.01,
        //clientInfo = new
        //{
        //emailOrPhone = "morenvv@outlook.com"
        //}
        //};

        //string jsonString = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });
        //fptr.setParam(Constants.LIBFPTR_PARAM_JSON_DATA, jsonString);
        //int resultCode = fptr.processJson();

        //string resultJson = resultCode == 0
        //? fptr.getParamString(Constants.LIBFPTR_PARAM_JSON_DATA)
        //: $"Ошибка выполнения processJson: {fptr.errorDescription()}";

        //LogHelper.WriteLog($"Чек ПРОДАЖИ № {(i / batchSize) + 1}", $"Задание:\n{jsonString}\n\nОтвет ККТ:\n{resultJson}");

        //Dispatcher.Invoke(() =>
        //{
        //try
        //{
        //bool isSuccess = false;

        //if (resultCode == 0 && resultJson.TrimStart().StartsWith("{"))
        //{
        //using var doc = JsonDocument.Parse(resultJson);
        //int driverCode = doc.RootElement
        //.GetProperty("driverError")
        //.GetProperty("code")
        //.GetInt32();

        //if (driverCode == 0)
        //{
        //string fiscalDate = "[нет даты]";
        //string fiscalDocNumber = "[нет №]";

        //if (doc.RootElement.TryGetProperty("fiscalParams", out var fiscalParams))
        //{
        //if (fiscalParams.TryGetProperty("fiscalDocumentDateTime", out var dateElem))
        //fiscalDate = dateElem.GetString();
        //if (fiscalParams.TryGetProperty("fiscalDocumentNumber", out var numberElem))
        //fiscalDocNumber = numberElem.ToString();
        //}

        //SaleResults.Add(new SaleResultRow
        //{
        //FiscalDocumentDateTime = fiscalDate,
        //FiscalDocumentNumber = fiscalDocNumber,
        //ImcCount = batch.Count
        //});

        //foreach (var item in batch)
        //{
        //item.IsProcessed = true;
        //AppData.ItemsForSale.Remove(item);
        //}

        //isSuccess = true;
        //}
        //}

        //if (!isSuccess)
        //{
        //foreach (var item in batch)
        //item.HasError = true;
        //}

        //progressBar.Value += batch.Count;
        //progressText.Text = $"{progressBar.Value} / {itemsToProcess.Count}";
        //}
        //catch (Exception ex)
        //{
        //LogHelper.WriteLog("Ошибка обработки результата JSON", ex.ToString());
        //foreach (var item in batch)
        //item.HasError = true;
        //}
        //});

        //await Task.Delay(100, token);
        //}

        //Dispatcher.Invoke(() =>
        //{
        //MessageBox.Show($"Обработка завершена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        //});
        //}
        //catch (Exception ex)
        //{
        //Dispatcher.Invoke(() =>
        //{
        //MessageBox.Show($"Ошибка выполнения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        //LogHelper.WriteLog("Ошибка выполнения JSON-задания", ex.ToString());
        //});
        //}
        //finally
        //{
        //Dispatcher.Invoke(() =>
        //{
        //progressBar.Visibility = Visibility.Collapsed;
        //progressText.Visibility = Visibility.Collapsed;
        //btnCancel.Visibility = Visibility.Collapsed;
        //cancellationTokenSource?.Dispose();
        //});
        //SetControlsEnabled(true);
        //}
        //});
        //}

        // ПРОДАЖА ЛМ ЧЗ
        private async void btnSell_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridSales.Items.Count == 0)
            {
                MessageBox.Show("Нет данных для продажи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetControlsEnabled(false);

            int batchSize;
            if (!int.TryParse(txtBatchSizeL.Text, out batchSize) || batchSize <= 0)
            {
                batchSize = 1;
            }

            List<RequestedCisItem> itemsToProcess = dataGridSales.Items
                .OfType<RequestedCisItem>()
                .Where(item => !string.IsNullOrWhiteSpace(item.RequestedCis))
                .ToList();

            if (itemsToProcess.Count == 0)
            {
                MessageBox.Show("Не удалось обработать данные.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LogHelper.WriteLog("Обработка данных для продажи", "Ошибка: Не удалось обработать данные");
                return;
            }

            // 🔍 ПРОВЕРКА КМ ЧЕРЕЗ ЛМ ЧЗ
            var cisCodes = itemsToProcess.Select(x => x.RequestedCis?.Trim()).ToList();
            var checker = new LmCisChecker();
            var result = await checker.CheckCodesAsync(cisCodes);

            if (result == null)
            {
                MessageBox.Show("Ошибка при обращении к ЛМ ЧЗ.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                SetControlsEnabled(true);
                return;
            }

            if (result.codes.Any(c => !c.valid || c.isBlocked || c.sold))
            {
                MessageBox.Show("Обнаружены недействительные или заблокированные КМ!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                SetControlsEnabled(true);
                return;
            }

            Dispatcher.Invoke(() =>
            {
                progressBar.Visibility = Visibility.Visible;
                progressText.Visibility = Visibility.Visible;
                btnCancel.Visibility = Visibility.Visible;

                progressBar.Value = 0;
                progressBar.Maximum = (double)itemsToProcess.Count;
                progressText.Text = $"0 / {itemsToProcess.Count}";

                SaleResults.Clear();
            });

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            await Task.Run(async () =>
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogHelper.logFilePath));

                    for (int i = 0; i < itemsToProcess.Count; i += batchSize)
                    {
                        if (token.IsCancellationRequested)
                        {
                            Dispatcher.Invoke(() =>
                                MessageBox.Show("Операция была отменена!", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information)
                            );
                            break;
                        }

                        var batch = itemsToProcess.Skip(i).Take(batchSize).ToList();

                        var jsonData = new
                        {
                            type = "sell",
                            validateMarkingCodes = true,
                            operatorInfo = new
                            {
                                name = "Голубец В. В.",
                                vatin = "771683739093"
                            },
                            electronically = true,
                            items = batch.Select(row => new
                            {
                                type = "position",
                                name = "ОЭС",
                                price = 0.01,
                                quantity = 1.0,
                                measurementUnit = "piece",
                                amount = 0.01,
                                infoDiscountAmount = 0.0,
                                tax = new
                                {
                                    type = "none"
                                },
                                paymentObject = "commodityWithMarking",
                                paymentMethod = "fullPayment",
                                imcParams = new
                                {
                                    imcType = "imcUnrecognized",
                                    imc = row.Value,
                                    itemEstimatedStatus = "itemPieceSold",
                                    imcModeProcessing = 0
                                }
                            }).ToList(),
                            payments = new[] { new { sum = batch.Count * 0.01, type = "electronically" } },
                            taxes = new object[] { },
                            total = batch.Count * 0.01,
                            clientInfo = new
                            {
                                emailOrPhone = "morenvv@outlook.com"
                            }
                        };

                        string jsonString = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });
                        fptr.setParam(Constants.LIBFPTR_PARAM_JSON_DATA, jsonString);
                        int resultCode = fptr.processJson();

                        string resultJson = resultCode == 0
                            ? fptr.getParamString(Constants.LIBFPTR_PARAM_JSON_DATA)
                            : $"Ошибка выполнения processJson: {fptr.errorDescription()}";

                        LogHelper.WriteLog($"Чек ПРОДАЖИ № {(i / batchSize) + 1}", $"Задание:\n{jsonString}\n\nОтвет ККТ:\n{resultJson}");

                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                bool isSuccess = false;

                                if (resultCode == 0 && resultJson.TrimStart().StartsWith("{"))
                                {
                                    using var doc = JsonDocument.Parse(resultJson);
                                    int driverCode = doc.RootElement
                                        .GetProperty("driverError")
                                        .GetProperty("code")
                                        .GetInt32();

                                    if (driverCode == 0)
                                    {
                                        string fiscalDate = "[нет даты]";
                                        string fiscalDocNumber = "[нет №]";

                                        if (doc.RootElement.TryGetProperty("fiscalParams", out var fiscalParams))
                                        {
                                            if (fiscalParams.TryGetProperty("fiscalDocumentDateTime", out var dateElem))
                                                fiscalDate = dateElem.GetString();
                                            if (fiscalParams.TryGetProperty("fiscalDocumentNumber", out var numberElem))
                                                fiscalDocNumber = numberElem.ToString();
                                        }

                                        SaleResults.Add(new SaleResultRow
                                        {
                                            FiscalDocumentDateTime = fiscalDate,
                                            FiscalDocumentNumber = fiscalDocNumber,
                                            ImcCount = batch.Count
                                        });

                                        foreach (var item in batch)
                                        {
                                            item.IsProcessed = true;
                                            AppData.ItemsForSale.Remove(item);
                                        }

                                        isSuccess = true;
                                    }
                                }

                                if (!isSuccess)
                                {
                                    foreach (var item in batch)
                                        item.HasError = true;
                                }

                                progressBar.Value += batch.Count;
                                progressText.Text = $"{progressBar.Value} / {itemsToProcess.Count}";
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLog("Ошибка обработки результата JSON", ex.ToString());
                                foreach (var item in batch)
                                    item.HasError = true;
                            }
                        });

                        await Task.Delay(100, token);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Обработка завершена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Ошибка выполнения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        LogHelper.WriteLog("Ошибка выполнения JSON-задания", ex.ToString());
                    });
                }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        progressBar.Visibility = Visibility.Collapsed;
                        progressText.Visibility = Visibility.Collapsed;
                        btnCancel.Visibility = Visibility.Collapsed;
                        cancellationTokenSource?.Dispose();
                    });
                    SetControlsEnabled(true);
                }
            });
        }



        // Обработчик для кнопки "Отменить"
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }

        // Включение/выключение элементов управления
        private void SetControlsEnabled(bool enabled)
        {
            Dispatcher.Invoke(() =>
            {
                btnSell.IsEnabled = enabled;
                txtBatchSizeL.IsEnabled = enabled;
            });
        }

        // ✅ Класс результата чека
        public class SaleResultRow
        {
            public string FiscalDocumentDateTime
            {
                get; set;
            }
            public string FiscalDocumentNumber
            {
                get; set;
            }
            public int ImcCount
            {
                get; set;
            }
        }
    }
}
