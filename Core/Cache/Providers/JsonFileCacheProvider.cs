using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Core.Cache.Providers
{
    internal class JsonFileCacheProvider : ICacheProvider
    {
        private readonly string cacheFilePath;

        public JsonFileCacheProvider(string cacheFilePath)
        {
            this.cacheFilePath = cacheFilePath;
        }

        public async Task<SerializableAwsCache> GetAsync()
        {
            try
            {
                var json = await File.ReadAllTextAsync(cacheFilePath);
                return JsonSerializer.Deserialize<SerializableAwsCache>(json);
            }
            catch
            {
                Console.WriteLine("Warning: Failed to load AWS cache. Continuing with empty cache.");
                return null;
            }
        }

        public async Task SaveAsync(SerializableAwsCache cache)
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
