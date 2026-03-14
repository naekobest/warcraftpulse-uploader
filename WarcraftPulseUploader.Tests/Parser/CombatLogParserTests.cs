using WarcraftPulseUploader.Parser;
using WarcraftPulseUploader.Services;
using Xunit;

namespace WarcraftPulseUploader.Tests.Parser;

/// <summary>Parses sample.txt once and shares the result across all sample-based tests.</summary>
public sealed class SampleFixture
{
    public CombatLogData Data { get; } = CombatLogParser.Parse(
        Path.Combine(AppContext.BaseDirectory, "Parser", "Fixtures", "sample.txt"));
}

public class CombatLogParserTests : IClassFixture<SampleFixture>
{
    private readonly CombatLogData _data;

    public CombatLogParserTests(SampleFixture fixture) => _data = fixture.Data;

    [Fact]
    public void Parse_Sample_ReturnsOneFight() =>
        Assert.Single(_data.Fights);

    [Fact]
    public void Parse_Sample_FightIsKill() =>
        Assert.True(_data.Fights[0].Kill);

    [Fact]
    public void Parse_Sample_ZoneNameCorrect() =>
        Assert.Equal("Molten Core", _data.ZoneName);

    [Fact]
    public void Parse_Sample_GameVersionIsClassicEra() =>
        Assert.Equal("classic_era", _data.GameVersion);

    [Fact]
    public void Parse_Sample_DetectsOnePlayer()
    {
        int total = _data.Players.Dps.Count + _data.Players.Healers.Count + _data.Players.Tanks.Count;
        Assert.Equal(1, total);
    }

    [Fact]
    public void Parse_Sample_DamageDonePopulated()
    {
        Assert.True(_data.DamageDone.ContainsKey("1"));
        Assert.NotEmpty(_data.DamageDone["1"].Entries);
    }

    [Fact]
    public void Parse_Sample_DamageDoneEntryHasNameAndTotal()
    {
        var entry = _data.DamageDone["1"].Entries[0];
        Assert.False(string.IsNullOrEmpty(entry.Name));
        Assert.True(entry.Total > 0);
    }

    [Fact]
    public void Parse_Sample_HealingPopulated()
    {
        Assert.True(_data.Healing.ContainsKey("1"));
        Assert.NotEmpty(_data.Healing["1"].Entries);
    }

