using Lagrange.Core.Common;

namespace AliceBot.Protocols.Lagrange.Configs;

public class RootConfig {
    public required ProtocolConfig Protocol { get; set; }

    public required ServerConfig Server { get; set; }

    public BotDeviceInfo? DeviceInfo { get; set; }

    public BotKeystore? Keystore { get; set; }
}