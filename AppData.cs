using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1
{
    public static class AppData
    {
        public static ObservableCollection<RequestedCisItem> ItemsForSale = new();
        public static ObservableCollection<RequestedCisItem> ItemsForSellReturn = new();
    }

}
