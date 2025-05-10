using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Abstractions.Controls;
using System;
using System.Collections.ObjectModel;


namespace KKTApp3.ViewModels
{
    public class DataGrid0ViewModel : ObservableObject, INavigationAware
    {
        public Task OnNavigatedToAsync()
        {
            OnNavigatedTo(null);
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            OnNavigatedFrom();
            return Task.CompletedTask;
        }

        public interface ISampleDataService
        {
            Task<IEnumerable<SampleOrder>> GetGridDataAsync();
        }
        public class SampleOrder
        {
            public int OrderID { get; set; }
            public DateTime OrderDate { get; set; }
            public string Company { get; set; }
            public string ShipTo { get; set; }
            public decimal OrderTotal { get; set; }
            public string Status { get; set; }
            public string Symbol { get; set; }
        }
        public class SampleDataService : ISampleDataService
        {
            public Task<IEnumerable<SampleOrder>> GetGridDataAsync()
            {
                return Task.FromResult<IEnumerable<SampleOrder>>(new List<SampleOrder>
        {
            new SampleOrder { OrderID = 1, OrderDate = DateTime.Now, Company = "Компания 1", ShipTo = "Москва", OrderTotal = 1234, Status = "Готов", Symbol = "✔" },
            new SampleOrder { OrderID = 2, OrderDate = DateTime.Now, Company = "Компания 2", ShipTo = "Питер", OrderTotal = 4321, Status = "В пути", Symbol = "🚚" }
        });
            }
        }

        private readonly ISampleDataService _sampleDataService;
    
        public ObservableCollection<SampleOrder> Source { get; } = new ObservableCollection<SampleOrder>();
    
        public DataGrid0ViewModel(ISampleDataService sampleDataService)
        {
            _sampleDataService = sampleDataService;
        }
    
        public async void OnNavigatedTo(object parameter)
        {
            Source.Clear();
    
            // Replace this with your actual data
            var data = await _sampleDataService.GetGridDataAsync();
    
            foreach (var item in data)
            {
                Source.Add(item);
            }
        }
    
        public void OnNavigatedFrom()
        {
        }
    }
    }