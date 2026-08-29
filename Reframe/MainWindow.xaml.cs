using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using Reframe.Core.Tabular;
using Reframe.Core.Tabular.Parsers;
using Reframe.Core.Transformers;
using Reframe.Core.Transformers.Formatting;
using Reframe.Services;
using Reframe.ViewModels;
using Wpf.Ui.Controls;

namespace Reframe;

public partial class MainWindow : FluentWindow
{
    private ClipboardWatcher? _clipboardWatcher;

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
                oldVm.HighlightSectionRequested -= OnHighlightSectionRequested;
                oldVm.ClipboardWatcher = null;
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
        vm.HighlightSectionRequested += OnHighlightSectionRequested;
        RebuildDataGridColumns(vm.PreviewDataTable);

        if (_clipboardWatcher == null)
        {
            _clipboardWatcher = new ClipboardWatcher();
            var helper = new WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                _clipboardWatcher.Attach(helper.Handle);
            }
            else
            {
                SourceInitialized += (s, e) =>
                {
                    var h = new WindowInteropHelper(this).Handle;
                    _clipboardWatcher?.Attach(h);
                };
            }
        }

        vm.ClipboardWatcher = _clipboardWatcher;
    }

    private void OnHighlightSectionRequested(object? sender, string sectionKey)
    {
        if (!string.IsNullOrEmpty(sectionKey))
        {
            Reframe.Controls.SectionState.Highlight(sectionKey);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.PreviewDataTable) && sender is MainViewModel vm)
        {
            RebuildDataGridColumns(vm.PreviewDataTable);
        }
        else if (e.PropertyName == nameof(MainViewModel.IsCommandPaletteOpen) && sender is MainViewModel vm2)
        {
            if (vm2.IsCommandPaletteOpen)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ActionSearchTextBox.Focus();
                    ActionSearchTextBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.IsWebRequestDialogOpen) && sender is MainViewModel vmWeb)
        {
            if (vmWeb.IsWebRequestDialogOpen)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    WebRequestUrlTextBox.Focus();
                    WebRequestUrlTextBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }
        else if (e.PropertyName == nameof(MainViewModel.SelectedAction) && sender is MainViewModel vm3)
        {
            if (vm3.SelectedAction != null)
            {
                ActionSearchResultsListBox.ScrollIntoView(vm3.SelectedAction);
            }
        }
    }

    private void CommandPaletteBackdrop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsCommandPaletteOpen = false;
        }
    }

    private void WebRequestBackdrop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsWebRequestDialogOpen = false;
        }
    }


    private void WebRequestUrlTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        if (e.Key == Key.Enter)
        {
            vm.ExecuteWebRequestCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            vm.CloseWebRequestDialogCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ActionSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        if (e.Key == Key.Down)
        {
            vm.SelectNextActionCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            vm.SelectPreviousActionCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            vm.ExecuteSelectedActionCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            vm.CloseCommandPaletteCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ActionSearchResultsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ExecuteSelectedActionCommand.Execute(null);
        }
    }

    private void ActionSearchResultsListBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        if (e.Key == Key.Enter)
        {
            vm.ExecuteSelectedActionCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            vm.CloseCommandPaletteCommand.Execute(null);
            e.Handled = true;
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
            string colName = column.ColumnName;
            string escapedPropName = colName
                .Replace("^", "^^")
                .Replace("[", "^[")
                .Replace("]", "^]");

            var removeMenuItem = new System.Windows.Controls.MenuItem
            {
                Header = $"🗑️ Remove Column '{colName}'"
            };
            removeMenuItem.Click += (s, e) =>
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.RemoveColumnCommand.Execute(colName);
                }
            };

            var copyColMenuItem = new System.Windows.Controls.MenuItem
            {
                Header = $"📋 Extract Column '{colName}'"
            };
            copyColMenuItem.Click += (s, e) =>
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.SelectedColumn = colName;
                    vm.ActionCommand.Execute("ExtractColumn");
                }
            };

            var headerContextMenu = new ContextMenu();
            headerContextMenu.Items.Add(removeMenuItem);
            headerContextMenu.Items.Add(copyColMenuItem);

            var baseHeaderStyle = PreviewDataGrid.TryFindResource(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader)) as Style
                                  ?? System.Windows.Application.Current?.TryFindResource(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader)) as Style;

            var headerStyle = baseHeaderStyle != null
                ? new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader), baseHeaderStyle)
                : new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));

            headerStyle.Setters.Add(new Setter(FrameworkElement.ContextMenuProperty, headerContextMenu));

            var cellElementStyle = PreviewDataGrid.TryFindResource("DataGridTextCellElementStyle") as Style
                                  ?? System.Windows.Application.Current?.TryFindResource("DataGridTextCellElementStyle") as Style;

            var boundColumn = new DataGridTextColumn
            {
                Header = string.IsNullOrEmpty(column.Caption) ? colName : column.Caption,
                Binding = new Binding($"[{escapedPropName}]"),
                SortMemberPath = colName,
                HeaderStyle = headerStyle
            };

            if (cellElementStyle != null)
            {
                boundColumn.ElementStyle = cellElementStyle;
            }

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

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is MainViewModel vm)
        {
            vm.ClipboardWatcher = null;
        }
        _clipboardWatcher?.Dispose();
        _clipboardWatcher = null;
    }
}
