using CoreLib.CORE.Helpers.CollectionHelpers;
using KKTApp3.ViewModels;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using UiDesktopApp1.Models;
using UiDesktopApp1.ViewModels.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Abstractions;
using UiDesktopApp1.Helpers;
using UiDesktopApp1.Services;
using UiDesktopApp1.ViewModels.Pages;
using UiDesktopApp1.Views.Windows;
using UiDesktopApp1.Models;

namespace UiDesktopApp1.Views.Pages
{
    public partial class DataPage : INavigableView<DataViewModel>
    {
        public DataViewModel ViewModel { get; }

        public DataPage(DataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel; 
            InitializeComponent();
        }


        // ✅ Обработчик кнопки для загрузки из файла
        private async void btnLoadTxt_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                Title = "Выберите файл"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                btnSelectApplied.Visibility = Visibility.Visible;
                btnSelectWithdrawn.Visibility = Visibility.Visible;
                await ViewModel.LoadFromFileAsync(openFileDialog.FileName);

            }
        }

        // Обработчик кнопки для очистки таблицы
        private void btnClearTable_Click(object sender, RoutedEventArgs e)
        {
            // Очистка списка, отображаемого в таблице
            ViewModel.RequestedCisList.Clear();
        }

        // Обработчик кнопки для удаления диапазона строк
        private void btnDeleteRange_Click(object sender, RoutedEventArgs e)
        {
            // Создаем и открываем окно
            var dialog = new DellDiapason();
            var result = dialog.ShowDialog();

            if (result == true)
            {
                // Парсим введённые значения
                if (int.TryParse(dialog.StartIndex, out int startIndex) &&
                    int.TryParse(dialog.EndIndex, out int endIndex))
                {
                    if (startIndex < 0 || endIndex < 0 || startIndex > endIndex || endIndex >= ViewModel.RequestedCisList.Count)
                    {
                        MessageBox.Show("Указан некорректный диапазон.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Удаляем строки в диапазоне
                    var itemsToRemove = ViewModel.RequestedCisList.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
                    foreach (var item in itemsToRemove)
                    {
                        ViewModel.RequestedCisList.Remove(item);
                    }
                }
                else
                {
                    MessageBox.Show("Введите корректные числовые значения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }


        // Обработчик кнопки для обновления данных
        private async void btnUpdateData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Автоматически получаем актуальный статус без запроса
                if (ViewModel != null)
                {
                    // Вызываем метод FetchCisInfoBatchedAsync из ViewModel
                    await ViewModel.FetchCisInfoBatchedAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSelectApplied_Click(object sender, RoutedEventArgs e)
        {
            SelectRowsByStatus("Нанесён");
        }

        private void btnSelectWithdrawn_Click(object sender, RoutedEventArgs e)
        {
            SelectRowsByStatus("Выбыл");
        }

        private void SelectRowsByStatus(string statusToMatch)
        {
            // Сброс всех чекбоксов
            foreach (var item in ViewModel.RequestedCisList)
                item.IsSelected = false;

            // Отметка нужных строк
            bool anyMatched = false;
            foreach (var item in ViewModel.RequestedCisList)
            {
                if (item.Status == statusToMatch)
                {
                    item.IsSelected = true;
                    anyMatched = true;
                }
            }

            // Если ничего не найдено, можно показать сообщение (по желанию)
            // if (!anyMatched)
            //     MessageBox.Show($"Нет строк со статусом: {statusToMatch}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnSell_Click(object sender, RoutedEventArgs e)
        {
            var selected = ViewModel.RequestedCisList
                .Where(item => item.IsSelected && item.Status == "Нанесён")
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("Нет выбранных КИ со статусом 'Нанесён'.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AppData.ItemsForSale.Clear();
            foreach (var item in selected)
                AppData.ItemsForSale.Add(item);

            // Навигация на страницу Продажа
            var navService = (INavigationService)App.Services.GetService(typeof(INavigationService));
            navService?.Navigate(typeof(UiDesktopApp1.Views.Pages.SalesPage));
        }

        private void btnSellReturn_Click(object sender, RoutedEventArgs e)
        {
            var selected = ViewModel.RequestedCisList
                .Where(item => item.IsSelected && item.Status == "Выбыл")
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("Нет выбранных КИ со статусом 'Нанесён'.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AppData.ItemsForSellReturn.Clear();
            foreach (var item in selected)
                AppData.ItemsForSellReturn.Add(item);

            // Навигация на страницу Продажа
            var navService = (INavigationService)App.Services.GetService(typeof(INavigationService));
            navService?.Navigate(typeof(UiDesktopApp1.Views.Pages.SellReturnPage));
        }

        private void dataGridRequestedCis_CurrentCellChanged(object sender, EventArgs e)
        {
            //btnSell.Visibility = ViewModel.HasSelectedAppliedItems ? Visibility.Visible : Visibility.Collapsed;
        }


    }
}
