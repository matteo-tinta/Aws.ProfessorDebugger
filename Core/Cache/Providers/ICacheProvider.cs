using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Cache.Providers
{
    internal interface ICacheProvider<T> where T: class
    {
        public Task<T?> GetAsync();
        public Task SaveAsync(T cache);
    }
}
