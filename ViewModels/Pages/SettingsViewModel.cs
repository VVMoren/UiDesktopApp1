using Atol.Drivers10.Fptr;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UiDesktopApp1.Models;
using UiDesktopApp1.Services;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = string.Empty;

        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

        [ObservableProperty]
        private ObservableCollection<KktSettingItem> kktSettings = new();

        public void LoadSettingsFromJson(string jsonPath)
        {
            try
            {
                string fullPath = Path.Combine(AppContext.BaseDirectory, jsonPath);

                if (!File.Exists(fullPath))
                {
                    MessageBox.Show($"Файл не найден: {fullPath}", "Ошибка загрузки JSON", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string json = File.ReadAllText(fullPath);
                var metadata = JsonConvert.DeserializeObject<Dictionary<int, KktSettingMetadata>>(json);


                if (metadata == null)
                {
                    MessageBox.Show("Не удалось прочитать настройки из JSON.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                kktSettings.Clear();

                foreach (var entry in metadata)
                {
                    var settingId = entry.Key;
                    var meta = entry.Value;

                    var item = new KktSettingItem
                    {
                        Id = settingId.ToString(),
                        Name = meta.Name,
                        Type = meta.Type,
                        Category = meta.Category
                    };


                    if (meta.Type == "bool")
                    {
                        item.BoolValue = false; // Значение будет позже заменено из драйвера
                    }
                    else if (meta.Type == "string" || meta.Type == "range")
                    {
                        item.StringValue = string.Empty;
                    }
                    else if (meta.Type == "select")
                    {
                        item.Options = new ObservableCollection<ValueText>(meta.Options);
                        item.SelectedValue = item.Options.FirstOrDefault()?.Value.ToString() ?? "0";
                    }

                    kktSettings.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке настроек из JSON: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadSettingsMetadata(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                MessageBox.Show($"Файл не найден: {jsonPath}");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            var metadata = JsonConvert.DeserializeObject<Dictionary<int, KktSettingMetadata>>(json);


            if (metadata == null)
            {
                MessageBox.Show("Метаданные не загружены.");
                return;
            }

            KktSettings.Clear();

            foreach (var entry in metadata)
            {
                var settingId = entry.Key;
                var meta = entry.Value;

                var item = new KktSettingItem
                {
                    Id = settingId.ToString(),
                    Name = $"#{settingId}",
                    Type = meta.Type,
                    Options = new ObservableCollection<ValueText>(meta.Options ?? new())
                };

                // Временные значения
                if (meta.Type == "bool")
                    item.BoolValue = false;
                else if (meta.Type is "range" or "string")
                    item.StringValue = "";
                else if (meta.Type == "select")
                    item.SelectedValue = item.Options.FirstOrDefault()?.Value.ToString() ?? "";

                KktSettings.Add(item);
            }
        }


        public void ApplySettingsToFptr(IFptr fptr)
        {
            foreach (var setting in KktSettings)
            {
                switch (setting.Type)
                {
                    case "bool":
                        fptr.setSingleSetting(setting.Id, setting.BoolValue ? "1" : "0");
                        break;
                    case "range":
                    case "string":
                        fptr.setSingleSetting(setting.Id, setting.StringValue ?? "");
                        break;
                    case "select":
                        fptr.setSingleSetting(setting.Id, setting.SelectedValue ?? "");
                        break;
                }
            }

            fptr.applySingleSettings();
        }

        public class KktSettingItem : ObservableObject
        {
            public string Category { get; set; }
            public string Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }

            public ObservableCollection<ValueText> Options { get; set; } = new();

            private string _stringValue;
            public string StringValue { get => _stringValue; set => SetProperty(ref _stringValue, value); }

            private bool _boolValue;
            public bool BoolValue { get => _boolValue; set => SetProperty(ref _boolValue, value); }

            private string _selectedValue;
            public string SelectedValue { get => _selectedValue; set => SetProperty(ref _selectedValue, value); }
        }
        public Task OnNavigatedToAsync() => Task.CompletedTask;
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private readonly MarkirovkaApiService _markirovkaApiService;

        public SettingsViewModel()
        {
            _markirovkaApiService = new MarkirovkaApiService();
        }

        [RelayCommand]
        private async Task SendRequestAsync()
        {
            try
            {
                string token = await _markirovkaApiService.GetTokenWithRetryAsync();
                MessageBox.Show($"Токен получен:\n{token}", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения токена:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
