using WarcraftPulseUploader.Parser;
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
    public void Parse_Sample_ZoneNameHandlesQuotedFields() =>
        Assert.False(string.IsNullOrWhiteSpace(_data.ZoneName));

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
