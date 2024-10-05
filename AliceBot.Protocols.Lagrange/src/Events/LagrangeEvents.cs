using System;
using AliceBot.Core.Events;
using AliceBot.Core.Events.Data;
using AliceBot.Core.Loggers;
using AliceBot.Core.Protocols;
using AliceBot.Protocols.Lagrange.Messages;
using Lagrange.Core;
using Lagrange.Core.Event.EventArg;

namespace AliceBot.Protocols.Lagrange.Events;

public class LagrangeEvents(ILogger logger, LagrangeProtocol protocol) : IProtocolEvents {
    private readonly ILogger _logger = logger;

    private readonly LagrangeProtocol _protocol = protocol;

    public event Action<IProtocol, PrivateMessageData>? OnPrivateMessage;

    public event Action<IProtocol, GroupMessageData>? OnGroupMessage;

    public void BotLogHandler(BotContext _lagrange, BotLogEvent @event) {
        _logger.Log(
            @event.Level switch {
                LogLevel.Debug or
                LogLevel.Verbose => ILogger.Level.Trace,
                LogLevel.Information => ILogger.Level.Info,
                LogLevel.Warning => ILogger.Level.Warn,
                LogLevel.Exception or
                LogLevel.Fatal or
                _ => ILogger.Level.Error,
            },
            @event.ToString()
        );
    }

    public void BotOnlineEvent(BotContext lagrange, BotOnlineEvent @event) {
        _logger.Log(ILogger.Level.Info, $"Bot {lagrange.BotUin} online.");
    }

    public void FriendMessageHandler(BotContext _lagrange, FriendMessageEvent @event) {
        OnPrivateMessage?.Invoke(_protocol, new PrivateMessageData(@event.Chain.ToPrivateMessage()));
    }

    public void GroupMessageHandler(BotContext _lagrange, GroupMessageEvent @event) {
        OnGroupMessage?.Invoke(_protocol, new GroupMessageData(@event.Chain.ToGroupMessage()));
    }

    public void Aggregate(BotContext lagrange) {
        lagrange.Invoker.OnBotLogEvent += BotLogHandler;
        lagrange.Invoker.OnBotOnlineEvent += BotOnlineEvent;
        lagrange.Invoker.OnFriendMessageReceived += FriendMessageHandler;
        lagrange.Invoker.OnGroupMessageReceived += GroupMessageHandler;
    }

    public void Disaggregate(BotContext lagrange) {
        lagrange.Invoker.OnBotLogEvent -= BotLogHandler;
        lagrange.Invoker.OnBotOnlineEvent -= BotOnlineEvent;
        lagrange.Invoker.OnFriendMessageReceived -= FriendMessageHandler;
        lagrange.Invoker.OnGroupMessageReceived -= GroupMessageHandler;
    }
}