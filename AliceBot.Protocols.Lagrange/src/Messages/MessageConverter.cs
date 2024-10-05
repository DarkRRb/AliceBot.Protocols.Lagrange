using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AliceBot.Core.Messages;
using AliceBot.Core.Messages.Segments;
using AliceBot.Protocols.Lagrange.Utilities;
using Lagrange.Core;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;

namespace AliceBot.Protocols.Lagrange.Messages;

public static class MessageConverter {
    public static PrivateMessage ToPrivateMessage(this MessageChain chain) {
        return new PrivateMessage(
            MessageIdUtility.GeneratePrivateMessageId(chain.FriendUin, chain.Sequence),
            chain.Time,
            chain.FriendUin.ToString(),
            chain.ToMessageContent()
        );
    }

    public static GroupMessage ToGroupMessage(this MessageChain chain) {
        uint groupId = chain.GroupUin ?? throw new Exception("GroupUin is null.");
        return new GroupMessage(
            MessageIdUtility.GenerateGroupMessageId(groupId, chain.Sequence),
            chain.Time,
            groupId.ToString(),
            chain.FriendUin.ToString(),
            chain.ToMessageContent()
        );
    }

    public static ForwardSegment.ForwardMessage ToForwardMessage(this MessageChain chain) {
        return new ForwardSegment.ForwardMessage.Builder()
            .SetTime(chain.Time)
            .SetAvatar(chain.FriendInfo?.Avatar ?? "")
            .SetUserId(chain.FriendUin.ToString())
            .Build(chain.ToMessageContent());
    }

    public static MessageContent ToMessageContent(this MessageChain chain) {
        return chain.Aggregate(
            new MessageContent.Builder(),
            (builder, entity) => builder.AddEntity(chain, entity),
            builder => builder.Build()
        );
    }

    public static MessageContent.Builder AddEntity(this MessageContent.Builder builder, MessageChain chain, IMessageEntity entity) {
        return entity switch {
            FaceEntity face => builder.Emoji(face.FaceId.ToString()),
            ForwardEntity forward => builder.Reply(
                chain.Type switch {
                    MessageChain.MessageType.Group => MessageIdUtility.GenerateGroupMessageId(
                        chain.GroupUin ?? throw new Exception("GroupUin is null."),
                        forward.Sequence
                    ),
                    MessageChain.MessageType.Friend => MessageIdUtility.GeneratePrivateMessageId(
                        chain.FriendUin,
                        forward.Sequence
                    ),
                    _ => throw new NotSupportedException($"{chain.Type} message type."),
                }
            ),
            ImageEntity image => builder.Image(image.ImageUrl),
            MentionEntity mention => builder.At(mention.Uin.ToString()),
            MultiMsgEntity multi => builder.Forward(multi.Chains.Select((chain) => chain.ToForwardMessage()).ToList()),
            TextEntity text => builder.Text(text.Text),
            _ => builder
        };
    }

    public static async Task<MessageChain> ToFriendChainAsync(this MessageContent content, BotContext lagrange, string userId, CancellationToken token) {
        MessageBuilder builder = MessageBuilder.Friend(uint.Parse(userId));
        foreach (var segment in content) {
            await builder.AddSegmentAsync(lagrange, segment, token);
        }
        return builder.Build();
    }

    public static async Task<MessageChain> ToGroupChainAsync(this MessageContent content, BotContext lagrange, string groupId, CancellationToken token) {
        MessageBuilder builder = MessageBuilder.Group(uint.Parse(groupId));
        foreach (var segment in content) {
            await builder.AddSegmentAsync(lagrange, segment, token);
        }
        return builder.Build();
    }

    public static async Task<MessageBuilder> AddSegmentAsync(this MessageBuilder builder, BotContext lagrange, ISegment segment, CancellationToken token) {
        return segment switch {
            AtSegment at => builder.Mention(uint.Parse(at.UserId)),
            EmojiSegment emoji => builder.Face(ushort.Parse(emoji.EmojiId)),
            ForwardSegment forward => await builder.AddForwardSegmentAsync(lagrange, forward, token),
            ImageSegment image => builder.Image(await BytesUtility.FromUrlAsync(image.Url, token)),
            ReplySegment reply => await builder.AddReplySegmentAsync(lagrange, reply, token),
            TextSegment text => builder.Text(text.Text),
            _ => builder
        };
    }

    public static async Task<MessageBuilder> AddForwardSegmentAsync(this MessageBuilder builder, BotContext lagrange, ForwardSegment forward, CancellationToken token) {
        MessageChain[] chains = new MessageChain[forward.Messages.Count];
        for (int i = 0; i < chains.Length; i++) {
            MessageBuilder iBuilder = MessageBuilder
                .Friend(!string.IsNullOrEmpty(forward.Messages[i].UserId) ? uint.Parse(forward.Messages[i].UserId) : 0)
                .Time(forward.Messages[i].Time.LocalDateTime)
                .FriendAvatar(forward.Messages[i].Avatar)
                .FriendName(forward.Messages[i].Name);
            foreach (var segment in forward.Messages[i].Content) {
                await iBuilder.AddSegmentAsync(lagrange, segment, token);
            }
            chains[i] = iBuilder.Build();
        }
        return builder.MultiMsg(chains);
    }

    public static async Task<MessageBuilder> AddReplySegmentAsync(this MessageBuilder builder, BotContext lagrange, ReplySegment reply, CancellationToken token) {
        MessageIdUtility.CheckIsSelf(reply.MessageId);

        MessageChain chain = await lagrange.GetMessageByMessageId(reply.MessageId, token);
        return builder.Forward(chain);
    }
}