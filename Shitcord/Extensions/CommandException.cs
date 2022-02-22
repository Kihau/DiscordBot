using System;

namespace Shitcord.Extensions;

public class CommandException : Exception
{
    public CommandException(string? messge) : base(messge) { }
}