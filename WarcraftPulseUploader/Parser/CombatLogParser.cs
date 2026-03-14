namespace WarcraftPulseUploader.Parser;

public static class CombatLogParser
{
    private static readonly Dictionary<string, string> ProjectIdMap = new()
    {
        { "2", "classic_era" },
        { "5", "season_of_discovery" },
    };

    private const uint PlayerFlag = 0x00000400;

    public static CombatLogData Parse(string filePath)
    {
        using var reader = new StreamReader(filePath, System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true, bufferSize: 65536);
        return ParseCore(reader);
    }

    public static (CombatLogData data, string hash) ParseWithHash(string filePath)
    {
        using var fileStream   = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                                     FileShare.ReadWrite, bufferSize: 65536);
        using var sha          = System.Security.Cryptography.SHA256.Create();
        using var cryptoStream = new System.Security.Cryptography.CryptoStream(
                                     fileStream, sha,
                                     System.Security.Cryptography.CryptoStreamMode.Read,
                                     leaveOpen: false);
        using var reader       = new StreamReader(cryptoStream, System.Text.Encoding.UTF8,
                                     detectEncodingFromByteOrderMarks: true, bufferSize: 65536);

        var data = ParseCore(reader);

        // Ensure all bytes have flowed through the CryptoStream.
        // ReadLine() reads to EOF so the buffer is normally already empty,
        // but this is a defensive safeguard.
        cryptoStream.CopyTo(Stream.Null);

