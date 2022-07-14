namespace Shitcord.Extensions;

public class CommandException : Exception
{
    public CommandException(string? message) : base(message) { }
}