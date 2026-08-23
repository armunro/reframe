using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class ColumnSelectorTests
{
    [Fact]
    public void ColumnItem_InitialProperties_AreCorrect()
    {
        var item = new ColumnItem
        {
            Index = 0,
            Name = "ID",
            SampleValue = "101",
            IsSelected = true
        };

        Assert.Equal(0, item.Index);
        Assert.Equal(1, item.DisplayIndex);
        Assert.Equal("ID", item.Name);
        Assert.Equal("101", item.SampleValue);
        Assert.True(item.HasSampleValue);
        Assert.True(item.IsSelected);
        Assert.Equal("[1] ID (e.g. 101)", item.DisplayText);
    }

    [Fact]
    public void ColumnItem_WithoutSampleValue_DisplayTextHasNoSample()
    {
        var item = new ColumnItem
        {
            Index = 2,
            Name = "Department",
            SampleValue = string.Empty,
            IsSelected = false
        };

        Assert.Equal(3, item.DisplayIndex);
        Assert.False(item.HasSampleValue);
        Assert.False(item.IsSelected);
        Assert.Equal("[3] Department", item.DisplayText);
    }

    [Fact]
    public void ColumnItem_PropertyChanged_FiresForDisplayTextAndFlags()
    {
        var item = new ColumnItem { Index = 0, Name = "ColA" };
        var notifiedProperties = new List<string>();
        item.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
            {
                notifiedProperties.Add(e.PropertyName);
            }
        };

        item.Name = "ColB";
        Assert.Contains(nameof(ColumnItem.Name), notifiedProperties);
        Assert.Contains(nameof(ColumnItem.DisplayText), notifiedProperties);

        notifiedProperties.Clear();
        item.SampleValue = "Val1";
        Assert.Contains(nameof(ColumnItem.SampleValue), notifiedProperties);
        Assert.Contains(nameof(ColumnItem.DisplayText), notifiedProperties);
        Assert.Contains(nameof(ColumnItem.HasSampleValue), notifiedProperties);
    }
}
