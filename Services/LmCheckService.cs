using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Services
{
    public class CisCheckRequest
    {
        public string[] codes { get; set; }
        public string fiscalDriveNumber { get; set; }
    }

    public class CisCheckResponse
    {
        public int code { get; set; }
        public string description { get; set; }
        public List<CisResult> codes { get; set; }
    }

    public class CisResult
    {
        public string cis { get; set; }
        public bool valid { get; set; }
        public bool found { get; set; }
        public bool isBlocked { get; set; }
        public bool sold { get; set; }
    }

}
