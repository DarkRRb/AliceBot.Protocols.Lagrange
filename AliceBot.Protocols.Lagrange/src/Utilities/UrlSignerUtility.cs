using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using AliceBot.Core.Loggers;
using Lagrange.Core.Utility.Sign;
using CoreProtocols = Lagrange.Core.Common.Protocols;

namespace AliceBot.Protocols.Lagrange.Utilities;

public class UrlSigner : SignProvider {
    private readonly ILogger _logger;

    private readonly string _url;

    private readonly string _platform;

    private readonly string _version;

    private readonly HttpClient _client = new();

    public UrlSigner(ILogger logger, string url, CoreProtocols platform) {
        _logger = logger;
        _url = url;
        switch (platform) {
            case CoreProtocols.Windows: {
                _platform = "Windows";
                _version = "9.9.2-15962";
                break;
            }
            case CoreProtocols.MacOs: {
                _platform = "MacOs";
                _version = "6.9.23-20139";
                break;
            }
            case CoreProtocols.Linux: {
                _platform = "Linux";
                _version = "3.2.10-25765";
                break;
            }
            default: { throw new Exception("Unsupported platform"); }
        }
    }

    public override byte[]? Sign(string cmd, uint seq, byte[] body, out byte[]? e, out string? t) {
        e = null;
        t = null;

        if (!WhiteListCommand.Contains(cmd)) return null;
        if (_url == null) throw new Exception("Sign server is not configured");

        using var request = new HttpRequestMessage {
            Method = HttpMethod.Post,
            RequestUri = new Uri(_url),
            Content = new StringContent(
                $"{{\"cmd\":\"{cmd}\",\"seq\":{seq},\"src\":\"{Convert.ToHexString(body)}\"}}",
                new MediaTypeHeaderValue(MediaTypeNames.Application.Json
            ))
        };

        using var message = _client.Send(request);
        if (message.StatusCode != HttpStatusCode.OK) throw new Exception($"Signer server returned a {message.StatusCode}");
        var json = JsonDocument.Parse(message.Content.ReadAsStream()).RootElement;

        if (json.TryGetProperty("platform", out JsonElement platformJson)) {
            if (platformJson.GetString() != _platform) throw new Exception("Signer platform mismatch");
        } else {
            _logger.Warn("Signer platform miss");
        }

        if (json.TryGetProperty("version", out JsonElement versionJson)) {
            if (versionJson.GetString() != _version) throw new Exception("Signer version mismatch");
        } else {
            _logger.Warn("Signer version miss");
        }

        var valueJson = json.GetProperty("value");
        var extraJson = valueJson.GetProperty("extra");
        var tokenJson = valueJson.GetProperty("token");
        var signJson = valueJson.GetProperty("sign");

        string? token = tokenJson.GetString();
        string? extra = extraJson.GetString();
        e = extra != null ? Convert.FromHexString(extra) : [];
        t = token != null ? Encoding.UTF8.GetString(Convert.FromHexString(token)) : "";
        string sign = signJson.GetString() ?? throw new Exception("Signer server returned an empty sign");
        return Convert.FromHexString(sign);
    }
}