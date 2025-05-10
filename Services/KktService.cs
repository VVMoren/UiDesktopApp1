using Atol.Drivers10.Fptr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public static class KktService
{
    private static Fptr? _fptr;

    public static Fptr Fptr
    {
        get
        {
            if (_fptr == null)
            {
                try
                {
                    _fptr = new Fptr();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Ошибка инициализации ККТ: " + ex.Message);
                    throw;
                }
            }
            return _fptr;
        }
    }
}
