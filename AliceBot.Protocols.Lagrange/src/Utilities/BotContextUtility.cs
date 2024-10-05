using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace AliceBot.Protocols.Lagrange.Utilities;

public static class BotContextUtility {
    public static Task<bool> LoginByPasswordAsync(this BotContext _lagrange, CancellationToken token) {
        return _lagrange.LoginByPassword().WaitAsync(token);
    }

    public static async Task<string> FetchQrCodeAsync(this BotContext _lagrange, CancellationToken token) {
        (string url, _) = await _lagrange.FetchQrCode().WaitAsync(token)
            ?? throw new InvalidOperationException("Failed to fetch the QR code.");

        return url;
    }

    public static Task LoginByQrCodeAsync(this BotContext _lagrange, CancellationToken token) {
        return _lagrange.LoginByQrCode().WaitAsync(token);
    }

    public static async Task<MessageChain> GetMessageByMessageId(this BotContext lagrange, string messageId, CancellationToken token) {
        uint uin = uint.Parse(MessageIdUtility.GetUin(messageId));
        uint sequence = uint.Parse(MessageIdUtility.GetSequence(messageId));

        List<MessageChain>? chains;
        if (MessageIdUtility.IsGroup(messageId)) {
            chains = await lagrange.GetGroupMessage(uin, sequence, sequence).WaitAsync(token);
        } else if (MessageIdUtility.IsPrivate(messageId)) {
            chains = await lagrange.GetC2cMessage(uin, sequence, sequence).WaitAsync(token);
        } else {
            throw new InvalidOperationException("Invalid message ID.");
        }

        if (chains == null || chains.Count == 0) {
            throw new InvalidOperationException("Failed to get the message.");
        }

        return chains[0];
    }

    public static Task<MessageResult> SendMessageAsync(this BotContext lagrange, MessageChain chain, CancellationToken token) {
        return lagrange.SendMessage(chain).WaitAsync(token);
    }
}