using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtpVerfication.API.Models
{
    public struct OtpVia
    {
        public OtpVia(string code,string url = default)
        {
            Code = code;
            Url= url;
        }

        public string Code { get; set; }
        public string Url { get; set; }

        public override string ToString()
        {
            return $"Code:{Code},Url:{Url}";
        }
    }
}
