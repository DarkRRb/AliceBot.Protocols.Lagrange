using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AliceBot.Core;
using AliceBot.Core.Actions.Results;
using AliceBot.Core.Events.Data;
using AliceBot.Core.Handlers;
using AliceBot.Core.Messages;
using AliceBot.Core.Messages.Segments;
using AliceBot.Core.Protocols;

namespace AliceBot.Protocols.Lagrange.Test;

public class Program {
    public static async Task Main(string[] args) {
        Console.OutputEncoding = Encoding.UTF8;

        Alice alice = new(AliceLogger.Create, "config.jsonc");

        CancellationTokenSource cts = new();
        void handler(object? _, ConsoleCancelEventArgs @event) {
            cts.Cancel();
            @event.Cancel = true;
        }
        Console.CancelKeyPress += handler;

        await alice.RegisterProtocolAsync("Lagrange", LagrangeProtocol.Create, cts.Token);

        await alice.StartAsync(cts.Token);

        await alice.RegisterHandlerAsync("Test", TestHandler.Create, cts.Token);

        Console.CancelKeyPress -= handler;
        TaskCompletionSource tcs = new();
        Console.CancelKeyPress += (_, @event) => {
            tcs.SetResult();
            @event.Cancel = true;
        };
        await tcs.Task;
        Console.CancelKeyPress += handler;

        await alice.StopAsync(cts.Token);
    }
}

public class TestHandler(Alice alice) : IHandler {
    private readonly Alice _alice = alice;

    private CancellationTokenSource _cts = new();

    public static TestHandler Create(Alice alice) {
        return new(alice);
    }

    public static bool CheckOnlyOneText(MessageContent content) {
        return content.Count == 1 && content[0] is TextSegment;
    }

    public static bool CheckMatchTestRich(MessageContent content) {
        if (!CheckOnlyOneText(content)) {
            return false;
        }

        return ((TextSegment)content[0]).Text == "#test rich text";
    }

    public static bool CheckMatchTestForward(MessageContent content) {
        if (!CheckOnlyOneText(content)) {
            return false;
        }

        return ((TextSegment)content[0]).Text == "#test forward";
    }

    private async Task PrivateTestRich(IProtocol protocol, PrivateMessageData data) {
        await Task.Delay(650, _cts.Token);

        SendPrivateMessageResult result = await protocol.Actions.SendPrivateMessageAsync(
            data.Message.UserId,
            new MessageContent.Builder()
                .Reply(data.Message.MessageId)
                .Text("Hi~ I'm Alice!")
                .Emoji("66")
                .Image("https://zhuoshiweilai.oss-cn-hangzhou.aliyuncs.com/server/952bbb50-63be-11ec-8e9f-4dc5a07aa814/default/485cefff-8d04-4ed7-831d-e78984d4ca32/Ph%E7%88%B1%E4%B8%BD%E4%B8%9D.jpg")
                .Build(),
            _cts.Token
        );

        await Task.Delay(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(30)), _cts.Token);

        await protocol.Actions.RecallMessageAsync(result.MessageId, _cts.Token);
    }

    private async Task PrivateTestForward(IProtocol protocol, PrivateMessageData data) {
        SendPrivateMessageResult result = await protocol.Actions.SendPrivateMessageAsync(
            data.Message.UserId,
            new MessageContent.Builder()
                .Forward([
                    new ForwardSegment.ForwardMessage.Builder()
                        .SetUserId(data.Message.UserId)
                        .Build(new MessageContent.Builder().Text("我真可爱!").Build()),
                    new ForwardSegment.ForwardMessage.Builder()
                        .SetTime(DateTimeOffset.Now - TimeSpan.FromHours(2))
                        .SetAvatar("https://zhuoshiweilai.oss-cn-hangzhou.aliyuncs.com/server/952bbb50-63be-11ec-8e9f-4dc5a07aa814/default/485cefff-8d04-4ed7-831d-e78984d4ca32/Ph%E7%88%B1%E4%B8%BD%E4%B8%9D.jpg")
                        .SetName("Alice")
                        .Build(new MessageContent.Builder().Text("我真可爱!").Build())
                ])
                .Build(),
            _cts.Token
        );

        await Task.Delay(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(30)), _cts.Token);

        await protocol.Actions.RecallMessageAsync(result.MessageId, _cts.Token);
    }

    private async void PrivateMessageHandler(IProtocol protocol, PrivateMessageData data) {
        if (CheckMatchTestRich(data.Message.Content)) {
            await PrivateTestRich(protocol, data);
        } else if (CheckMatchTestForward(data.Message.Content)) {
            await PrivateTestForward(protocol, data);
        }
    }

    private async Task GroupTestRich(IProtocol protocol, GroupMessageData data) {
        SendGroupMessageResult result = await protocol.Actions.SendGroupMessageAsync(
            data.Message.GroupId,
            new MessageContent.Builder()
                .Reply(data.Message.MessageId)
                .Text("Hi~ ")
                .At(data.Message.UserId)
                .Text(" I'm Alice!")
                .Emoji("66")
                .Image("https://zhuoshiweilai.oss-cn-hangzhou.aliyuncs.com/server/952bbb50-63be-11ec-8e9f-4dc5a07aa814/default/485cefff-8d04-4ed7-831d-e78984d4ca32/Ph%E7%88%B1%E4%B8%BD%E4%B8%9D.jpg")
                .Build(),
            _cts.Token
        );

        await Task.Delay(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(30)), _cts.Token);

        await protocol.Actions.RecallMessageAsync(result.MessageId, _cts.Token);
    }

    private async Task GroupTestForward(IProtocol protocol, GroupMessageData data) {
        SendGroupMessageResult result = await protocol.Actions.SendGroupMessageAsync(
            data.Message.GroupId,
            new MessageContent.Builder()
                .Forward([
                    new ForwardSegment.ForwardMessage.Builder()
                        .SetUserId(data.Message.UserId)
                        .Build(new MessageContent.Builder().Text("我真可爱!").Build()),
                    new ForwardSegment.ForwardMessage.Builder()
                        .SetTime(DateTimeOffset.Now - TimeSpan.FromHours(2))
                        .SetAvatar("https://zhuoshiweilai.oss-cn-hangzhou.aliyuncs.com/server/952bbb50-63be-11ec-8e9f-4dc5a07aa814/default/485cefff-8d04-4ed7-831d-e78984d4ca32/Ph%E7%88%B1%E4%B8%BD%E4%B8%9D.jpg")
                        .SetName("Alice")
                        .Build(new MessageContent.Builder().Text("我真可爱!").Build())
                ])
                .Build(),
            _cts.Token
        );

        await Task.Delay(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(30)), _cts.Token);

        await protocol.Actions.RecallMessageAsync(result.MessageId, _cts.Token);
    }

    private async void GroupMessageHandler(IProtocol protocol, GroupMessageData data) {
        if (CheckMatchTestRich(data.Message.Content)) {
            await GroupTestRich(protocol, data);
        } else if (CheckMatchTestForward(data.Message.Content)) {
            await GroupTestForward(protocol, data);
        }
    }

    public Task StartAsync(CancellationToken token) {
        _alice.Events.OnPrivateMessage += PrivateMessageHandler;
        _alice.Events.OnGroupMessage += GroupMessageHandler;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken token) {
        _cts.Cancel();
        _cts = new();

        _alice.Events.OnPrivateMessage -= PrivateMessageHandler;
        _alice.Events.OnGroupMessage -= GroupMessageHandler;

        return Task.CompletedTask;
    }
}