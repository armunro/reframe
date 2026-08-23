using System;
using System.IO;
using System.Windows.Controls;
using Reframe.Controls;
using Reframe.Core.State;
using Xunit;

namespace Reframe.Tests;

public class SectionStateTests : IDisposable
{
    private readonly string _tempFile;

    public SectionStateTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"reframe_sections_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            try { File.Delete(_tempFile); } catch { }
        }
    }

    [Fact]
    public void DefaultState_ForUntrackedSection_IsCollapsed()
    {
        var manager = new SectionStateManager(_tempFile);

        bool state = manager.GetState("Presets_DeveloperFavorites");

        Assert.False(state);
    }

    [Fact]
    public void SetState_RemembersOpenAndClosedSections()
    {
        var manager = new SectionStateManager(_tempFile);

        manager.SetState("Presets_DeveloperFavorites", true);
        manager.SetState("Presets_QuickArrays", false);
        manager.SetState("Tabular_HeadersColumns", true);

        Assert.True(manager.GetState("Presets_DeveloperFavorites"));
        Assert.False(manager.GetState("Presets_QuickArrays"));
        Assert.True(manager.GetState("Tabular_HeadersColumns"));
        Assert.False(manager.GetState("Lines_JoinLines")); // unrecorded -> collapsed
    }

    [Fact]
    public void Persistence_SavesAndReloadsStateFromDisk()
    {
        var manager1 = new SectionStateManager(_tempFile);
        manager1.SetState("Code_SqlQueries", true);
        manager1.SetState("CaseEnc_CaseConversions", true);
        manager1.SetState("Lines_NumberLines", false);

        Assert.True(File.Exists(_tempFile));

        var manager2 = new SectionStateManager(_tempFile);
        manager2.Load();

        Assert.True(manager2.GetState("Code_SqlQueries"));
        Assert.True(manager2.GetState("CaseEnc_CaseConversions"));
        Assert.False(manager2.GetState("Lines_NumberLines"));
        Assert.False(manager2.GetState("Tabular_BreakApartExtract")); // unrecorded
    }

    [Fact]
    public void CaseInsensitiveKeyMatching_WorksCorrectly()
    {
        var manager = new SectionStateManager(_tempFile);
        manager.SetState("presets_developerfavorites", true);

        Assert.True(manager.GetState("PRESETS_DEVELOPERFAVORITES"));
        Assert.True(manager.GetState("Presets_DeveloperFavorites"));
    }

    [Fact]
    public void SectionState_AttachedProperty_AppliesStateAndUpdatesManager()
    {
        Exception? threadEx = null;
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                var manager = SectionStateManager.Instance;
                string testKey = $"TestKey_{Guid.NewGuid():N}";

                try
                {
                    // By default untracked key is collapsed
                    var expander = new Expander();
                    SectionState.SetKey(expander, testKey);

                    Assert.False(expander.IsExpanded);

                    // Expanding it updates manager
                    expander.IsExpanded = true;
                    Assert.True(manager.GetState(testKey));

                    // Collapsing it updates manager
                    expander.IsExpanded = false;
                    Assert.False(manager.GetState(testKey));
                }
                finally
                {
                    // Cleanup
                    manager.SetState(testKey, false);
                }
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadEx != null)
        {
            throw new Exception("STA test failed", threadEx);
        }
    }
}
