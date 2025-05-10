using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Atol.Drivers10.Fptr;
using KKTServiceLib.Atol;
using UiDesktopApp1.Services;
using UiDesktopApp1.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace UiDesktopApp1.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel
        {
            get;
        }

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            navigationService.SetNavigationControl(RootNavigation);

            // ⏬ Обновляем иконки при запуске
            UpdateStatusIcons();
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Завершить приложение при закрытии окна
            Application.Current.Shutdown();
        }

        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        // 🔹 Метод для обновления иконок состояния
        public void UpdateStatusIcons()
        {
            try
            {
                string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon");

                ImgKktStatus.Source = new BitmapImage(new Uri(Path.Combine(basePath,
                    IsKktConnected() ? "kkt_open.png" : "kkt_close.png")));

                ImgShiftStatus.Source = new BitmapImage(new Uri(Path.Combine(basePath,
                    IsShiftOpened() ? "SmenaOPEN.png" : "SmenaCLOSE.png")));

                ImgTokenStatus.Source = new BitmapImage(new Uri(Path.Combine(basePath,
                    IsTokenAvailable() ? "TokenTRUE.png" : "TokenFALSE.png")));

                ImgKktStatus.ToolTip = IsKktConnected() ? "ККТ подключена" : "ККТ не подключена";
                ImgShiftStatus.ToolTip = IsShiftOpened() ? "Смена открыта" : "Смена закрыта";
                ImgTokenStatus.ToolTip = IsTokenAvailable() ? "Токен получен" : "Токен не получен";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Ошибка обновления иконок: {ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        private bool IsKktConnected()
        {
            return KktService.Fptr?.isOpened() == true;
        }

        private bool IsShiftOpened()
        {
            var fptr = KktService.Fptr;
            if (fptr == null || !fptr.isOpened()) return false;

            try
            {
                fptr.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_SHIFT_STATE);
                fptr.queryData();
                var state = fptr.getParamInt(Constants.LIBFPTR_PARAM_SHIFT_STATE);
                return state == Constants.LIBFPTR_SS_OPENED || state == Constants.LIBFPTR_SS_EXPIRED;
            }
            catch
            {
                return false;
            }
        }

        private bool IsTokenAvailable()
        {
            string tokenPath = @"C:\SQLMCTApp2\Token.txt";
            return File.Exists(tokenPath) && !string.IsNullOrWhiteSpace(File.ReadAllText(tokenPath));
        }
    }
}
