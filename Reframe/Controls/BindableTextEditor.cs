using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using Reframe.Highlighting;

namespace Reframe.Controls;

public class BindableTextEditor : TextEditor
{
    public static readonly DependencyProperty BoundTextProperty =
        DependencyProperty.Register(
            nameof(BoundText),
            typeof(string),
            typeof(BindableTextEditor),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                OnBoundTextChanged));

    public static readonly DependencyProperty SyntaxLanguageProperty =
        DependencyProperty.Register(
            nameof(SyntaxLanguage),
            typeof(string),
            typeof(BindableTextEditor),
            new FrameworkPropertyMetadata("Auto", OnSyntaxLanguageChanged));

    private bool _isUpdatingText;

    public BindableTextEditor()
    {
        // Setup Dark Neutral Gray Theme Defaults rgb(24, 24, 24) / #181818
        Background = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x18));
        Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
        LineNumbersForeground = new SolidColorBrush(Color.FromRgb(0x68, 0x68, 0x68));
        ShowLineNumbers = true;
        FontFamily = new FontFamily("Consolas, Cascadia Code, JetBrains Mono, Courier New, monospace");
        FontSize = 13;
        BorderThickness = new Thickness(0);
        Padding = new Thickness(8);

        // Configure TextArea colors for Dark Gray Theme
        if (TextArea != null)
        {
            TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(0x66, 0x46, 0x5E, 0x8A));
            TextArea.SelectionBorder = null;
            TextArea.SelectionForeground = null;
            if (TextArea.Caret != null)
            {
                TextArea.Caret.CaretBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0xE1, 0xEB));
            }
            if (TextArea.TextView != null)
            {
                TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush(Color.FromRgb(0x4E, 0xA6, 0xEA));
            }
        }

        TextChanged += OnEditorTextChanged;
    }

    public string BoundText
    {
        get => (string)GetValue(BoundTextProperty);
        set => SetValue(BoundTextProperty, value);
    }

    public string SyntaxLanguage
    {
        get => (string)GetValue(SyntaxLanguageProperty);
        set => SetValue(SyntaxLanguageProperty, value);
    }

    private static void OnBoundTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BindableTextEditor editor)
        {
            editor.UpdateEditorText((string)e.NewValue);
        }
    }

    private void UpdateEditorText(string? newText)
    {
        if (_isUpdatingText) return;

        try
        {
            _isUpdatingText = true;
            string text = newText ?? string.Empty;
            if (Text != text)
            {
                int caret = CaretOffset;
                Document.Text = text;
                CaretOffset = Math.Min(caret, Document.TextLength);
            }
        }
        finally
        {
            _isUpdatingText = false;
        }
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingText) return;

        try
        {
            _isUpdatingText = true;
            SetCurrentValue(BoundTextProperty, Text);
        }
        finally
        {
            _isUpdatingText = false;
        }
    }

    private static void OnSyntaxLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BindableTextEditor editor)
        {
            editor.UpdateSyntaxHighlighting((string)e.NewValue);
        }
    }

    public void UpdateSyntaxHighlighting(string? language)
    {
        SyntaxHighlighting = DarkThemeHighlighting.GetDefinition(language);
    }
}
