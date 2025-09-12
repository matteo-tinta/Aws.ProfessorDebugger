namespace Core.Cache.Providers
{
    internal interface ICacheProvider<T> where T: class
    {
        public Task<T?> GetAsync();
        public Task SaveAsync(T cache);
    }
}
