using System.Text.Json.Serialization;
using CoreProtocols = Lagrange.Core.Common.Protocols;

namespace AliceBot.Protocols.Lagrange.Configs;

public class ProtocolConfig {
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required CoreProtocols Protocol { get; set; }

    public required string SignerUrl { get; set; }
    
    public required bool AutoRelogin { get; set; }
}