using Archipelago.MultiClient.Net;
using static CreepyUtil.Archipelago.ArchipelagoTag;

namespace CreepyUtil.Archipelago;

public class TagManager
{
    private ArchipelagoSession Session;
    private HashSet<ArchipelagoTag> Tags;

    public TagManager(ArchipelagoSession session)
    {
        Session = session;
        FetchTags();
    }

    public static TagManager operator +(TagManager manager, ArchipelagoTag tag)
    {
        if (!manager.Tags.Add(tag)) return manager;
        manager.UpdateTags();
        return manager;
    }

    public static TagManager operator -(TagManager manager, ArchipelagoTag tag)
    {
        if (!manager.Tags.Contains(tag)) return manager;
        manager.Tags.Remove(tag);
        manager.UpdateTags();
        return manager;
    }

    public ArchipelagoTag[] GetTags() => Tags.ToArray();

    public void FetchTags()
    {
        Tags = new HashSet<ArchipelagoTag>(Session.ConnectionInfo.Tags.Select(tag => tag.ToArchipelagoTag()));
    }
    
    public void ToggleDeathLink()
    {
        if (this[DeathLink])
        {
            _ = this - DeathLink;
        }
        else
        {
            _ = this + DeathLink;
        }
    }
    
    private void UpdateTags()
    {
        Session.ConnectionInfo.UpdateConnectionOptions(Tags.Select(tag => tag.StringTag()).ToArray());
    }

    public bool this[ArchipelagoTag tag] => Tags.Contains(tag);
}

public enum ArchipelagoTag
{
    /// <summary>
    /// Mostly for debugging and comparing client behaviors
    /// </summary>
    AP,

    /// <summary>
    /// Client does NOT want chat messages
    /// </summary>
    NoText,

    /// <summary>
    /// Client is only for chat and basic messaging [game is optional]
    /// </summary>
    TextOnly,

    /// <summary>
    /// Client will track instead of sending locations [game is optional]
    /// </summary>
    Tracker,

    /// <summary>
    /// Client will send hints instead of locations [game is optional]
    /// </summary>
    HintGame,

    /// <summary>
    /// Client participates in DeathLink, can send and receive DeathLinks
    /// </summary>
    DeathLink,
}

public static class TagTranslator
{
    public static readonly TwoWayLookup<string, ArchipelagoTag> Translator = new()
    {
        { "AP", AP },
        { "NoText", NoText },
        { "TextOnly", TextOnly },
        { "Tracker", Tracker },
        { "HintGame", HintGame },
        { "DeathLink", DeathLink }
    };

    public static ArchipelagoTag ToArchipelagoTag(this string tag)
        => Translator.TryGetValue(tag, out var apTag)
            ? apTag
            : throw new ArgumentException($"[{tag}] is not an Archipelago client tag");

    public static string StringTag(this ArchipelagoTag tag) => Translator[tag];
}