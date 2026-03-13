using System.Text.Json.Serialization;

namespace WarcraftPulseUploader.Parser;

/// <summary>
/// Wire format sent to POST /api/reports/upload-parsed.
/// Property names match the snake_case keys the server expects.
/// Dictionary keys are strings because JSON object keys are always strings
/// (even when the PHP side uses integer fight IDs).
/// </summary>
public sealed class CombatLogData
{
    [JsonPropertyName("game_version")]       public string GameVersion { get; set; } = "";
    [JsonPropertyName("zone_id")]            public int ZoneId { get; set; }
    [JsonPropertyName("zone_name")]          public string ZoneName { get; set; } = "";
    [JsonPropertyName("start_time")]         public string StartTime { get; set; } = "";
    [JsonPropertyName("end_time")]           public string EndTime { get; set; } = "";
    [JsonPropertyName("fights")]             public List<FightData> Fights { get; set; } = [];
    [JsonPropertyName("players")]            public PlayerDetails Players { get; set; } = new();
    [JsonPropertyName("damage_done")]        public Dictionary<string, EntriesWrapper> DamageDone { get; set; } = [];
    [JsonPropertyName("damage_taken")]       public Dictionary<string, EntriesWrapper> DamageTaken { get; set; } = [];
    [JsonPropertyName("healing")]            public Dictionary<string, EntriesWrapper> Healing { get; set; } = [];
    [JsonPropertyName("deaths")]             public Dictionary<string, List<DeathEvent>> Deaths { get; set; } = [];
    [JsonPropertyName("buffs")]              public Dictionary<string, AurasWrapper> Buffs { get; set; } = [];
    [JsonPropertyName("debuffs")]            public Dictionary<string, AurasWrapper> Debuffs { get; set; } = [];
    [JsonPropertyName("combatant_info")]     public Dictionary<string, List<CombatantInfoEvent>> CombatantInfo { get; set; } = [];
    [JsonPropertyName("events")]             public Dictionary<string, Dictionary<string, List<RawEvent>>> Events { get; set; } = [];
    [JsonPropertyName("report_wide_events")] public Dictionary<string, List<RawEvent>> ReportWideEvents { get; set; } = [];
}

public sealed class FightData
{
    [JsonPropertyName("id")]           public int Id { get; set; }
    [JsonPropertyName("name")]         public string Name { get; set; } = "";
    [JsonPropertyName("encounter_id")] public int EncounterId { get; set; }
    [JsonPropertyName("start_time")]   public long StartTime { get; set; }
    [JsonPropertyName("end_time")]     public long EndTime { get; set; }
    [JsonPropertyName("kill")]         public bool Kill { get; set; }
    [JsonPropertyName("difficulty")]   public int Difficulty { get; set; }
}

public sealed class PlayerDetails
{
    [JsonPropertyName("dps")]     public List<PlayerEntry> Dps { get; set; } = [];
    [JsonPropertyName("healers")] public List<PlayerEntry> Healers { get; set; } = [];
    [JsonPropertyName("tanks")]   public List<PlayerEntry> Tanks { get; set; } = [];
}

public sealed class PlayerEntry
{
    [JsonPropertyName("id")]     public int Id { get; set; }
    [JsonPropertyName("guid")]   public string? Guid { get; set; }
    [JsonPropertyName("name")]   public string Name { get; set; } = "";
    [JsonPropertyName("server")] public string Server { get; set; } = "";
    [JsonPropertyName("type")]   public string? Type { get; set; }
    [JsonPropertyName("specs")]  public List<SpecEntry> Specs { get; set; } = [];
}

public sealed class SpecEntry
{
    [JsonPropertyName("spec")] public string? Spec { get; set; }
}

public sealed class EntriesWrapper
{
    [JsonPropertyName("entries")] public List<Dictionary<string, object>> Entries { get; set; } = [];
}

public sealed class AurasWrapper
{
    [JsonPropertyName("auras")] public List<AuraData> Auras { get; set; } = [];
}

public sealed class AuraData
{
    [JsonPropertyName("guid")]        public int Guid { get; set; }
    [JsonPropertyName("name")]        public string Name { get; set; } = "";
    [JsonPropertyName("totalUptime")] public long TotalUptime { get; set; }
    [JsonPropertyName("bands")]       public List<AuraBand> Bands { get; set; } = [];
}

public sealed class AuraBand
{
    [JsonPropertyName("startTime")] public long StartTime { get; set; }
    [JsonPropertyName("endTime")]   public long EndTime { get; set; }
}

public sealed class DeathEvent
{
    [JsonPropertyName("name")]      public string Name { get; set; } = "";
    [JsonPropertyName("timestamp")] public long Timestamp { get; set; }
}

public sealed class CombatantInfoEvent
{
    [JsonPropertyName("sourceId")]   public int SourceId { get; set; }
    [JsonPropertyName("talentTree")] public int[] TalentTree { get; set; } = [];
}

public sealed class RawEvent
{
    [JsonPropertyName("sourceId")]  public int SourceId { get; set; }
    [JsonPropertyName("targetId")]  public int TargetId { get; set; }
    [JsonPropertyName("spellId")]   public int SpellId { get; set; }
    [JsonPropertyName("spellName")] public string SpellName { get; set; } = "";
    [JsonPropertyName("timestamp")] public long Timestamp { get; set; }
}
