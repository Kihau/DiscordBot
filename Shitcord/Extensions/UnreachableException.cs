namespace Shitcord.Extensions;

public class UnreachableException : Exception
{
    public UnreachableException ( string
        ? message = null        ) 
        : base                  ( message is null
        ? "Unreachable code detected." 
        : "Unreachable code detected: "
        + message 
        ) { 
    }
}

