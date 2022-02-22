using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Shitcord.Extensions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAuthorizedAttribute : CheckBaseAttribute
{
	private readonly List<ulong> _authorizedUsers;
	public RequireAuthorizedAttribute()
	{
		_authorizedUsers = new List<ulong>
		{
			278778540554715137,
			790507097615237120,
			489788192145539072,
		};
	}
	
	public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
	{
		if (help) return Task.FromResult(true);
		
		if (_authorizedUsers.Contains(ctx.User.Id) || ctx.User.Id == ctx.Client.CurrentUser.Id)
			return Task.FromResult(true);
		
		throw new CommandException("You are not authorized to use this command");
		
		//if (_authorizedUsers.Contains(ctx.User.Id) || ctx.User.Id == ctx.Client.CurrentUser.Id)
		// 	return Task.FromResult(true);
	}
}