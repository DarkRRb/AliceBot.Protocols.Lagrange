using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AliceBot.Core;
using AliceBot.Core.Actions;
using AliceBot.Core.Actions.Results;
using AliceBot.Core.Events;
using AliceBot.Core.Loggers;
using AliceBot.Core.Messages;
using AliceBot.Core.Protocols;
using AliceBot.Protocols.Lagrange.Configs;
using AliceBot.Protocols.Lagrange.Configs.Converters;
using AliceBot.Protocols.Lagrange.Events;
using AliceBot.Protocols.Lagrange.Messages;
using AliceBot.Protocols.Lagrange.Utilities;
using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace AliceBot.Protocols.Lagrange;

public partial class LagrangeProtocol : IProtocol, IActions {
    private const string CONFIG_KEY = "Lagrange";

    private readonly Alice _alice;

    private readonly ILogger _logger;

    private readonly RootConfig _config;

    private CancellationTokenSource _cts = new();

    private readonly LagrangeEvents _events;

    private BotContext? _lagrange;

    public IProtocolEvents Events => _events;

    public IActions Actions => this;

    public LagrangeProtocol(Alice alice) {
        _alice = alice;
        _logger = alice.GetLogger(nameof(LagrangeProtocol));
        _config = alice.GetConfig<RootConfig>(CONFIG_KEY);
        _events = new(_logger, this);
    }

    public static LagrangeProtocol Create(Alice alice) {
        return new(alice);
    }

    public Task<GetSelfInfoResult> GetSelfInfo(CancellationToken token) {
        if (_lagrange == null) {
            throw new InvalidOperationException("Lagrange is not started.");
        }

        return Task.FromResult(new GetSelfInfoResult(_lagrange.BotUin.ToString(), _lagrange.BotName ?? throw new InvalidOperationException("Failed to get the bot name.")));
    }

    public async Task<SendPrivateMessageResult> SendPrivateMessageAsync(string userId, MessageContent message, CancellationToken token) {
        if (_lagrange == null) {
            throw new InvalidOperationException("Lagrange is not started.");
        }

        MessageResult result = await _lagrange.SendMessageAsync(
            await message.ToFriendChainAsync(_lagrange, userId, token),
            token
        );
        if (!result.Sequence.HasValue) {
            throw new InvalidOperationException("Failed to send private message.");
        }

        return new SendPrivateMessageResult(MessageIdUtility.GeneratePrivateMessageId(userId, result.Sequence.Value));
    }

    public async Task<SendGroupMessageResult> SendGroupMessageAsync(string groupId, MessageContent message, CancellationToken token) {
        if (_lagrange == null) {
            throw new InvalidOperationException("Lagrange is not started.");
        }

        MessageResult result = await _lagrange.SendMessageAsync(
            await message.ToGroupChainAsync(_lagrange, groupId, token),
            token
        );
        if (!result.Sequence.HasValue) {
            throw new InvalidOperationException("Failed to send group message.");
        }

        return new SendGroupMessageResult(MessageIdUtility.GenerateGroupMessageId(groupId, result.Sequence.Value));
    }

    public async Task RecallMessageAsync(string messageId, CancellationToken token) {
        if (_lagrange == null) {
            throw new InvalidOperationException("Lagrange is not started.");
        }

        MessageIdUtility.CheckIsSelf(messageId);

        if (MessageIdUtility.IsGroup(messageId)) {
            string[] parts = messageId.Split(':');
            uint groupId = uint.Parse(parts[2]);
            uint sequence = uint.Parse(parts[3]);

            if (!await _lagrange.RecallGroupMessage(groupId, sequence)) {
                throw new InvalidOperationException("Failed to recall the message.");
            }
        } else if (MessageIdUtility.IsPrivate(messageId)) {
            if (!await _lagrange.RecallFriendMessage(await _lagrange.GetMessageByMessageId(messageId, token))) {
                throw new InvalidOperationException("Failed to recall the message.");
            }
        } else {
            throw new InvalidOperationException("The message is not from AliceBot.Protocols.Lagrange.");
        }
    }

    public async Task StartAsync(CancellationToken token) {
        if (_lagrange != null) {
            throw new InvalidOperationException("Lagrange is already started.");
        }

        token = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token).Token;

        try {
            _lagrange = BotFactory.Create(
                _config.ToBotConfig(_logger),
                _config.DeviceInfo ??= BotDeviceInfo.GenerateInfo(),
                _config.Keystore ??= new BotKeystore()
            );
            _alice.SaveConfig(CONFIG_KEY, _config);

            _events.Aggregate(_lagrange);

            if (_lagrange.UpdateKeystore().Session.TempPassword is { Length: > 0 }) {
                if (await _lagrange.LoginByPasswordAsync(token)) {
                    _config.Keystore = _lagrange.UpdateKeystore();
                    _alice.SaveConfig(CONFIG_KEY, _config);

                    return;
                }
            }

            _logger.Info($"\n{QrCodeUtility.CreateConsoleQrCodeString(await _lagrange.FetchQrCodeAsync(token))}");
            await _lagrange.LoginByQrCodeAsync(token);

            _config.Keystore = _lagrange.UpdateKeystore();
            _alice.SaveConfig(CONFIG_KEY, _config);
        } catch {
            if (_lagrange != null) {
                _events.Disaggregate(_lagrange);
                _lagrange.Dispose();
            }

            throw;
        }
    }

    public Task StopAsync(CancellationToken token) {
        _cts.Cancel();
        _cts = new();

        if (_lagrange == null) {
            throw new InvalidOperationException("Lagrange is not started.");
        }

        _events.Disaggregate(_lagrange);
        _lagrange.Dispose();

        return Task.CompletedTask;
    }
}