        string hash = Convert.ToHexString(sha.Hash!);
        return (data, hash);
    }

    private static CombatLogData ParseCore(StreamReader reader)
    {
        var header = reader.ReadLine()
            ?? throw new ParseException("File is empty.");

        if (!header.Contains("COMBAT_LOG_VERSION"))
            throw new ParseException("No COMBAT_LOG_VERSION line found.");

        string gameVersion = ExtractGameVersion(header);

        var actorMap    = new Dictionary<string, int>();
        int nextActorId = 1;

        DateTime? reportStart = null;
        DateTime? reportEnd   = null;
        int       zoneId      = 0;
        string    zoneName    = "";

        var fights       = new List<FightData>();
        int fightSeq     = 0;
        int? openFightId       = null;
        string openFightName   = "";
        int openEncounterId    = 0;
        int openDifficulty     = 0;
        DateTime openFightStart = default;

        var damageDone     = new Dictionary<int, Dictionary<int, long>>();
        var damageTaken    = new Dictionary<int, Dictionary<int, long>>();
        var healingDone    = new Dictionary<int, Dictionary<int, long>>();
        var deaths         = new Dictionary<int, List<DeathEvent>>();
        var buffs          = new Dictionary<int, Dictionary<int, AuraTracker>>();
        var debuffs        = new Dictionary<int, Dictionary<int, AuraTracker>>();
        var castEvents     = new Dictionary<int, List<RawEvent>>();
        var interrupts     = new Dictionary<int, List<RawEvent>>();
        var dispels        = new Dictionary<int, List<RawEvent>>();
        var combatantInfos = new Dictionary<int, List<CombatantInfoEvent>>();

        var playerMeta = new Dictionary<int, PlayerMeta>();
        var firstSpell = new Dictionary<int, int>();

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            int sep = line.IndexOf("  ");
            if (sep < 0) continue;

            string timestampStr = line[..sep];
            string rest         = line[(sep + 2)..];

            if (!TryParseTimestamp(timestampStr, out DateTime ts)) continue;

            reportStart ??= ts;
            reportEnd     = ts;

            var fields = SplitCsvLine(rest.AsSpan());
            if (fields.Length == 0) continue;
            string eventType = fields[0];

            switch (eventType)
            {
                case "ZONE_CHANGE":
                    if (fields.Length >= 3)
                    {
                        zoneId   = int.TryParse(fields[1], out int zid) ? zid : zoneId;
                        zoneName = fields[2];
                    }
                    break;

                case "ENCOUNTER_START":
                    if (fields.Length >= 5)
                    {
                        fightSeq++;
                        openFightId      = fightSeq;
                        openEncounterId  = int.TryParse(fields[1], out int eid) ? eid : 0;
                        openFightName    = fields[2];
                        openDifficulty   = int.TryParse(fields[3], out int diff) ? diff : 0;
                        openFightStart   = ts;

                        damageDone [fightSeq] = new();
                        damageTaken[fightSeq] = new();
                        healingDone[fightSeq] = new();
                        deaths     [fightSeq] = new();
                        buffs      [fightSeq] = new();
                        debuffs    [fightSeq] = new();
                        castEvents [fightSeq] = new();
                        interrupts [fightSeq] = new();
                        dispels    [fightSeq] = new();
                        combatantInfos[fightSeq] = new();
                    }
                    break;

                case "ENCOUNTER_END":
                    if (openFightId is int fightId && fields.Length >= 6)
                    {
                        bool kill = fields[5].Trim() == "1";
                        long startMs = (long)(openFightStart - reportStart!.Value).TotalMilliseconds;
                        long endMs   = (long)(ts - reportStart.Value).TotalMilliseconds;

                        fights.Add(new FightData
                        {
                            Id          = fightId,
                            Name        = openFightName,
                            EncounterId = openEncounterId,
                            Difficulty  = openDifficulty,
                            StartTime   = startMs,
                            EndTime     = endMs,
                            Kill        = kill,
                        });
                        openFightId = null;
                    }
                    break;

                case "COMBATANT_INFO":
                    if (openFightId is int cfFightId && fields.Length >= 3)
                    {
                        string guid    = fields[1];
                        int    actorId = GetOrAdd(actorMap, guid, ref nextActorId);
                        if (!playerMeta.ContainsKey(actorId))
                        {
                            string pName = fields[2];
                            var parts = pName.Split('-', 2);
                            playerMeta[actorId] = new PlayerMeta { Name = parts[0], Realm = parts.Length > 1 ? parts[1] : "" };
                        }
                        int t1 = fields.Length > 24 ? ParseInt(fields[24]) : 0;
                        int t2 = fields.Length > 25 ? ParseInt(fields[25]) : 0;
                        int t3 = fields.Length > 26 ? ParseInt(fields[26]) : 0;

                        DetectClassFromTalents(actorId, t1, t2, t3, playerMeta);

                        combatantInfos[cfFightId].Add(new CombatantInfoEvent
                        {
                            SourceId   = actorId,
                            TalentTree = [t1, t2, t3],
                        });
                    }
                    break;

                default:
                    if (openFightId is int activeFight)
                        ProcessEvent(eventType, fields, activeFight, ts, reportStart!.Value,
                            actorMap, ref nextActorId, playerMeta, firstSpell,
                            damageDone, damageTaken, healingDone,
                            deaths, buffs, debuffs, castEvents, interrupts, dispels);
                    break;
            }
        }

        if (fights.Count == 0)
            throw new ParseException("No encounters found in log.");

        ResolveClassFromSpells(playerMeta, firstSpell);

        return BuildResult(gameVersion, zoneId, zoneName,
            reportStart!.Value, reportEnd!.Value, fights, playerMeta,
            damageDone, damageTaken, healingDone, deaths,
            buffs, debuffs, combatantInfos, castEvents, interrupts, dispels);
    }

    public static CombatLogData ParseWithSizeGuard(string filePath, long maxBytes = ParseLimits.MaxFileSizeBytes)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists)
            throw new ParseException($"File not found: {filePath}");
        if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            throw new ParseException("Symlinks and junctions are not supported as combat log paths.");
        if (info.Length > maxBytes)
            throw new ParseException($"Combat log is too large to process ({info.Length / 1024 / 1024} MB). Maximum allowed: {maxBytes / 1024 / 1024} MB.");
        return Parse(filePath);
    }

    private static void ProcessEvent(
        string eventType, string[] fields, int fightId,
        DateTime ts, DateTime reportStart,
        Dictionary<string, int> actorMap, ref int nextActorId,
        Dictionary<int, PlayerMeta> playerMeta,
        Dictionary<int, int> firstSpell,
        Dictionary<int, Dictionary<int, long>> damageDone,
        Dictionary<int, Dictionary<int, long>> damageTaken,
        Dictionary<int, Dictionary<int, long>> healingDone,
        Dictionary<int, List<DeathEvent>> deaths,
        Dictionary<int, Dictionary<int, AuraTracker>> buffs,
        Dictionary<int, Dictionary<int, AuraTracker>> debuffs,
        Dictionary<int, List<RawEvent>> casts,
        Dictionary<int, List<RawEvent>> interrupts,
        Dictionary<int, List<RawEvent>> dispels)
    {
        if (fields.Length < 9) return;

        string srcGuid    = fields[1];
        string srcName    = fields[2];
        uint   srcFlags   = ParseHex(fields[3]);
        string tgtGuid    = fields[5];
        string tgtName    = fields[6];
        uint   tgtFlags   = ParseHex(fields[7]);
        long   tsMs       = (long)(ts - reportStart).TotalMilliseconds;

        bool srcIsPlayer = (srcFlags & PlayerFlag) != 0;
        bool tgtIsPlayer = (tgtFlags & PlayerFlag) != 0;

        int srcId = srcIsPlayer ? GetOrAdd(actorMap, srcGuid, ref nextActorId) : -1;
        int tgtId = tgtIsPlayer ? GetOrAdd(actorMap, tgtGuid, ref nextActorId) : -1;

        if (srcIsPlayer && srcId > 0 && !playerMeta.ContainsKey(srcId))
        {
            var parts = srcName.Split('-', 2);
            playerMeta[srcId] = new PlayerMeta { Name = parts[0], Realm = parts.Length > 1 ? parts[1] : "" };
        }

        switch (eventType)
        {
            case "SPELL_DAMAGE":
            case "SWING_DAMAGE":
            case "RANGE_DAMAGE":
                long dmgAmount = eventType == "SWING_DAMAGE"
                    ? (fields.Length > 9 ? ParseLong(fields[9]) : 0)
                    : (fields.Length > 12 ? ParseLong(fields[12]) : 0);

                if (srcIsPlayer && srcId > 0)
                    AddTo(damageDone[fightId], srcId, dmgAmount);

                if (tgtIsPlayer && tgtId > 0)
                    AddTo(damageTaken[fightId], tgtId, dmgAmount);
                break;

            case "SPELL_HEAL":
            case "SPELL_PERIODIC_HEAL":
                if (srcIsPlayer && srcId > 0 && fields.Length > 12)
                    AddTo(healingDone[fightId], srcId, ParseLong(fields[12]));
                break;

            case "UNIT_DIED":
                if (tgtIsPlayer && tgtId > 0 && deaths[fightId].Count < ParseLimits.MaxDeathsPerFight)
                    deaths[fightId].Add(new DeathEvent { Name = tgtName.Split('-')[0], Timestamp = tsMs });
                break;

            case "SPELL_AURA_APPLIED":
            case "SPELL_AURA_REMOVED":
            {
                if (fields.Length < 13) break;
                int    spellId   = ParseInt(fields[9]);
                string spellName = fields[10];
                bool   isBuff    = fields[12].Trim() == "BUFF";

                var store = isBuff ? buffs[fightId] : debuffs[fightId];

                if (eventType == "SPELL_AURA_APPLIED")
                {
                    if (!store.TryGetValue(spellId, out var tracker))
                    {
                        tracker = new AuraTracker { SpellId = spellId, SpellName = spellName };
                        store[spellId] = tracker;
                    }
                    if (tracker.BandCount < ParseLimits.MaxAuraBands)
                        tracker.OpenBand(tsMs);
                }
                else
                {
                    if (store.TryGetValue(spellId, out var tracker))
                        tracker.CloseBand(tsMs);
                }
                break;
            }

            case "SPELL_CAST_SUCCESS":
                if (srcIsPlayer && srcId > 0 && fields.Length > 9)
                {
                    int spellId = ParseInt(fields[9]);
                    // Always track first spell for class detection, even after the cast list is full.
                    firstSpell.TryAdd(srcId, spellId);
                    if (casts[fightId].Count < ParseLimits.MaxCastsPerFight)
                        casts[fightId].Add(new RawEvent { SourceId = srcId, SpellId = spellId, SpellName = fields[10], Timestamp = tsMs });
                }
                break;

            case "SPELL_INTERRUPT":
                if (srcIsPlayer && srcId > 0 && interrupts[fightId].Count < ParseLimits.MaxInterruptsPerFight)
                    interrupts[fightId].Add(new RawEvent { SourceId = srcId, TargetId = tgtId, SpellId = ParseInt(fields.Length > 9 ? fields[9] : "0"), Timestamp = tsMs });
                break;

            case "SPELL_DISPEL":
                if (srcIsPlayer && srcId > 0 && dispels[fightId].Count < ParseLimits.MaxDispelsPerFight)
                    dispels[fightId].Add(new RawEvent { SourceId = srcId, TargetId = tgtId, SpellId = ParseInt(fields.Length > 9 ? fields[9] : "0"), Timestamp = tsMs });
                break;
        }
    }

    private static CombatLogData BuildResult(
        string gameVersion, int zoneId, string zoneName,
        DateTime start, DateTime end,
        List<FightData> fights,
        Dictionary<int, PlayerMeta> playerMeta,
        Dictionary<int, Dictionary<int, long>> damageDone,
        Dictionary<int, Dictionary<int, long>> damageTaken,
        Dictionary<int, Dictionary<int, long>> healingDone,
        Dictionary<int, List<DeathEvent>> deaths,
        Dictionary<int, Dictionary<int, AuraTracker>> buffs,
        Dictionary<int, Dictionary<int, AuraTracker>> debuffs,
        Dictionary<int, List<CombatantInfoEvent>> combatantInfos,
        Dictionary<int, List<RawEvent>> casts,
        Dictionary<int, List<RawEvent>> interrupts,
        Dictionary<int, List<RawEvent>> dispels)
    {
        var players = BuildPlayerDetails(playerMeta);

        var events = new Dictionary<string, Dictionary<string, List<RawEvent>>>();
        foreach (var fightId in casts.Keys)
        {
            events[fightId.ToString()] = new Dictionary<string, List<RawEvent>>
            {
                ["Casts"]      = casts[fightId],
                ["Interrupts"] = interrupts[fightId],
                ["Dispels"]    = dispels[fightId],
            };
        }

        var deathsOut = new Dictionary<string, List<DeathEvent>>(deaths.Count);
        foreach (var (fightId, list) in deaths)
            deathsOut[fightId.ToString()] = list;

        var combatantInfosOut = new Dictionary<string, List<CombatantInfoEvent>>(combatantInfos.Count);
        foreach (var (fightId, list) in combatantInfos)
            combatantInfosOut[fightId.ToString()] = list;

        return new CombatLogData
        {
            GameVersion      = gameVersion,
            ZoneId           = zoneId,
            ZoneName         = zoneName,
            StartTime        = start.ToString("O"),
            EndTime          = end.ToString("O"),
            Fights           = fights,
            Players          = players,
            DamageDone       = ToEntriesWrappers(damageDone, playerMeta),
            DamageTaken      = ToEntriesWrappers(damageTaken, playerMeta),
            Healing          = ToEntriesWrappers(healingDone, playerMeta),
            Deaths           = deathsOut,
            Buffs            = ToAurasWrappers(buffs),
            Debuffs          = ToAurasWrappers(debuffs),
            CombatantInfo    = combatantInfosOut,
            Events           = events,
            ReportWideEvents = [],
        };
    }

    private static Dictionary<string, EntriesWrapper> ToEntriesWrappers(
        Dictionary<int, Dictionary<int, long>> source,
        Dictionary<int, PlayerMeta> meta)
    {
        var result = new Dictionary<string, EntriesWrapper>(source.Count);
        foreach (var (fightId, actorTotals) in source)
        {
            var entries = actorTotals
                .Select(kv => new EntryItem
                {
                    Name  = meta.TryGetValue(kv.Key, out var m) ? m.Name : kv.Key.ToString(),
                    Total = kv.Value,
                })
                .ToList();
            result[fightId.ToString()] = new EntriesWrapper { Entries = entries };
        }
        return result;
    }

    private static Dictionary<string, AurasWrapper> ToAurasWrappers(
        Dictionary<int, Dictionary<int, AuraTracker>> source)
    {
        var result = new Dictionary<string, AurasWrapper>(source.Count);
        foreach (var (fightId, trackers) in source)
        {
            result[fightId.ToString()] = new AurasWrapper
            {
                Auras = trackers.Values.Select(t => t.ToAuraData()).ToList(),
            };
        }
        return result;
    }

    private static PlayerDetails BuildPlayerDetails(Dictionary<int, PlayerMeta> meta)
    {
        var dps     = new List<PlayerEntry>();
        var healers = new List<PlayerEntry>();
        var tanks   = new List<PlayerEntry>();

        foreach (var (id, m) in meta)
        {
            var entry = new PlayerEntry
            {
                Id     = id,
                Guid   = null,
                Name   = m.Name,
                Server = m.Realm,
                Type   = m.WowClass,
                Specs  = m.SpecName is not null ? [new SpecEntry { Spec = m.SpecName }] : [],
            };
            switch (m.Role)
            {
                case "healer": healers.Add(entry); break;
                case "tank":   tanks.Add(entry);   break;
                default:       dps.Add(entry);     break;
            }
        }

        return new PlayerDetails { Dps = dps, Healers = healers, Tanks = tanks };
    }

    // Stub: requires config/combat_log/classic_class_specs.php (Issue #403)
    private static void DetectClassFromTalents(int actorId, int t1, int t2, int t3, Dictionary<int, PlayerMeta> meta)
    {
    }

    // Stub: requires classic_signature_spells.php (Issue #403)
    private static void ResolveClassFromSpells(Dictionary<int, PlayerMeta> meta, Dictionary<int, int> firstSpell)
    {
        foreach (var (actorId, spellId) in firstSpell)
        {
            if (meta.TryGetValue(actorId, out var m) && m.WowClass is null)
            {
                // TODO(#403): look up spellId in ported signature spell table, set m.WowClass / m.Role
            }
        }
    }

    private static string ExtractGameVersion(string header)
    {
        var parts = header.Split(',');
        int idx   = Array.IndexOf(parts, "PROJECT_ID");
        if (idx < 0 || idx + 1 >= parts.Length)
            throw new ParseException("PROJECT_ID not found in log header.");

        string projectId = parts[idx + 1].Trim();
        return ProjectIdMap.TryGetValue(projectId, out string? ver)
            ? ver
            : throw new ParseException($"Unsupported PROJECT_ID {projectId}. Only Classic Era (2) and Season of Discovery (5) are supported.");
    }

    private static int GetOrAdd(Dictionary<string, int> map, string guid, ref int next)
    {
        if (!map.TryGetValue(guid, out int id))
            map[guid] = id = next++;
        return id;
    }

    private static void AddTo(Dictionary<int, long> dict, int key, long amount)
    {
        dict[key] = dict.GetValueOrDefault(key) + amount;
    }

    private static bool TryParseTimestamp(string s, out DateTime result)
    {
        result = default;
        if (!DateTime.TryParseExact(s, "M/d H:mm:ss.fff",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out result))
            return DateTime.TryParse(s, out result);
        return true;
    }

    private static string[] SplitCsvLine(ReadOnlySpan<char> line)
    {
        var fields = new List<string>(16);
        bool inQuote = false;
        int start = 0;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"') { inQuote = !inQuote; }
            else if (c == ',' && !inQuote)
            {
                fields.Add(ExtractUnquoted(line[start..i]));
                start = i + 1;
            }
        }
        fields.Add(ExtractUnquoted(line[start..]));
        return fields.ToArray();
    }

    private static string ExtractUnquoted(ReadOnlySpan<char> field)
    {
        if (field.IndexOf('"') < 0)
            return new string(field);
        // Strip quote characters — WoW log fields use quotes as delimiters, not content
        var sb = new System.Text.StringBuilder(field.Length);
        foreach (char c in field)
            if (c != '"') sb.Append(c);
        return sb.ToString();
    }

    private static uint ParseHex(string s)
    {
        ReadOnlySpan<char> span = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? s.AsSpan(2)
            : s.AsSpan();
        return uint.TryParse(span, System.Globalization.NumberStyles.HexNumber, null, out uint v) ? v : 0;
    }
    private static int  ParseInt(string s) => int.TryParse(s, out int v) ? v : 0;
    private static long ParseLong(string s) => long.TryParse(s, out long v) ? v : 0;
}

