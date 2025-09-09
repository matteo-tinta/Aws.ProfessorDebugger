using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Cache.Providers
{
    internal interface ICacheProvider
    {
        public Task<SerializableAwsCache?> GetAsync();
        public Task SaveAsync(SerializableAwsCache cache);
    }
}
