using System.Text.Json;

namespace Core.Cache.Providers
{
    internal class JsonFileCacheProvider<T>(string cacheFilePath) : ICacheProvider<T>
        where T : class
    {
        public async Task<T> GetAsync()
        {
            try
            {
                var json = await File.ReadAllTextAsync(cacheFilePath);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                //TODO: add feedback
                return default;
            }
        }

        public async Task SaveAsync(T cache)
        {
            try
            {
                var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IncludeFields = true
                });

                await File.WriteAllTextAsync(cacheFilePath, json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to save cache file: ${ex.Message}");
            }
        }
    }
}
