// 📌 Models/UpdProduct.cs

using System;
using System.ComponentModel;

namespace UiDesktopApp1.Models
{
    public class UpdProduct : INotifyPropertyChanged
    {
        public int НомСтр
        {
            get; set;
        }
        public string НаимТов
        {
            get; set;
        }
        public int КолТов
        {
            get; set;
        }

        private decimal _ценаТов;
        public decimal ЦенаТов
        {
            get => _ценаТов;
            set
            {
                _ценаТов = value;
                OnPropertyChanged(nameof(ЦенаТов));
                OnPropertyChanged(nameof(СтТовБезНДС));
                OnPropertyChanged(nameof(СтТовУчНал));
            }
        }

        public decimal СтТовБезНДС => ЦенаТов * КолТов;
        public decimal СтТовУчНал => СтТовБезНДС;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
