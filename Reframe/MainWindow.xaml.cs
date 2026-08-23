using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Reframe.Core.Tabular;
using Reframe.Core.Tabular.Parsers;
using Reframe.Core.Transformers;
using Reframe.Core.Transformers.Formatting;
using Reframe.ViewModels;
using Wpf.Ui.Controls;

namespace Reframe;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataObject.AddPastingHandler(InputEditor, OnInputEditorPasting);

        if (DataContext is MainViewModel vm)
        {
            AttachViewModel(vm);
        }

        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is MainViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            }
            if (e.NewValue is MainViewModel newVm)
            {
                AttachViewModel(newVm);
            }
        };
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (Keyboard.Modifiers == ModifierKeys.Alt)
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.Left)
            {
                if (DataContext is MainViewModel vm && vm.HistoryBackCommand.CanExecute(null))
                {
                    vm.HistoryBackCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (key == Key.Right)
            {
                if (DataContext is MainViewModel vm && vm.HistoryForwardCommand.CanExecute(null))
                {
                    vm.HistoryForwardCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }

    private void AttachViewModel(MainViewModel vm)
    {
        vm.PropertyChanged += OnViewModelPropertyChanged;
        RebuildDataGridColumns(vm.PreviewDataTable);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.PreviewDataTable) && sender is MainViewModel vm)
        {
            RebuildDataGridColumns(vm.PreviewDataTable);
        }
    }

    private void RebuildDataGridColumns(DataTable? dt)
    {
        PreviewDataGrid.Columns.Clear();
        if (dt == null) return;

        foreach (DataColumn column in dt.Columns)
        {
            // For DataRowView / DataTable, property paths with special characters like '/', '.', '[', '(', etc.
            // fail when bound directly as PropertyPath. Escape with indexer syntax: [ColumnName].
            // In WPF PropertyPath indexers, '^', '[', and ']' are escaped with '^'.
            string escapedPropName = column.ColumnName
                .Replace("^", "^^")
                .Replace("[", "^[")
                .Replace("]", "^]");

            var boundColumn = new DataGridTextColumn
            {
                Header = string.IsNullOrEmpty(column.Caption) ? column.ColumnName : column.Caption,
                Binding = new Binding($"[{escapedPropName}]"),
                SortMemberPath = column.ColumnName
            };
            PreviewDataGrid.Columns.Add(boundColumn);
        }
    }

    private void PreviewDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;

        if (DataContext is not MainViewModel vm || vm.PreviewDataTable == null)
            return;

        var column = e.Column;
        var columnName = column.SortMemberPath;
        if (string.IsNullOrEmpty(columnName))
        {
            if (column is DataGridBoundColumn boundCol && boundCol.Binding is Binding b && !string.IsNullOrEmpty(b.Path?.Path))
            {
                columnName = b.Path.Path.Trim('[', ']');
            }
            else
            {
                columnName = column.Header?.ToString();
            }
        }

        if (string.IsNullOrEmpty(columnName) || !vm.PreviewDataTable.Columns.Contains(columnName))
            return;

        var newDirection = column.SortDirection switch
        {
            System.ComponentModel.ListSortDirection.Ascending => System.ComponentModel.ListSortDirection.Descending,
            System.ComponentModel.ListSortDirection.Descending => (System.ComponentModel.ListSortDirection?)null,
            _ => System.ComponentModel.ListSortDirection.Ascending
        };

        foreach (var col in PreviewDataGrid.Columns)
        {
            if (col != column)
            {
                col.SortDirection = null;
            }
        }

        column.SortDirection = newDirection;

        var defaultView = vm.PreviewDataTable.DefaultView;
        if (newDirection == null)
        {
            defaultView.Sort = string.Empty;
        }
        else
        {
            // Escape column name with brackets for DataView.Sort clause
            string sortCol = $"[{columnName.Replace("]", "]]")}]";
            defaultView.Sort = newDirection == System.ComponentModel.ListSortDirection.Ascending
                ? $"{sortCol} ASC"
                : $"{sortCol} DESC";
        }
    }

    private void OnInputEditorPasting(object sender, DataObjectPastingEventArgs e)
    {
        try
        {
            string source = "Pasted";
            string? textToPaste = null;

            if (e.DataObject.GetDataPresent(DataFormats.Html))
            {
                var html = e.DataObject.GetData(DataFormats.Html) as string;
                if (!string.IsNullOrEmpty(html) && HtmlTableParser.IsHtmlTable(html))
                {
                    string cleanTable = HtmlTableParser.ExtractTableHtml(html);
                    textToPaste = TextBeautifier.Beautify(cleanTable);
                    source = "Pasted (HTML Table)";
                }
            }

            if (textToPaste == null && e.DataObject.GetDataPresent(DataFormats.UnicodeText))
            {
                var raw = e.DataObject.GetData(DataFormats.UnicodeText) as string;
                if (!string.IsNullOrEmpty(raw))
                {
                    textToPaste = TextBeautifier.Beautify(raw);
                }
            }
            else if (textToPaste == null && e.DataObject.GetDataPresent(DataFormats.Text))
            {
                var raw = e.DataObject.GetData(DataFormats.Text) as string;
                if (!string.IsNullOrEmpty(raw))
                {
                    textToPaste = TextBeautifier.Beautify(raw);
                }
            }

            if (textToPaste != null)
            {
                var newObj = new DataObject();
                newObj.SetData(DataFormats.UnicodeText, textToPaste);
                newObj.SetData(DataFormats.Text, textToPaste);
                e.DataObject = newObj;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.RecordHistory(InputEditor.Text, source);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        catch
        {
            // Fallback to standard paste behavior
        }
    }

    private void MainWindow_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void MainWindow_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.LoadFromFile(files[0]);
                    e.Handled = true;
                }
            }
        }
    }
}
