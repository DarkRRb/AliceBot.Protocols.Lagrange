using AliceBot.Core.Loggers;
using AliceBot.Protocols.Lagrange.Utilities;
using Lagrange.Core.Common;

namespace AliceBot.Protocols.Lagrange.Configs.Converters;

public static class ConfigConverter {
    public static BotConfig ToBotConfig(this RootConfig config, ILogger logger) {
        return new BotConfig {
            Protocol = config.Protocol.Protocol,
            AutoReconnect = config.Server.AutoReconnect,
            UseIPv6Network = config.Server.UseIpv6Network,
            GetOptimumServer = config.Server.GetOptimumServer,
            CustomSignProvider = new UrlSigner(logger, config.Protocol.SignerUrl, config.Protocol.Protocol),
            AutoReLogin = config.Protocol.AutoRelogin
        };
    }
}