/// <summary>Hard caps on per-fight collection sizes to bound memory and upload payload.</summary>
public static class ParseLimits
{
    // 500 MB — guards against runaway logs that would OOM the process or timeout the upload.
    public const long MaxFileSizeBytes     = 500L * 1024 * 1024;
    // 40-player raid × ~25 wipes per boss = 1 000; comfortably covers the longest progression sessions.
    public const int MaxDeathsPerFight     = 1000;
    // ~1 250 casts/min × 40 players × 60-min fight = upper bound well below 50 000 for any real log.
    public const int MaxCastsPerFight      = 50_000;
    // Interrupts and dispels are rare; 5 000 is orders of magnitude above any real encounter.
    public const int MaxInterruptsPerFight = 5_000;
    public const int MaxDispelsPerFight    = 5_000;
    // Each aura typically has 1–3 bands; 10 000 is far above the ~200 distinct buffs in a 40-player raid.
    public const int MaxAuraBands          = 10_000;
}

/// <summary>In-memory player metadata accumulated while parsing; not serialized.</summary>
internal sealed class PlayerMeta
{
    public string  Name     { get; set; } = "";
    public string  Realm    { get; set; } = "";
    public string? WowClass { get; set; }
    public string? SpecName { get; set; }
    public string  Role     { get; set; } = "dps";
}

/// <summary>Tracks SPELL_AURA_APPLIED / SPELL_AURA_REMOVED pairs to calculate uptime bands.</summary>
internal sealed class AuraTracker
{
    public int    SpellId   { get; set; }
    public string SpellName { get; set; } = "";

    private long _openBandStart = -1;
    private readonly List<AuraBand> _bands = [];

    public int BandCount => _bands.Count;

    public void OpenBand(long tsMs)
    {
        if (_openBandStart < 0)
            _openBandStart = tsMs;
    }

    public void CloseBand(long tsMs)
    {
        if (_openBandStart >= 0)
        {
            _bands.Add(new AuraBand { StartTime = _openBandStart, EndTime = tsMs });
            _openBandStart = -1;
        }
    }

    public AuraData ToAuraData()
    {
        long uptime = _bands.Sum(b => b.EndTime - b.StartTime);
        return new AuraData
        {
            Guid        = SpellId,
            Name        = SpellName,
            TotalUptime = uptime,
            Bands       = [.. _bands],
        };
    }
}
