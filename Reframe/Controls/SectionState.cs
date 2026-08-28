using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Reframe.Core.State;

namespace Reframe.Controls;

/// <summary>
/// Attached property for WPF <see cref="Expander"/> controls to remember their open/closed state
/// and support temporary visual highlighting.
/// All sections are collapsed by default unless previously expanded.
/// </summary>
public static class SectionState
{
    private static readonly Dictionary<string, WeakReference<Expander>> _registeredExpanders = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<Expander, DispatcherTimer> _activeHighlightTimers = new();
    private static readonly Dictionary<Expander, (Brush? BorderBrush, Thickness BorderThickness)> _originalProperties = new();

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
            _registeredExpanders[key] = new WeakReference<Expander>(expander);

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
                _registeredExpanders[key] = new WeakReference<Expander>(expander);
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

    /// <summary>
    /// Finds the Expander registered for the given section key, expands it, scrolls it into view, and highlights it.
    /// </summary>
    public static void Highlight(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (!_registeredExpanders.TryGetValue(key, out var weakRef) || !weakRef.TryGetTarget(out var expander))
            return;

        void DoHighlight()
        {
            expander.IsExpanded = true;
            expander.BringIntoView();
            ApplyTemporaryHighlight(expander);
        }

        if (!expander.Dispatcher.CheckAccess())
        {
            expander.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)DoHighlight);
        }
        else
        {
            expander.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)DoHighlight);
        }
    }

    private static void ApplyTemporaryHighlight(Expander expander)
    {
        if (_activeHighlightTimers.TryGetValue(expander, out var existingTimer))
        {
            existingTimer.Stop();
            _activeHighlightTimers.Remove(expander);
        }
        else
        {
            _originalProperties[expander] = (expander.BorderBrush, expander.BorderThickness);
        }

        var highlightBorder = new SolidColorBrush(Color.FromRgb(0, 150, 255));

        expander.BorderBrush = highlightBorder;
        expander.BorderThickness = new Thickness(1);

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(2000)
        };

        timer.Tick += (s, e) =>
        {
            timer.Stop();
            _activeHighlightTimers.Remove(expander);

            if (_originalProperties.TryGetValue(expander, out var original))
            {
                expander.BorderBrush = original.BorderBrush;
                expander.BorderThickness = original.BorderThickness;
                _originalProperties.Remove(expander);
            }
        };

        _activeHighlightTimers[expander] = timer;
        timer.Start();
    }
}
