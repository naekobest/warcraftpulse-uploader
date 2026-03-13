using WarcraftPulseUploader.Parser;
using Xunit;

namespace WarcraftPulseUploader.Tests.Parser;

public class CombatLogParserTests
{
    private static string FixturePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Parser", "Fixtures", name);

    [Fact]
    public void Parse_Sample_ReturnsOneFight()
    {
        var data = CombatLogParser.Parse(FixturePath("sample.txt"));
        Assert.Single(data.Fights);
    }

    [Fact]
    public void Parse_Sample_FightIsKill()
    {
        var data = CombatLogParser.Parse(FixturePath("sample.txt"));
        Assert.True(data.Fights[0].Kill);
    }

    [Fact]
    public void Parse_Sample_ZoneNameCorrect()
    {
        var data = CombatLogParser.Parse(FixturePath("sample.txt"));
        Assert.Equal("Molten Core", data.ZoneName);
    }

    [Fact]
    public void Parse_Sample_GameVersionIsClassicEra()
    {
        var data = CombatLogParser.Parse(FixturePath("sample.txt"));
        Assert.Equal("classic_era", data.GameVersion);
    }

    [Fact]
    public void Parse_Sample_DetectsOnePlayer()
    {
        var data = CombatLogParser.Parse(FixturePath("sample.txt"));
        int total = data.Players.Dps.Count + data.Players.Healers.Count + data.Players.Tanks.Count;
        Assert.Equal(1, total);
    }

    [Fact]
    public void Parse_Sample_DamageDonePopulated()
    {
        var data = CombatLogParser.Parse(FixturePath("sample.txt"));
        Assert.True(data.DamageDone.ContainsKey("1"));
        Assert.NotEmpty(data.DamageDone["1"].Entries);
    }

    [Fact]
    public void Parse_Sample_HealingPopulated()
    {
        var data = CombatLogParser.Parse(FixturePath("sample.txt"));
        Assert.True(data.Healing.ContainsKey("1"));
        Assert.NotEmpty(data.Healing["1"].Entries);
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
}
