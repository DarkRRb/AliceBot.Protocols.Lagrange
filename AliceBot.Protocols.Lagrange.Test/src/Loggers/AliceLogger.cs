using System;
using AliceBot.Core.Loggers;

namespace AliceBot.Protocols.Lagrange.Test;

public class AliceLogger(string tag) : ILogger {
    private readonly string _tag = tag;

    public static AliceLogger Create(string tag) {
        return new(tag);
    }

    public void Log(ILogger.Level level, string message) {
        Console.WriteLine($"[{level}] [{_tag}] {message}");
    }
}