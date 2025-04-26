using DiscordRPC;

namespace CreepyUtil.DiscordRpc;

public class DiscordIntegration
{
    public static string DiscordAppId = string.Empty;
    public static DiscordRpcClient Discord;
    public static bool DiscordAlive;
    public static DateTime Now;
    public static Func<string>? Details;
    public static Func<string>? State;
    public static Func<string>? LargeImage;
    public static Func<string>? LargeText;
    public static Func<string>? SmallImage;
    public static Func<string>? SmallText;
    public static Action<string>? LogOut;
    
    private static bool InitDiscord;
    
    public static void Init(string discordAppId)
    {
        if (InitDiscord) return;
        InitDiscord = true;
        DiscordAppId = discordAppId;
        if (discordAppId != string.Empty) CheckDiscord(discordAppId);
        Now = DateTime.UtcNow;
    }

    public static void CheckDiscord(string appId, bool retry = true)
    {
        if (appId == string.Empty) return;
        DiscordAlive = true;
        try
        {
            Discord.UpdateStartTime(Now);
        }
        catch
        {
            // look man, idk 
        }

        try
        {
            Discord = new DiscordRpcClient(appId, autoEvents: false)
            {
                SkipIdenticalPresence = true
            };
            Discord.Initialize();

            RichPresence rp = new();
            var assets = rp.Assets = new Assets();
            if (Details is not null) rp.Details = Details();
            if (State is not null) rp.State = State();
            if (LargeImage is not null) assets.LargeImageKey = LargeImage();
            if (LargeText is not null) assets.LargeImageText = LargeText();
            if (SmallImage is not null) assets.SmallImageKey = SmallImage();
            if (SmallText is not null) assets.SmallImageText = SmallText();

            Discord.SetPresence(rp);
            UpdateActivity();
        }
        catch
        {
            DiscordAlive = false;
            LogOut?.Invoke("F");

            if (retry) // retry if fluke 
            {
                LogOut?.Invoke("retrying");
                CheckDiscord(appId, false);
            } else LogOut?.Invoke("FFFFFFFFF");
        }
    }
    
    public static void UpdateActivity()
    {
        try
        {
            if (!DiscordAlive) return;
            if (Details is not null) Discord.UpdateDetails(Details());
            if (State is not null) Discord.UpdateState(State());
            if (LargeImage is not null && LargeText is not null) Discord.UpdateLargeAsset(LargeImage(), LargeText());
            if (SmallImage is not null && SmallText is not null) Discord.UpdateSmallAsset(SmallImage(), SmallText());
            Discord.Invoke();
        }
        catch (Exception e)
        {
            DiscordAlive = false;
            LogOut?.Invoke($"Error: {e.Message}\n{e.StackTrace}");
        }
    }
}