using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtpVerfication.API.Options
{
    public class OtpVerificationOptions
    {
        internal bool IsInMemoryCache { get; set; } = false;
        public bool EnableUrl { get; set; } = true;
        public int Iterations { get; set; } = 1;
        public int Size { get; set; } = 6;
        public int Length { get; set; } = 20;
        public int Expire { get; set; } = 2;
    }


}
