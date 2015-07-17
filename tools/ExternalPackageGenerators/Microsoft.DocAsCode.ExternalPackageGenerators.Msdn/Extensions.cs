namespace Microsoft.DocAsCode.ExternalPackageGenerators.Msdn
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    internal static class Extensions
    {
        public static IEnumerable<XmlReader> Elements(this XmlReader reader, string name)
        {
            reader.Read();
            while (reader.ReadToNextSibling(name))
            {
                using (var result = reader.ReadSubtree())
                {
                    result.Read();
                    yield return result;
                }
            }
        }

        public static IEnumerable<T> ProtectResource<T>(this IEnumerable<T> source)
            where T : IDisposable
        {
            foreach (var item in source)
            {
                using (item)
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> EmptyIfThrow<T>(this Func<T> func)
        {
            try
            {
                return new[] { func() };
            }
            catch (Exception)
            {
                return Enumerable.Empty<T>();
            }
        }

        public static async Task<HttpResponseMessage> GetWithRetryAsync(this HttpClient client, string url, params int[] retryDelay)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    return await client.GetAsync(url);
                }
                catch (TaskCanceledException)
                {
                    if (retryCount >= retryDelay.Length)
                    {
                        throw;
                    }
                }
                await Task.Delay(retryDelay[retryCount]);
                retryCount++;
            }
        }
    }
}
