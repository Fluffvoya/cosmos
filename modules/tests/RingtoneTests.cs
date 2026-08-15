using app_ringtone;

namespace tests;

/// <summary>
/// Unit tests for the app-ringtone module: RingtoneEntry model.
/// </summary>
public class RingtoneTests
{
    // ── RingtoneEntry properties ────────────────────────────────────

    [Fact]
    public void RingtoneEntry_DefaultConstructor_PropertiesAreEmpty()
    {
        var entry = new RingtoneEntry();
        Assert.Equal(string.Empty, entry.Id);
        Assert.Equal(string.Empty, entry.FilePath);
        Assert.Equal(string.Empty, entry.Label);
    }

    [Fact]
    public void RingtoneEntry_CanSetProperties()
    {
        var entry = new RingtoneEntry
        {
            Id = "test-id-123",
            FilePath = @"C:\audio\ringtone.mp3",
            Label = "ringtone.mp3"
        };

        Assert.Equal("test-id-123", entry.Id);
        Assert.Equal(@"C:\audio\ringtone.mp3", entry.FilePath);
        Assert.Equal("ringtone.mp3", entry.Label);
    }

    // ── RingtoneEntry.FromPath ──────────────────────────────────────

    [Fact]
    public void FromPath_SetsFilePath()
    {
        var entry = RingtoneEntry.FromPath(@"C:\sounds\alarm.wav");
        Assert.Equal(@"C:\sounds\alarm.wav", entry.FilePath);
    }

    [Fact]
    public void FromPath_DerivesLabelFromFileName()
    {
        var entry = RingtoneEntry.FromPath(@"C:\sounds\alarm.wav");
        Assert.Equal("alarm.wav", entry.Label);
    }

    [Fact]
    public void FromPath_DerivesLabelFromUnixPath()
    {
        var entry = RingtoneEntry.FromPath("/home/user/sounds/bell.mp3");
        Assert.Equal("bell.mp3", entry.Label);
    }

    [Fact]
    public void FromPath_GeneratesNonEmptyId()
    {
        var entry = RingtoneEntry.FromPath(@"C:\test.mp3");
        Assert.False(string.IsNullOrEmpty(entry.Id));
    }

    [Fact]
    public void FromPath_GeneratesUniqueIds()
    {
        var entry1 = RingtoneEntry.FromPath(@"C:\test.mp3");
        var entry2 = RingtoneEntry.FromPath(@"C:\test.mp3");
        Assert.NotEqual(entry1.Id, entry2.Id);
    }

    [Fact]
    public void FromPath_FileNameOnly_LabelIsSameAsPath()
    {
        var entry = RingtoneEntry.FromPath("notification.ogg");
        Assert.Equal("notification.ogg", entry.Label);
    }
}
