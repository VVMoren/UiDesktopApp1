using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models
{
    public class ApiResponse
    {
        public CisInfo CisInfo { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
    }

}