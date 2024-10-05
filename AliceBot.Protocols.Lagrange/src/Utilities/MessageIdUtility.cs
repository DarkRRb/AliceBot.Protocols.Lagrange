using System;

namespace AliceBot.Protocols.Lagrange.Utilities;

public class MessageIdUtility {
    public static string GenerateGroupMessageId(uint groupId, uint sequence) {
        return $"AliceBot.Protocols.Lagrange:g:{groupId}:{sequence}";
    }

    public static string GenerateGroupMessageId(string groupId, uint sequence) {
        return $"AliceBot.Protocols.Lagrange:g:{groupId}:{sequence}";
    }

    public static string GeneratePrivateMessageId(uint userId, uint sequence) {
        return $"AliceBot.Protocols.Lagrange:p:{userId}:{sequence}";
    }

    public static string GeneratePrivateMessageId(string userId, uint sequence) {
        return $"AliceBot.Protocols.Lagrange:p:{userId}:{sequence}";
    }

    public static void CheckIsSelf(string messageId) {
        if (!messageId.StartsWith("AliceBot.Protocols.Lagrange:")) {
            throw new InvalidOperationException("The message is not from AliceBot.Protocols.Lagrange.");
        }
    }

    public static bool IsGroup(string messageId) {
        return messageId.StartsWith("AliceBot.Protocols.Lagrange:g:");
    }

    public static bool IsPrivate(string messageId) {
        return messageId.StartsWith("AliceBot.Protocols.Lagrange:p:");
    }

    public static string GetUin(string messageId) {
        return messageId.Split(':')[2];
    }

    public static string GetSequence(string messageId) {
        return messageId.Split(':')[3];
    }
}