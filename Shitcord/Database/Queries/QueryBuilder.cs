namespace Shitcord.Database.Queries;

public class QueryBuilder
{
    public SelectQuery Retrieve(params string[] colName)
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
