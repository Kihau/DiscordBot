using System;

namespace Shitcord.Modules;

public class CommandException : Exception
{
    public CommandException(string? messge) : base(messge) { }
}