using Shitcord.Services.Database;
namespace Shitcord.Services.Queries;

public class QueryBuilder
{
    public SelectQuery Retrieve(string colName)
    {
        return new SelectQuery(colName);
    }
    public DeleteQuery Delete()
    {
        return new DeleteQuery();
    }
    public InsertQuery Insert()
    {
        return new InsertQuery();
    }
    public UpdateQuery Update(string tableName)
    {
        return new UpdateQuery(tableName);
    }

    public static QueryBuilder New()
    {
        return new QueryBuilder();
    }
}