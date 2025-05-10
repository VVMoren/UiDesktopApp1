using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Atol.Drivers10.Fptr;
using KKTServiceLib.Atol;
using KKTServiceLib.Atol.Types.Operations;
using KKTServiceLib.Atol.Types.Operations.Fiscal.Shift;
using KKTServiceLib.Atol.Types.Operations.Fiscal.Shift.CloseShift;
using KKTServiceLib.Atol.Types.Operations.Fiscal.Shift.OpenShift;
using KKTServiceLib.Atol.Types.Operations.KKT.Settings.ReadKKTSettings;
using Microsoft.Win32;
using Newtonsoft.Json;
using UiDesktopApp1.Helpers;
using UiDesktopApp1.Services;
using UiDesktopApp1.ViewModels.Pages;
using UiDesktopApp1.Views.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.Views.Pages
{
    public partial class SettingsPage : Page
    {
        private string _saveToken = string.Empty;
        private const string TokenFilePath = @"C:\Users\VVMor\source\repos\UiDesktopApp1-master\Resources\Token.txt";
        private const string ApiUrl = "https://markirovka.crpt.ru/api/v3/true-api/cises/info";
        private const int BatchSize = 1000;
        private CancellationTokenSource _cancellationTokenSource;
        private Fptr _fptr;

        public SettingsViewModel ViewModel { get; }

        // Основной конструктор с передачей ViewModel (используется при DI или ручной инициализации)
        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();

            //string metadataPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "kkt_settings_metadata.json");
            //ViewModel.LoadSettingsFromJson(metadataPath);

            LoadSavedToken();
            // Отложенная загрузка JSON-файла, чтобы избежать рекурсии
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    string metadataPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "kkt_settings_metadata.json");
                    ViewModel.LoadSettingsFromJson(metadataPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }));
        }

        // Дополнительный конструктор по умолчанию — создаёт ViewModel сам
        public SettingsPage() : this(new SettingsViewModel())
        {
        }

        // 🔹 Обновление статуса ККТ
        private void UpdateKktStatus()
        {
            try
            {
                if (_fptr == null || !_fptr.isOpened())
                {
                    btnOpenSmena.IsEnabled = false;
                    btnCloseSmena.IsEnabled = false;
                    expanderKktSettings.IsEnabled = false;
                    SetKktIcon("kkt_close.png");
                    return;
                }

                _fptr.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_SHIFT_STATE);

                // ⏱ Дать ККТ время инициализироваться перед запросом
                Thread.Sleep(100);

                _fptr.queryData();

                uint shiftState = _fptr.getParamInt(Constants.LIBFPTR_PARAM_SHIFT_STATE);

                btnOpenSmena.IsEnabled = shiftState == Constants.LIBFPTR_SS_CLOSED;
                btnCloseSmena.IsEnabled = shiftState == Constants.LIBFPTR_SS_OPENED || shiftState == Constants.LIBFPTR_SS_EXPIRED;
                expanderKktSettings.IsEnabled = true;

                switch (shiftState)
                {
                    case Constants.LIBFPTR_SS_CLOSED:
                        SetKktIcon("kkt_close.png");
                        break;
                    case Constants.LIBFPTR_SS_OPENED:
                    case Constants.LIBFPTR_SS_EXPIRED:
                        SetKktIcon("kkt_open.png");
                        break;
                    default:
                        SetKktIcon("kkt_noconnect.png");
                        break;
                }

                // 🔄 Обновляем иконки в главном окне
                if (Application.Current.MainWindow is MainWindow mainWindow)
                    mainWindow.UpdateStatusIcons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статуса ККТ: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetKktIcon(string fileName)
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon", fileName);

                if (File.Exists(iconPath))
                {
                    imgKktStatus.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath));
                }
                else
                {
                    // Подставляем пустую иконку или оставляем старую
                    imgKktStatus.Source = null;
                }
            }
            catch
            {
                // Игнорируем сбои при установке изображения
                imgKktStatus.Source = null;
            }
        }



        // 🔹 Выбор COM-портов
        private void comboComPorts_Loaded(object sender, RoutedEventArgs e)
        {
            var ports = SerialPort.GetPortNames().OrderBy(p => p).ToList();

            if (ports.Count > 0)
            {
                comboComPorts.ItemsSource = ports;
                comboComPorts.SelectedIndex = 0;
            }
            else
            {
                comboComPorts.ItemsSource = new List<string> { "Нет портов" };
                comboComPorts.SelectedIndex = 0;
            }
        }


        // 🔹 Подключение / Отключение ККТ
        private void btnConnectKKT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _fptr = KktService.Fptr;

                if (_fptr.isOpened())
                {
                    _fptr.close();
                    btnConnectKKT.Content = "Подключить ККТ";
                    expanderKktSettings.IsEnabled = false;
                    expanderKktSettings.IsExpanded = false;
                    MessageBox.Show("ККТ отключена.", "Отключение ККТ", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string selectedPort = comboComPorts.SelectedItem as string;

                if (string.IsNullOrWhiteSpace(selectedPort) || !selectedPort.StartsWith("COM"))
                {
                    MessageBox.Show("Пожалуйста, выберите COM-порт из списка.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string portNumber = new string(selectedPort.Where(char.IsDigit).ToArray());

                _fptr.setSingleSetting("model", "auto");
                _fptr.setSingleSetting("port", "com");
                _fptr.setSingleSetting("port_number", portNumber);
                _fptr.applySingleSettings();

                if (_fptr.open() == 0)
                {
                    btnConnectKKT.Content = "Отключить ККТ";
                    MessageBox.Show("ККТ успешно подключена!", "Подключение ККТ", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateKktStatus();
                    // 🔄 Обновление иконок в главном окне
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                        mainWindow.UpdateStatusIcons();

                }
                else
                {
                    MessageBox.Show($"Ошибка подключения ККТ: {_fptr.errorDescription()}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации драйвера: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Проверяем состояние ККТ при запуске
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_fptr != null && _fptr.isOpened())
                btnConnectKKT.Content = "Отключить ККТ";
            else
                btnConnectKKT.Content = "Подключить ККТ";
        }

        // 🔹 Открытие смены
        private void btnOpenSmena_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_fptr == null) return;

                _fptr.setParam(1021, "Голубец В. В.");
                _fptr.setParam(1203, "771683739093");
                _fptr.operatorLogin();
                _fptr.openShift();

                UpdateKktStatus();
                MessageBox.Show("Смена успешно открыта!", "Открытие смены", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии смены: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Закрытие смены
        private void btnCloseSmena_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_fptr == null) return;

                _fptr.setParam(1021, "Голубец В. В.");
                _fptr.setParam(1203, "771683739093");
                _fptr.operatorLogin();
                _fptr.setParam(Constants.LIBFPTR_PARAM_REPORT_TYPE, Constants.LIBFPTR_RT_CLOSE_SHIFT);
                _fptr.report();

                UpdateKktStatus();
                MessageBox.Show("Смена успешно закрыта!", "Закрытие смены", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при закрытии смены: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Настройки
        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_fptr == null)
            {
                MessageBox.Show("ККТ не подключена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var settingsInfo = new StringBuilder();

                settingsInfo.AppendLine($"Проверка срока годности КМ: {_fptr.getSingleSetting("checkMarkingCodeValidity")}");
                settingsInfo.AppendLine($"Тип кода маркировки: {_fptr.getSingleSetting("markingCodeType")}");
                settingsInfo.AppendLine($"Режим обработки маркировки: {_fptr.getSingleSetting("markingProcessingMode")}");
                settingsInfo.AppendLine($"Включена валидация кода маркировки: {_fptr.getSingleSetting("enableMarkingCodeValidation")}");
                settingsInfo.AppendLine($"Таймаут валидации КМ: {_fptr.getSingleSetting("markingCodeValidationTimeout")} мс");

                settingsInfo.AppendLine("\nВсе доступные настройки ККТ:");

                _fptr.setParam(Constants.LIBFPTR_PARAM_RECORDS_TYPE, Constants.LIBFPTR_RT_SETTINGS);
                _fptr.beginReadRecords();
                while (_fptr.readNextRecord() == Constants.LIBFPTR_OK)
                {
                    string settingName = _fptr.getParamString(Constants.LIBFPTR_PARAM_SETTING_NAME);
                    string settingValue = "";

                    switch ((int)_fptr.getParamInt(Constants.LIBFPTR_PARAM_SETTING_TYPE))
                    {
                        case Constants.LIBFPTR_ST_BOOL:
                            settingValue = _fptr.getParamBool(Constants.LIBFPTR_PARAM_SETTING_VALUE).ToString();
                            break;
                        case Constants.LIBFPTR_ST_NUMBER:
                            settingValue = _fptr.getParamInt(Constants.LIBFPTR_PARAM_SETTING_VALUE).ToString();
                            break;
                        case Constants.LIBFPTR_ST_STRING:
                            settingValue = _fptr.getParamString(Constants.LIBFPTR_PARAM_SETTING_VALUE);
                            break;
                    }

                    settingsInfo.AppendLine($"{settingName}: {settingValue}");
                }
                _fptr.endReadRecords();

                MessageBox.Show(settingsInfo.ToString(), "Настройки ККТ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения настроек ККТ: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_fptr == null || !_fptr.isOpened())
                {
                    MessageBox.Show("ККТ не подключена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ViewModel.ApplySettingsToFptr(_fptr);
                MessageBox.Show("Настройки успешно применены!", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string LogFilePath { get; set; } = LogHelper.logFilePath;

        private void BtnSelectLogFile_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.SelectLogFilePath();
            txtLogFilePath.Text = LogHelper.logFilePath;
        }


        private void LoadSavedToken()
        {
            try
            {
                if (File.Exists(TokenFilePath))
                    _saveToken = File.ReadAllText(TokenFilePath).Trim();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка чтения токена: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // 🔄 Обновление иконки токена
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                    mainWindow.UpdateStatusIcons();
            });
        }

    }
}