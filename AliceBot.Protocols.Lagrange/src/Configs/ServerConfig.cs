namespace AliceBot.Protocols.Lagrange.Configs;

public class ServerConfig {
    public required bool AutoReconnect { get; set; }

    public required bool UseIpv6Network { get; set; }

    public required bool GetOptimumServer { get; set; }
}