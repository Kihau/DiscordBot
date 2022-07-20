using Microsoft.Extensions.Logging;
using Shitcord.Extensions;

public static class LoggerTests 
{
    public static void TestSomething()
    {
        var logger = new BotLogger();
        logger.Log(LogLevel.Error, 12, "test", "something");
        logger.Log(LogLevel.Error, 12, "test", "something");
        logger.Log(LogLevel.Error, 12, "test", "something");
        logger.Log(LogLevel.Error, 12, "test", "something");
        logger.Log(LogLevel.Error, 12, "test", "something");
        logger.Log(LogLevel.Error, 11, "test", "something");
    }
}
