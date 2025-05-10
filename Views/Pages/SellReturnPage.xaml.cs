using Atol.Drivers10.Fptr;
using System;
using System.Collections.Generic;
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
//using Newtonsoft.Json;
using static UiDesktopApp1.Views.Pages.SettingsPage;

namespace UiDesktopApp1.Views.Pages
{
    public partial class SellReturnPage : Page
    {
        private CancellationTokenSource cancellationTokenSource;
        private readonly IFptr fptr = KktService.Fptr; // ✅ экземпляр драйвера

        public SellReturnPage()
        {
            InitializeComponent();
            dataGridSellReturn.ItemsSource = AppData.ItemsForSellReturn; // Подключаем данные
        }

        // ПРОДАЖА
        private async void btnSellReturn_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridSellReturn.Items.Count == 0)
            {
                MessageBox.Show("Нет данных для продажи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetControlsEnabled(false);

            foreach (var item in dataGridSellReturn.Items)
            {
                LogHelper.WriteLog("Тип элемента в dataGridSellReturn", item?.GetType().FullName ?? "null");
            }

            //int batchSize = 1;  // Заглушка для размера пакета, можно заменить на привязку

            int batchSize;
            if (!int.TryParse(txtBatchSizeL.Text, out batchSize) || batchSize <= 0)
            {
                batchSize = 1;
            }

            List<RequestedCisItem> itemsToProcess = dataGridSellReturn.Items
                .OfType<RequestedCisItem>()
                .Where(item => !string.IsNullOrWhiteSpace(item.RequestedCis))
                .ToList();

            if (itemsToProcess.Count == 0)
            {
                MessageBox.Show("Не удалось обработать данные.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LogHelper.WriteLog("Обработка данных для продажи", "Ошибка: Не удалось обработать данные");
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
            });

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            await Task.Run(async () =>
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogHelper.logFilePath));
                    int processedItems = 0;

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
                            type = "sellReturn",
                            validateMarkingCodes = true,
                            operatorInfo = new { name = "Голубец В. В.", vatin = "771683739093" },
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
                                tax = new { type = "none" },
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
                            clientInfo = new { emailOrPhone = "morenvv@outlook.com"
                            }
                        };

                        //string jsonString = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
                        string jsonString = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });

                        fptr.setParam(Constants.LIBFPTR_PARAM_JSON_DATA, jsonString);
                        int resultCode = fptr.processJson();

                        string resultJson = resultCode == 0
                            ? fptr.getParamString(Constants.LIBFPTR_PARAM_JSON_DATA)
                            : $"Ошибка выполнения processJson: {fptr.errorDescription()}";

                        LogHelper.WriteLog($"Чек ВОЗВРАТА № {(i / batchSize) + 1}", $"Задание:\n{jsonString}\n\nОтвет ККТ:\n{resultJson}");

                        Dispatcher.Invoke(() =>
                        {
                            foreach (var row in batch)
                                row.IsProcessed = true;

                            progressBar.Value += batch.Count;
                            progressText.Text = $"{progressBar.Value} / {itemsToProcess.Count}";
                        });

                        await Task.Delay(100, token);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Успешно обработано {itemsToProcess.Count} записей.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Ошибка выполнения JSON-задания: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                btnSellReturn.IsEnabled = enabled;
                txtBatchSizeL.IsEnabled = enabled;
            });
        }
    }
}
