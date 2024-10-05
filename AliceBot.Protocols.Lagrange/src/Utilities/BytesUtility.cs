using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AliceBot.Protocols.Lagrange.Utilities;

public class BytesUtility {
    public static async Task<byte[]> FromUrlAsync(string url, CancellationToken token) {
        int split = url.IndexOf(':');
        return url[..split] switch {
            "base64" => Convert.FromBase64String(url[(split + 3)..]),
            "file" => await File.ReadAllBytesAsync(url[(split + 4)..], token),
            "http" or "https" => await HttpClientUtility.GetByteArrayAsync(url, token),
            _ => throw new NotSupportedException("Unsupported protocol."),
        };
    }
}