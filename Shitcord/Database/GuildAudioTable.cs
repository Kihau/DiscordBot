namespace Shitcord.Database;

public class GuildAudioTable
{
    public const string TABLE_NAME = "guild_audio_data";
    
    public static readonly Column GUILD_ID = new("str", "bigint", false);
    public static readonly Column QU_CHANNEL = new("qu_channel_id", "bigint");
    public static readonly Column SU_CHANNEL = new("su_channel_id", "bigint");
    public static readonly Column QU_MSG = new("qu_msg_id", "bigint");
    public static readonly Column SU_MSG = new("su_msg_id", "bigint");
    public static readonly Column VOLUME = new("volume", "int", false);
    public static readonly Column LOOPING = new("int", "boolean", false);
    
    public readonly List<Column> columns = new(){GUILD_ID, QU_CHANNEL, SU_CHANNEL, QU_MSG, SU_MSG, VOLUME, LOOPING};
    
    public static Column? PRIMARY_KEY = GUILD_ID;
}