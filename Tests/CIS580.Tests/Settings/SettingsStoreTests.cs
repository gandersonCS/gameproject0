using System;
using System.IO;
using CIS580.Settings;

namespace CIS580.Tests.Settings;

public sealed class SettingsStoreTests : IDisposable
{
    private enum Speed { Slow, Fast }

    private sealed class Prefs
    {
        public int Volume { get; set; } = 5;
        public Speed Speed { get; set; } = Speed.Slow;
    }

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "cis580-tests-" + Guid.NewGuid().ToString("N"));
    private readonly SettingsStore _store;

    public SettingsStoreTests() => _store = new SettingsStore(_directory);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Missing_LoadsTheDefaults()
    {
        var prefs = _store.Load("prefs.json", () => new Prefs { Volume = 7 });
        Assert.Equal(7, prefs.Volume);
        Assert.Null(_store.LastError);
    }

    [Fact]
    public void Saved_LoadsBack_CreatingTheFolder()
    {
        Assert.True(_store.Save("prefs.json", new Prefs { Volume = 2, Speed = Speed.Fast }));

        var prefs = _store.Load("prefs.json", () => new Prefs());

        Assert.Equal(2, prefs.Volume);
        Assert.Equal(Speed.Fast, prefs.Speed);
    }

    [Fact]
    public void Json_IsReadable()
    {
        _store.Save("prefs.json", new Prefs { Speed = Speed.Fast });
        string json = File.ReadAllText(_store.PathOf("prefs.json"));

        Assert.Contains("\"speed\": \"Fast\"", json);
        Assert.False(File.Exists(_store.PathOf("prefs.json.tmp")));
    }

    [Fact]
    public void MissingProperties_KeepTheirDefaults()
    {
        // e.g. settings saved by an older version of the game
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_store.PathOf("prefs.json"), "{ \"volume\": 9 }");

        var prefs = _store.Load("prefs.json", () => new Prefs());

        Assert.Equal(9, prefs.Volume);
        Assert.Equal(Speed.Slow, prefs.Speed);
    }

    [Fact]
    public void Damaged_LoadsTheDefaults_AndKeepsACopy()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_store.PathOf("prefs.json"), "{ not json");

        var prefs = _store.Load("prefs.json", () => new Prefs());

        Assert.Equal(5, prefs.Volume);
        Assert.NotNull(_store.LastError);
        Assert.True(File.Exists(_store.PathOf("prefs.json.bad")));
        Assert.False(_store.Exists("prefs.json"));
    }

    [Fact]
    public void AFailedSave_ReportsRatherThanThrows()
    {
        // A file where the folder should be
        File.WriteAllText(_directory, "");
        try
        {
            Assert.False(_store.Save("prefs.json", new Prefs()));
            Assert.NotNull(_store.LastError);
        }
        finally
        {
            File.Delete(_directory);
        }
    }

    [Fact]
    public void Delete_GoesBackToTheDefaults()
    {
        _store.Save("prefs.json", new Prefs { Volume = 1 });
        _store.Delete("prefs.json");
        Assert.Equal(5, _store.Load("prefs.json", () => new Prefs()).Volume);
    }
}
