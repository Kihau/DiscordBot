namespace Shitcord.Database.Queries;

public class QueryException : Exception
{
    public QueryException(string? message) : base(message) { }
}