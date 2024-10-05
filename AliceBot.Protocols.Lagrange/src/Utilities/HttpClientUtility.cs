using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AliceBot.Protocols.Lagrange.Utilities;

public class HttpClientUtility {
    private static readonly HttpClient client = new();

    public static async Task<byte[]> GetByteArrayAsync(string url, CancellationToken token) {
        using HttpResponseMessage response = await client.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(token);
    }
}