using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtpVerfication.API.Options
{
    public static class OtpVerificationOptionsExtensions
    {
        public static OtpVerificationOptions UseInMemoryCache(this OtpVerificationOptions options)
        {
            options.IsInMemoryCache = true;
            return options;
        }
    }
}
