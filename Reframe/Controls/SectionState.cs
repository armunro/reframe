using System.Windows;
using System.Windows.Controls;
using Reframe.Core.State;

namespace Reframe.Controls;

/// <summary>
/// Attached property for WPF <see cref="Expander"/> controls to remember their open/closed state.
/// All sections are collapsed by default unless previously expanded.
/// </summary>
public static class SectionState
{
    public static readonly DependencyProperty KeyProperty =
        DependencyProperty.RegisterAttached(
            "Key",
            typeof(string),
            typeof(SectionState),
            new PropertyMetadata(null, OnKeyChanged));

    public static string? GetKey(DependencyObject obj) => (string?)obj.GetValue(KeyProperty);

    public static void SetKey(DependencyObject obj, string? value) => obj.SetValue(KeyProperty, value);

    private static void OnKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Expander expander) return;

        expander.Expanded -= OnExpanderExpanded;
        expander.Collapsed -= OnExpanderCollapsed;
        expander.Loaded -= OnExpanderLoaded;

        if (e.NewValue is string key && !string.IsNullOrEmpty(key))
        {
            expander.Expanded += OnExpanderExpanded;
            expander.Collapsed += OnExpanderCollapsed;

            if (expander.IsLoaded)
            {
                ApplyState(expander, key);
            }
            else
            {
                expander.Loaded += OnExpanderLoaded;
            }
        }
    }

    private static void OnExpanderLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Expander expander)
        {
            string? key = GetKey(expander);
            if (!string.IsNullOrEmpty(key))
            {
                ApplyState(expander, key);
            }
        }
    }

    private static void ApplyState(Expander expander, string key)
    {
        bool isExpanded = SectionStateManager.Instance.GetState(key, defaultValue: false);
        expander.IsExpanded = isExpanded;
    }

    private static void OnExpanderExpanded(object sender, RoutedEventArgs e)
    {
        if (sender is Expander expander && ReferenceEquals(e.OriginalSource, expander))
        {
            string? key = GetKey(expander);
            if (!string.IsNullOrEmpty(key))
            {
                SectionStateManager.Instance.SetState(key, true);
            }
        }
    }

    private static void OnExpanderCollapsed(object sender, RoutedEventArgs e)
    {
        if (sender is Expander expander && ReferenceEquals(e.OriginalSource, expander))
        {
            string? key = GetKey(expander);
            if (!string.IsNullOrEmpty(key))
            {
                SectionStateManager.Instance.SetState(key, false);
            }
        }
    }
}
