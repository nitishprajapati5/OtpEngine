using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OtpVerfication.API.Models;
using OtpVerfication.API.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OtpVerfication.API
{
    public class OtpVerification : IOtpVerfication
    {
        private readonly IHttpContextAccessor httpContext;
        private readonly IDistributedCache distributedCache;
        private readonly IMemoryCache memoryCache;
        private readonly IDataProtector dataProtection;
        private readonly OtpVerificationOptions options;

        public OtpVerification(IDataProtectionProvider _dataProtection, IHttpContextAccessor _httpContext,
                                IDistributedCache _distributedCache = null, IMemoryCache _memoryCache = null,
                                IOptions<OtpVerificationOptions> _options = null)
        {
            this.httpContext = _httpContext;
            this.distributedCache = _distributedCache;
            this.memoryCache = _memoryCache;
            this.options = _options?.Value ?? new OtpVerificationOptions();
            this.dataProtection = _dataProtection.CreateProtector("This is a very secure key");
        }

        private record class IdPlain(string id, string plain);

        private string BaseOtpUrl => $"{httpContext.HttpContext.Request.Scheme}";

        private string Key(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException($"{nameof(OtpVerfication)}Unique{nameof(id)}) can't be null or emply");

            return $"{nameof(OtpVerfication)}{id}";
        }

        private bool TryUnprotectUrl(string key,out string id,out string plain)
        {
            id = plain = String.Empty;

            try
            {
                var data = dataProtection.Unprotect(key);

                var obj = System.Text.Json.JsonSerializer.Deserialize<IdPlain>(data);
                id = obj.id;
                plain = obj.plain;
                return true;
            }
            catch(Exception ex) when (ex is CryptographicException || ex is RuntimeBinderException)
            {
                return false;
            }
        }

        public OtpVia Generate(string id)
        {
            return Generate(id, out _);
        }

        public OtpVia Generate(string id, out DateTime expire)
        {
            return Generate(id, options, out expire);
        }

        public OtpVia Generate(string id, OtpVerificationOptions option)
        {
            return Generate(id, option, out _);
        }

        public OtpVia Generate(string id,OtpVerificationOptions option,out DateTime expire)
        {
            var plain = OtpVerificationExtension.Generate(option, out expire, out string hash);

            if (options.IsInMemoryCache)
            {
                memoryCache.Set(Key(id),hash,new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = expire,
                    Priority = CacheItemPriority.High,
                });
            }
            else
            {
                distributedCache.SetString(Key(id), hash, new DistributedCacheEntryOptions()
                {
                    AbsoluteExpiration = expire,
                });
            }

            string url = string.Empty;
            if (option.EnableUrl)
            {
                url = BaseOtpUrl + dataProtection.Protect(
                        System.Text.Json.JsonSerializer.Serialize(new IdPlain(id, plain)));

            }
            return new OtpVia(plain, url);
        }

        public bool Scan(string id,string plain,OtpVerificationOptions option)
        {
            string hash = string.Empty;

            if(options.IsInMemoryCache)
            {
                 hash = memoryCache.Get<string>(Key(id));
            }
            else
            {
                 hash = distributedCache.GetString(Key(id));
            }

            if(hash is null)
            {
                return false;
            }

            if (OtpVerificationExtension.Scan(plain, hash, option))
            {
                if (options.IsInMemoryCache)
                {
                    memoryCache.Remove(Key(id));
                }
                else
                {
                    distributedCache.Remove(Key(id));
                }
                return true;
            }

            return false;
        }

        public bool Scan(string id,string plain,int expire)
        {
            return Scan(id, plain, new OtpVerificationOptions() { Expire = expire });
        }

        public bool Scan(string id,string plain)
        {
            return Scan(id, plain, options);
        }

        public bool Scan(string url)
        {
            if(TryUnprotectUrl(url,out string id,out string code))
            {
                return Scan(id, code);
            }

            return false;
        }
    }
}
