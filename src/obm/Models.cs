using Realms;

namespace OsuBeatmapManager;

public class BeatmapCollection : RealmObject
{
    [PrimaryKey]
    public Guid ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public IList<string> BeatmapMD5Hashes { get; } = null!;
}

public class RealmUser : RealmObject
{
    public int OnlineID { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class BeatmapMetadata : RealmObject
{
    public string Title { get; set; } = string.Empty;
    public string TitleUnicode { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string ArtistUnicode { get; set; } = string.Empty;
    public RealmUser? Author { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
}

[MapTo("BeatmapSet")]
public class BeatmapSetInfo : RealmObject
{
    [PrimaryKey]
    public Guid ID { get; set; }
    public int OnlineID { get; set; }
}

[MapTo("Beatmap")]
public class BeatmapInfo : RealmObject
{
    [PrimaryKey]
    public Guid ID { get; set; }
    [MapTo("BeatmapSet")]
    public BeatmapSetInfo? BeatmapSet { get; set; }
    [MapTo("Metadata")]
    public BeatmapMetadata? Metadata { get; set; }
    public string DifficultyName { get; set; } = string.Empty;
    public double StarRating { get; set; }
    public int OnlineID { get; set; }
    public string MD5Hash { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}

public sealed class CollectionCsvRow
{
    public string Collection { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public double StarRating { get; set; }
    public int BeatmapID { get; set; }
    public int BeatmapSetID { get; set; }
    public string MD5 { get; set; } = string.Empty;
    public string DownloadLink { get; set; } = string.Empty;
}