    [Fact]
    public void Parse_EmptyFile_ThrowsParseException()
    {
        var path = Path.GetTempFileName();
        try
        {
            Assert.Throws<ParseException>(() => CombatLogParser.Parse(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_NoEncounters_ThrowsParseException()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "COMBAT_LOG_VERSION,9,ADVANCED_LOG_ENABLED,1,BUILD_VERSION,1.15.8,PROJECT_ID,2\n");
        try
        {
            Assert.Throws<ParseException>(() => CombatLogParser.Parse(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_UnsupportedProjectId_ThrowsParseException()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "COMBAT_LOG_VERSION,9,ADVANCED_LOG_ENABLED,1,BUILD_VERSION,4.4.0,PROJECT_ID,1\n");
        try
        {
            Assert.Throws<ParseException>(() => CombatLogParser.Parse(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_ThrowsParseException_WhenFileExceedsMaxSize()
    {
        var path = Path.GetTempFileName();
        try
        {
            var ex = Assert.Throws<ParseException>(() =>
                CombatLogParser.ParseWithSizeGuard(path, maxBytes: 0));
            Assert.Contains("too large", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a combat log")]
    [InlineData("  \t  \n  ")]
    [InlineData("1/1 0:00:00.000  COMBAT_LOG_VERSION,20,advanced,1,PROJECT_ID,2")]
    public void Parse_DoesNotThrow_OnMalformedInput(string content)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, content);
            try { CombatLogParser.ParseWithSizeGuard(path); }
            catch (ParseException) { /* expected for invalid logs */ }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Parse_DoesNotThrow_OnVeryLongLine()
    {
        var path = Path.GetTempFileName();
        try
        {
            var longLine = new string('A', 1_000_000);
            File.WriteAllText(path, longLine);
            try { CombatLogParser.ParseWithSizeGuard(path); }
            catch (ParseException) { /* expected */ }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Parse_DeathsCappedAtLimit()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("COMBAT_LOG_VERSION,9,ADVANCED_LOG_ENABLED,1,BUILD_VERSION,1.15.8,PROJECT_ID,2");
        sb.AppendLine("1/1 0:00:00.000  ZONE_CHANGE,1234,Molten Core,null");
        sb.AppendLine("1/1 0:00:01.000  ENCOUNTER_START,703,Ragnaros,9,40");
        for (int i = 0; i < 1100; i++)
            sb.AppendLine($"1/1 0:00:02.000  UNIT_DIED,0x0000000000000000,nil,0x80000000,0x00000400,Player-1-{i:D4},Player{i}-Realm,0x00000512,0x00000000");
        sb.AppendLine("1/1 0:00:03.000  ENCOUNTER_END,703,Ragnaros,9,40,1");

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, sb.ToString());
            var data = CombatLogParser.ParseWithSizeGuard(path);
            Assert.True(data.Deaths.ContainsKey("1"));
            Assert.Equal(ParseLimits.MaxDeathsPerFight, data.Deaths["1"].Count);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ParseWithHash_HashMatchesManualSha256()
    {
        string fixturePath = Path.Combine(AppContext.BaseDirectory, "Parser", "Fixtures", "sample.txt");
        string expectedHash = UploadHistory.HashFile(fixturePath);

        var (_, actualHash) = CombatLogParser.ParseWithHash(fixturePath);

        Assert.Equal(expectedHash, actualHash, ignoreCase: true);
    }

    [Fact]
    public void Parse_AuraBandsCappedAtLimit()
    {
        // Build a log that applies and removes the same buff more than MaxAuraBands times.
        // Each APPLIED+REMOVED pair adds one band; the limit should prevent the list from growing further.
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("COMBAT_LOG_VERSION,9,ADVANCED_LOG_ENABLED,1,BUILD_VERSION,1.15.8,PROJECT_ID,2");
        sb.AppendLine("1/1 0:00:00.000  ZONE_CHANGE,1234,Molten Core,null");
        sb.AppendLine("1/1 0:00:01.000  ENCOUNTER_START,703,Ragnaros,9,40");
        for (int i = 0; i < ParseLimits.MaxAuraBands + 100; i++)
        {
            // Use distinct timestamps so each band has a non-zero duration.
            string applyTs  = $"1/1 0:{i / 3600:D2}:{(i % 3600) / 60:D2}:{i % 60:D2}.000";
            string removeTs = $"1/1 0:{i / 3600:D2}:{(i % 3600) / 60:D2}:{i % 60:D2}.500";
            sb.AppendLine($"{applyTs}  SPELL_AURA_APPLIED,0x0000000000000001,Npc,0x10a48,0x0,0x0000000000000002,Player1-Realm,0x00000512,0x0,9999,PowerWordFortitude,1,BUFF");
            sb.AppendLine($"{removeTs}  SPELL_AURA_REMOVED,0x0000000000000001,Npc,0x10a48,0x0,0x0000000000000002,Player1-Realm,0x00000512,0x0,9999,PowerWordFortitude,1,BUFF");
        }
        sb.AppendLine("1/1 0:00:03.000  ENCOUNTER_END,703,Ragnaros,9,40,1");

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, sb.ToString());
            var data = CombatLogParser.ParseWithSizeGuard(path);
            Assert.True(data.Buffs.ContainsKey("1"));
            var aura = data.Buffs["1"].Auras.FirstOrDefault(a => a.Guid == 9999);
            Assert.NotNull(aura);
            Assert.Equal(ParseLimits.MaxAuraBands, aura.Bands.Count);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void ParseWithSizeGuard_ThrowsParseException_WhenPathIsSymlink()
    {
        var target = Path.GetTempFileName();
        var link   = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
        try
        {
            File.WriteAllText(target, "dummy content");
            try
            {
                File.CreateSymbolicLink(link, target);
            }
            catch (UnauthorizedAccessException)
            {
                return; // skip — no symlink privilege on this machine
            }

            var ex = Assert.Throws<ParseException>(() => CombatLogParser.ParseWithSizeGuard(link));
            Assert.Contains("symlink", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(link))   File.Delete(link);
            if (File.Exists(target)) File.Delete(target);
        }
    }
}

public class AuraTrackingTests
{
    [Fact]
    public void Parse_OpenAuraBandAtEncounterEnd_IsClosedWithEncounterDuration()
    {
        // A SPELL_AURA_APPLIED without a matching REMOVED — the band should be closed at ENCOUNTER_END
        string header = "COMBAT_LOG_VERSION,9,ADVANCED_LOG_ENABLED,1,BUILD_VERSION,1.15.8,PROJECT_ID,2";
        string start  = "1/1 0:00:00.000  ENCOUNTER_START,1,\"Lucifron\",9,40";
        string apply  = "1/1 0:00:01.000  SPELL_AURA_APPLIED,Player-1-00000001,\"Testplayer\",0x400,0x0,Boss-0-00000001,\"Lucifron\",0x0,0x0,17,\"Mark of Kazzak\",0x20,BUFF";
        string end    = "1/1 0:00:10.000  ENCOUNTER_END,1,\"Lucifron\",9,40,1";
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, string.Join("\n", header, start, apply, end));
            var data = CombatLogParser.Parse(path);
            Assert.Single(data.Fights);
            Assert.True(data.Buffs.ContainsKey("1"), "Buffs dict should have fight 1");
            var aura = data.Buffs["1"].Auras.FirstOrDefault(a => a.Name == "Mark of Kazzak");
            Assert.NotNull(aura);
            // Aura applied at 1000ms, encounter ends at 10000ms → uptime should be 9000ms
            Assert.Equal(9000, aura.TotalUptime);
        }
        finally { File.Delete(path); }
    }
}

public class TimestampParsingTests
{
    private static CombatLogData ParseMinimalLog(string timestamp)
    {
        string header = "COMBAT_LOG_VERSION,9,ADVANCED_LOG_ENABLED,1,BUILD_VERSION,1.15.8,PROJECT_ID,2";
        string start  = $"{timestamp}  ENCOUNTER_START,1,\"Lucifron\",9,40";
        string end    = $"{timestamp}  ENCOUNTER_END,1,\"Lucifron\",9,40,1";
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, string.Join("\n", header, start, end));
            return CombatLogParser.Parse(path);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("1/15 21:45:23.456")]
    [InlineData("12/31 9:05:01.000")]
    [InlineData("3/1 0:00:00.001")]
    [InlineData("6/15 23:59:59.999")]
    public void Parse_ValidTimestamps_ProduceOneFight(string ts)
    {
        var data = ParseMinimalLog(ts);
        Assert.Single(data.Fights);
    }

    [Theory]
    [InlineData("1/15 21:45:23.456")]
    [InlineData("12/31 9:05:01.000")]
    public void Parse_SameStartEnd_FightDurationIsZero(string ts)
    {
        var data = ParseMinimalLog(ts);
        Assert.Equal(0, data.Fights[0].EndTime - data.Fights[0].StartTime);
    }
}
