using Archipelago.MultiClient.Net;
using static CreepyUtil.Archipelago.ArchipelagoTag;

namespace CreepyUtil.Archipelago;

public class TagManager
{
    private ApClient.ApClient Client;
    private ArchipelagoSession Session;
    private HashSet<ArchipelagoTag> Tags = [];

    public TagManager(ApClient.ApClient client, ArchipelagoSession session, params ArchipelagoTag[]? tags)
    {
        Client = client;
        Session = session;
        if (tags is null || tags.Length == 0) return;
        Tags = tags.ToHashSet();
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

    public string[] GetTagsAsStrings()
        => Tags.SelectMany(tag => tag.StringTag(Client.DeathLinkGroups.ToArray())).ToArray();

    public void SetTags(params ArchipelagoTag[] tags)
    {
        Tags = new HashSet<ArchipelagoTag>(tags);
        UpdateTags();
    }

    public void FetchTags()
    {
        Tags = new HashSet<ArchipelagoTag>(Session.ConnectionInfo.Tags.Select(tag =>
        {
            var apTag = tag.ToArchipelagoTag(out var dl);
            if (dl is not null && apTag is DeathLink) Client.DeathLinkGroups.Add(dl);
            return apTag;
        }));
    }

    public void ToggleDeathLink()
    {
        if (this[DeathLink]) _ = this - DeathLink;
        else _ = this + DeathLink;
    }

    private void UpdateTags() { Session.ConnectionInfo.UpdateConnectionOptions(GetTagsAsStrings()); }

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
    /// Client participates in DeathLink, can send and receive DeathLinks packets
    /// </summary>
    DeathLink,

    /// <summary>
    /// Client participates in TrapLink, can send and receive TrapLinks packets
    /// </summary>
    TrapLink,

    /// <summary>
    /// Client participates in RingLink, can send and receive RingLink packets
    /// </summary>
    RingLink,
}

public static class TagTranslator
{
    private static readonly TwoWayLookup<string, ArchipelagoTag> Translator = new()
    {
        { "AP", AP }, { "NoText", NoText }, { "TextOnly", TextOnly }, { "Tracker", Tracker },
        { "HintGame", HintGame }, { "TrapLink", TrapLink }
    };

    public static ArchipelagoTag ToArchipelagoTag(this string tag, out string? deathLinkGroup)
    {
        if (tag.StartsWith("DeathLink"))
        {
            deathLinkGroup = tag.Substring(9);
            return DeathLink;
        }

        deathLinkGroup = null;
        return Translator.TryGetValue(tag, out var apTag)
            ? apTag
            : throw new ArgumentException($"[{tag}] is not an Archipelago client tag");
    }

    public static string[] StringTag(this ArchipelagoTag tag, params string[] deathLinkGroups)
    {
        return tag is DeathLink ? deathLinkGroups.Select(g => $"DeathLink{g}").ToArray() : ([Translator[tag]]);
    }
}