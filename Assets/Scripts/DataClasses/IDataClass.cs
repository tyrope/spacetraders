using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STCommander
{
    public interface IDataClass
    {
        public Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge );
        public Task<bool> SaveToCache();
    }
}
