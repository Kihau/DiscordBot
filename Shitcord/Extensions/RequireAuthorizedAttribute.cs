using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Shitcord.Database;
using Shitcord.Database.Queries;
using Shitcord.Services;

namespace Shitcord.Extensions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAuthorizedAttribute : CheckBaseAttribute
{
    public RequireAuthorizedAttribute() { }
    
    public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        if (help) return Task.FromResult(true);

        if (ctx.User.Id == ctx.Client.CurrentUser.Id)
            return Task.FromResult(true);

        var service = ctx.Services.GetService(typeof(DatabaseService));
        if (service is null) 
            throw new CommandException("Could not retrive service");

        var database = service as DatabaseService;
        if (database is null) 
            throw new CommandException("Could not get database service");
        
        var user_exists = database.ExistsInTable(AuthUsersTable.TABLE_NAME, 
            Condition.New(AuthUsersTable.USER_ID).Equals(ctx.User.Id)
        );

        if (user_exists) return Task.FromResult(true);
        
        throw new CommandException("You are not authorized to use this command");
    }
}
