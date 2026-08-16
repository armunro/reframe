using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TextForge.Core.Analysis;
using TextForge.Core.History;
using TextForge.Core.Tabular;
using TextForge.Core.Transformers;
using TextForge.Highlighting;

namespace TextForge.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private string _inputText = string.Empty;
    private string _outputText = string.Empty;
    private string _statusMessage = "Ready";
    private bool _isRealTimeTransform = true;
    private bool _isWordWrap = false;
    private bool _autoSendOutputToInput = false;
    private bool _isAutoSendingOutput = false;
    private TextAnalysisResult _analysis = new();
    private DataTable? _previewDataTable;
    private TabularData? _currentTable;

    // Line Quote Options
    private QuoteStyle _quoteStyle = QuoteStyle.SingleQuotes;
    private string _customQuotePrefix = "";
    private string _customQuoteSuffix = "";
    private bool _skipEmptyLines = true;
    private bool _escapeInnerQuotes = true;

    // Line Join Options
    private string _joinDelimiter = ", ";
    private QuoteStyle _joinItemQuote = QuoteStyle.None;
    private string _joinOverallPrefix = "";
    private string _joinOverallSuffix = "";

    // Line Split Options
    private string _splitDelimiter = ",";
    private bool _splitIsRegex = false;
    private bool _splitTrimItems = true;
    private bool _splitRemoveEmpty = true;

    // Sort & Distinct Options
    private SortOrder _sortOrder = SortOrder.NaturalNumericAsc;
    private DeduplicateMode _deduplicateMode = DeduplicateMode.Distinct;
    private bool _caseSensitiveDistinct = false;

    // Trim & Clean Options
    private bool _trimStart = true;
    private bool _trimEnd = true;
    private bool _collapseWhitespace = false;
    private bool _removeEmptyLines = true;

    // Prefix/Suffix & Numbering
    private string _linePrefix = "";
    private string _lineSuffix = "";
    private bool _linePrefixSkipFirst = false;
    private bool _linePrefixSkipLast = false;
    private bool _lineSuffixSkipFirst = false;
    private bool _lineSuffixSkipLast = false;
    private string _numberFormat = "{n}. ";
    private int _startNumber = 1;

    // Filter Options
    private string _filterQuery = "";
    private bool _filterIsRegex = false;
    private bool _filterKeepMatching = true;
    private bool _filterCaseSensitive = false;

    // Find & Replace on Lines
    private string _lineFind = "";
    private string _lineReplace = "";
    private bool _lineReplaceIsRegex = false;
    private bool _lineReplaceCaseSensitive = false;

    // Regex Extract
    private string _regexExtractPattern = @"\d+";
    private int _regexCaptureGroup = 0;

    // Tabular Options
    private string _sqlTableName = "MyTable";
    private ObservableCollection<string> _detectedColumns = new();
    private ObservableCollection<ColumnItem> _columnItems = new();
    private string? _selectedColumn;
    private string? _selectedKeyColumn;
    private string? _selectedValueColumn;
    private string _tableExtractDelimiter = ", ";
    private string _tableColumnPrefix = "";
    private string _tableColumnSuffix = "";
    private string _tableColumnFind = "";
    private string _tableColumnReplaceWith = "";
    private string _tableFilterQuery = "";
    private SortOrder _tableSortOrder = SortOrder.NaturalNumericAsc;

    // History & Timeline Management
    private readonly InputHistoryManager _historyManager = new();
    private InputHistoryItem? _selectedHistoryItem;
    private string _historySearchQuery = string.Empty;
    private ICollectionView? _historyView;
    private bool _isNavigatingHistory = false;

    // Selected Operation ID for real-time mode
    private string _currentAction = "SqlIn";

    public MainViewModel()
    {
        InitializeCommands();
        // Set sample text initially
        InputText = "1001\n1002\n1003\n1004\n1005";
        RecordHistory(InputText, "Initial Sample");
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (_inputText != value)
            {
                _inputText = value;
                OnPropertyChanged();
                AnalyzeInput();
                if (IsRealTimeTransform && !_isAutoSendingOutput)
                {
                    ExecuteCurrentAction(autoSendToInput: false);
                }
            }
        }
    }

    public string OutputText
    {
        get => _outputText;
        set
        {
            if (_outputText != value)
            {
                _outputText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OutputStats));
                OnPropertyChanged(nameof(EffectiveOutputSyntax));
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsRealTimeTransform
    {
        get => _isRealTimeTransform;
        set
        {
            if (_isRealTimeTransform != value)
            {
                _isRealTimeTransform = value;
                OnPropertyChanged();
                if (value)
                {
                    ExecuteCurrentAction(autoSendToInput: false);
                }
            }
        }
    }

    public bool IsWordWrap
    {
        get => _isWordWrap;
        set
        {
            if (_isWordWrap != value)
            {
                _isWordWrap = value;
                OnPropertyChanged();
            }
        }
    }

    public bool AutoSendOutputToInput
    {
        get => _autoSendOutputToInput;
        set
        {
            if (_autoSendOutputToInput != value)
            {
                _autoSendOutputToInput = value;
                OnPropertyChanged();
            }
        }
    }

    private string _selectedInputSyntax = "Auto";
    private string _selectedOutputSyntax = "Auto";

    public TextAnalysisResult Analysis
    {
        get => _analysis;
        private set
        {
            _analysis = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(InputStats));
            OnPropertyChanged(nameof(DetectedFormatBadge));
            OnPropertyChanged(nameof(EffectiveInputSyntax));
        }
    }

    public IReadOnlyList<string> AvailableSyntaxLanguages => DarkThemeHighlighting.SupportedLanguages;

    public string SelectedInputSyntax
    {
        get => _selectedInputSyntax;
        set
        {
            if (_selectedInputSyntax != value)
            {
                _selectedInputSyntax = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveInputSyntax));
            }
        }
    }

    public string SelectedOutputSyntax
    {
        get => _selectedOutputSyntax;
        set
        {
            if (_selectedOutputSyntax != value)
            {
                _selectedOutputSyntax = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveOutputSyntax));
            }
        }
    }

    public string EffectiveInputSyntax
    {
        get
        {
            if (!string.Equals(_selectedInputSyntax, "Auto", StringComparison.OrdinalIgnoreCase))
            {
                return _selectedInputSyntax;
            }

            return DetectSyntaxFromFormat(Analysis.Format, InputText);
        }
    }

    public string EffectiveOutputSyntax
    {
        get
        {
            if (!string.Equals(_selectedOutputSyntax, "Auto", StringComparison.OrdinalIgnoreCase))
            {
                return _selectedOutputSyntax;
            }

            return DetectSyntaxFromAction(_currentAction, OutputText);
        }
    }

    private static string DetectSyntaxFromFormat(DetectedFormat format, string text)
    {
        return format switch
        {
            DetectedFormat.Json => "JSON",
            DetectedFormat.Yaml => "YAML",
            DetectedFormat.CsvTable => "CSV",
            DetectedFormat.TsvTable => "TSV",
            DetectedFormat.HtmlTable => "HTML",
            DetectedFormat.MarkdownTable => "Markdown",
            DetectedFormat.SqlInClause => "SQL",
            _ => DetectSyntaxFromContent(text)
        };
    }

    private static string DetectSyntaxFromAction(string action, string text)
    {
        return action switch
        {
            "ToCsv" => "CSV",
            "ToTsv" => "TSV",
            "ToYaml" or "ToYamlObjects" or "ToYamlArrays" or "ToYamlArray" or "ToYamlList" or "KvToYaml" or "JsonToYaml" or "FormatYaml" or "TableToKeyValueYaml" or "ExtractSelectedToYaml" => "YAML",
            "SqlIn" or "SqlInMultiLine" or "ExtractSqlIn" => "SQL",
            "ToCSharpArray" or "ToCSharpList" or "EscapeCSharp" or "UnescapeCSharp" or "ExtractCSharpArray" => "C#",
            "ToTypeScriptArray" => "TypeScript",
            "ToPythonList" => "Python",
            "ToJsonArray" or "KvToJson" or "YamlToJson" or "FormatJson" or "JwtDecode" or "ExtractJsonMap" => "JSON",
            "FormatXml" => "XML",
            "ToMarkdownTable" => "Markdown",
            "ToHtmlTable" => "HTML",
            _ => DetectSyntaxFromContent(text)
        };
    }

    private static string DetectSyntaxFromContent(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "Plain Text";
        var trimmed = text.TrimStart();
        if (trimmed.StartsWith("{") || trimmed.StartsWith("[")) return "JSON";
        if (trimmed.StartsWith("---") || (trimmed.StartsWith("- ") && (trimmed.Contains('\n') || trimmed.Contains('\r')))) return "YAML";
        if (trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<table", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<div", StringComparison.OrdinalIgnoreCase)) return "HTML";
        if (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
            (trimmed.StartsWith("<") && trimmed.TrimEnd().EndsWith(">"))) return "XML";
        if (trimmed.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("INSERT ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("UPDATE ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("DELETE ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("IN (", StringComparison.OrdinalIgnoreCase)) return "SQL";

        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0 && lines[0].Contains('\t'))
        {
            return "TSV";
        }

        return "Plain Text";
    }

    public string DetectedFormatBadge => string.IsNullOrEmpty(Analysis.FormatDescription) ? "Text" : Analysis.FormatDescription;

    public DataTable? PreviewDataTable
    {
        get => _previewDataTable;
        private set
        {
            _previewDataTable = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasTabularData));
        }
    }

    public bool HasTabularData => Analysis.IsTabular && _currentTable != null && _currentTable.Columns.Count > 0;

    private int _selectedCenterTabIndex = 0;
    public int SelectedCenterTabIndex
    {
        get => _selectedCenterTabIndex;
        set
        {
            if (_selectedCenterTabIndex != value)
            {
                _selectedCenterTabIndex = value;
                OnPropertyChanged();
            }
        }
    }

    private int _selectedSidebarTabIndex = 0;
    public int SelectedSidebarTabIndex
    {
        get => _selectedSidebarTabIndex;
        set
        {
            if (_selectedSidebarTabIndex != value)
            {
                _selectedSidebarTabIndex = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isPresetsTabHighlighted;
    public bool IsPresetsTabHighlighted
    {
        get => _isPresetsTabHighlighted;
        private set
        {
            if (_isPresetsTabHighlighted != value)
            {
                _isPresetsTabHighlighted = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isLinesTabHighlighted;
    public bool IsLinesTabHighlighted
    {
        get => _isLinesTabHighlighted;
        private set
        {
            if (_isLinesTabHighlighted != value)
            {
                _isLinesTabHighlighted = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isTabularTabHighlighted;
    public bool IsTabularTabHighlighted
    {
        get => _isTabularTabHighlighted;
        private set
        {
            if (_isTabularTabHighlighted != value)
            {
                _isTabularTabHighlighted = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isCodeTabHighlighted;
    public bool IsCodeTabHighlighted
    {
        get => _isCodeTabHighlighted;
        private set
        {
            if (_isCodeTabHighlighted != value)
            {
                _isCodeTabHighlighted = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isCaseEncTabHighlighted;
    public bool IsCaseEncTabHighlighted
    {
        get => _isCaseEncTabHighlighted;
        private set
        {
            if (_isCaseEncTabHighlighted != value)
            {
                _isCaseEncTabHighlighted = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _hasHeaders = true;
    public bool HasHeaders
    {
        get => _hasHeaders;
        set
        {
            if (_hasHeaders != value)
            {
                _hasHeaders = value;
                OnPropertyChanged();
                OnHeadersOptionChanged();
            }
        }
    }

    public ObservableCollection<string> DetectedColumns => _detectedColumns;
    public ObservableCollection<ColumnItem> ColumnItems => _columnItems;

    public string? SelectedColumn
    {
        get => _selectedColumn;
        set
        {
            if (_selectedColumn != value)
            {
                _selectedColumn = value;
                OnPropertyChanged();
                TriggerRealTime("ExtractColumn");
            }
        }
    }

    public string? SelectedKeyColumn
    {
        get => _selectedKeyColumn;
        set
        {
            if (_selectedKeyColumn != value)
            {
                _selectedKeyColumn = value;
                OnPropertyChanged();
            }
        }
    }

    public string? SelectedValueColumn
    {
        get => _selectedValueColumn;
        set
        {
            if (_selectedValueColumn != value)
            {
                _selectedValueColumn = value;
                OnPropertyChanged();
            }
        }
    }

    public string TableExtractDelimiter
    {
        get => _tableExtractDelimiter;
        set
        {
            if (_tableExtractDelimiter != value)
            {
                _tableExtractDelimiter = value;
                OnPropertyChanged();
            }
        }
    }

    public string TableColumnPrefix
    {
        get => _tableColumnPrefix;
        set
        {
            if (_tableColumnPrefix != value)
            {
                _tableColumnPrefix = value;
                OnPropertyChanged();
            }
        }
    }

    public string TableColumnSuffix
    {
        get => _tableColumnSuffix;
        set
        {
            if (_tableColumnSuffix != value)
            {
                _tableColumnSuffix = value;
                OnPropertyChanged();
            }
        }
    }

    public string TableColumnFind
    {
        get => _tableColumnFind;
        set
        {
            if (_tableColumnFind != value)
            {
                _tableColumnFind = value;
                OnPropertyChanged();
            }
        }
    }

    public string TableColumnReplaceWith
    {
        get => _tableColumnReplaceWith;
        set
        {
            if (_tableColumnReplaceWith != value)
            {
                _tableColumnReplaceWith = value;
                OnPropertyChanged();
            }
        }
    }

    public string TableFilterQuery
    {
        get => _tableFilterQuery;
        set
        {
            if (_tableFilterQuery != value)
            {
                _tableFilterQuery = value;
                OnPropertyChanged();
            }
        }
    }

    public SortOrder TableSortOrder
    {
        get => _tableSortOrder;
        set
        {
            if (_tableSortOrder != value)
            {
                _tableSortOrder = value;
                OnPropertyChanged();
            }
        }
    }

    public string InputStats => $"{Analysis.CharacterCount} chars | {Analysis.LineCount} lines | {Analysis.WordCount} words | Format: {Analysis.FormatDescription}";
    public string OutputStats
    {
        get
        {
            int chars = _outputText.Length;
            int lines = string.IsNullOrEmpty(_outputText) ? 0 : _outputText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;
            return $"{chars} chars | {lines} lines";
        }
    }

    // Line Options Properties
    public QuoteStyle QuoteStyle { get => _quoteStyle; set { _quoteStyle = value; OnPropertyChanged(); TriggerRealTime("QuoteLines"); } }
    public string CustomQuotePrefix { get => _customQuotePrefix; set { _customQuotePrefix = value; OnPropertyChanged(); TriggerRealTime("QuoteLines"); } }
    public string CustomQuoteSuffix { get => _customQuoteSuffix; set { _customQuoteSuffix = value; OnPropertyChanged(); TriggerRealTime("QuoteLines"); } }
    public bool SkipEmptyLines { get => _skipEmptyLines; set { _skipEmptyLines = value; OnPropertyChanged(); TriggerRealTime(); } }
    public bool EscapeInnerQuotes { get => _escapeInnerQuotes; set { _escapeInnerQuotes = value; OnPropertyChanged(); TriggerRealTime("QuoteLines"); } }

    public string JoinDelimiter { get => _joinDelimiter; set { _joinDelimiter = value; OnPropertyChanged(); TriggerRealTime("JoinLines"); } }
    public QuoteStyle JoinItemQuote { get => _joinItemQuote; set { _joinItemQuote = value; OnPropertyChanged(); TriggerRealTime("JoinLines"); } }
    public string JoinOverallPrefix { get => _joinOverallPrefix; set { _joinOverallPrefix = value; OnPropertyChanged(); TriggerRealTime("JoinLines"); } }
    public string JoinOverallSuffix { get => _joinOverallSuffix; set { _joinOverallSuffix = value; OnPropertyChanged(); TriggerRealTime("JoinLines"); } }

    public string SplitDelimiter { get => _splitDelimiter; set { _splitDelimiter = value; OnPropertyChanged(); TriggerRealTime("SplitLine"); } }
    public bool SplitIsRegex { get => _splitIsRegex; set { _splitIsRegex = value; OnPropertyChanged(); TriggerRealTime("SplitLine"); } }
    public bool SplitTrimItems { get => _splitTrimItems; set { _splitTrimItems = value; OnPropertyChanged(); TriggerRealTime("SplitLine"); } }
    public bool SplitRemoveEmpty { get => _splitRemoveEmpty; set { _splitRemoveEmpty = value; OnPropertyChanged(); TriggerRealTime("SplitLine"); } }

    public SortOrder SortOrder { get => _sortOrder; set { _sortOrder = value; OnPropertyChanged(); TriggerRealTime("SortLines"); } }
    public DeduplicateMode DeduplicateMode { get => _deduplicateMode; set { _deduplicateMode = value; OnPropertyChanged(); TriggerRealTime("Deduplicate"); } }
    public bool CaseSensitiveDistinct { get => _caseSensitiveDistinct; set { _caseSensitiveDistinct = value; OnPropertyChanged(); TriggerRealTime("Deduplicate"); } }

    public bool TrimStart { get => _trimStart; set { _trimStart = value; OnPropertyChanged(); TriggerRealTime("TrimLines"); } }
    public bool TrimEnd { get => _trimEnd; set { _trimEnd = value; OnPropertyChanged(); TriggerRealTime("TrimLines"); } }
    public bool CollapseWhitespace { get => _collapseWhitespace; set { _collapseWhitespace = value; OnPropertyChanged(); TriggerRealTime("TrimLines"); } }
    public bool RemoveEmptyLines { get => _removeEmptyLines; set { _removeEmptyLines = value; OnPropertyChanged(); TriggerRealTime("TrimLines"); } }

    public string LinePrefix { get => _linePrefix; set { _linePrefix = value; OnPropertyChanged(); TriggerRealTime("PrefixSuffix"); } }
    public string LineSuffix { get => _lineSuffix; set { _lineSuffix = value; OnPropertyChanged(); TriggerRealTime("PrefixSuffix"); } }
    public bool LinePrefixSkipFirst { get => _linePrefixSkipFirst; set { _linePrefixSkipFirst = value; OnPropertyChanged(); TriggerRealTime("PrefixSuffix"); } }
    public bool LinePrefixSkipLast { get => _linePrefixSkipLast; set { _linePrefixSkipLast = value; OnPropertyChanged(); TriggerRealTime("PrefixSuffix"); } }
    public bool LineSuffixSkipFirst { get => _lineSuffixSkipFirst; set { _lineSuffixSkipFirst = value; OnPropertyChanged(); TriggerRealTime("PrefixSuffix"); } }
    public bool LineSuffixSkipLast { get => _lineSuffixSkipLast; set { _lineSuffixSkipLast = value; OnPropertyChanged(); TriggerRealTime("PrefixSuffix"); } }
    public string NumberFormat { get => _numberFormat; set { _numberFormat = value; OnPropertyChanged(); TriggerRealTime("NumberLines"); } }
    public int StartNumber { get => _startNumber; set { _startNumber = value; OnPropertyChanged(); TriggerRealTime("NumberLines"); } }

    public string FilterQuery { get => _filterQuery; set { _filterQuery = value; OnPropertyChanged(); TriggerRealTime("FilterLines"); } }
    public bool FilterIsRegex { get => _filterIsRegex; set { _filterIsRegex = value; OnPropertyChanged(); TriggerRealTime("FilterLines"); } }
    public bool FilterKeepMatching { get => _filterKeepMatching; set { _filterKeepMatching = value; OnPropertyChanged(); TriggerRealTime("FilterLines"); } }
    public bool FilterCaseSensitive { get => _filterCaseSensitive; set { _filterCaseSensitive = value; OnPropertyChanged(); TriggerRealTime("FilterLines"); } }

    public string LineFind { get => _lineFind; set { _lineFind = value; OnPropertyChanged(); TriggerRealTime("ReplaceInLines"); } }
    public string LineReplace { get => _lineReplace; set { _lineReplace = value; OnPropertyChanged(); TriggerRealTime("ReplaceInLines"); } }
    public bool LineReplaceIsRegex { get => _lineReplaceIsRegex; set { _lineReplaceIsRegex = value; OnPropertyChanged(); TriggerRealTime("ReplaceInLines"); } }
    public bool LineReplaceCaseSensitive { get => _lineReplaceCaseSensitive; set { _lineReplaceCaseSensitive = value; OnPropertyChanged(); TriggerRealTime("ReplaceInLines"); } }

    public string RegexExtractPattern { get => _regexExtractPattern; set { _regexExtractPattern = value; OnPropertyChanged(); TriggerRealTime("RegexExtract"); } }
    public int RegexCaptureGroup { get => _regexCaptureGroup; set { _regexCaptureGroup = value; OnPropertyChanged(); TriggerRealTime("RegexExtract"); } }

    public string SqlTableName { get => _sqlTableName; set { _sqlTableName = value; OnPropertyChanged(); TriggerRealTime("ToSqlInserts"); } }

    // History Properties
    public InputHistoryManager HistoryManager => _historyManager;
    public ObservableCollection<InputHistoryItem> HistoryItems => _historyManager.Items;
    public ICollectionView HistoryView => _historyView ??= CollectionViewSource.GetDefaultView(HistoryItems);
    public int HistoryCount => _historyManager.Count;
    public string HistoryTabHeader => $"History ({HistoryCount})";
    public bool CanHistoryBack => _historyManager.CanGoBack;
    public bool CanHistoryForward => _historyManager.CanGoForward;

    public InputHistoryItem? SelectedHistoryItem
    {
        get => _selectedHistoryItem ?? _historyManager.CurrentItem;
        set
        {
            if (_selectedHistoryItem != value)
            {
                _selectedHistoryItem = value;
                OnPropertyChanged();
            }
        }
    }

    public string HistorySearchQuery
    {
        get => _historySearchQuery;
        set
        {
            if (_historySearchQuery != value)
            {
                _historySearchQuery = value;
                OnPropertyChanged();
                ApplyHistoryFilter();
            }
        }
    }

    // Commands
    public ICommand ClearInputCommand { get; private set; } = null!;
    public ICommand LoadFileCommand { get; private set; } = null!;
    public ICommand PasteInputCommand { get; private set; } = null!;
    public ICommand PasteTableCommand { get; private set; } = null!;
    public ICommand CopyOutputCommand { get; private set; } = null!;
    public ICommand SendToInputCommand { get; private set; } = null!;
    public ICommand ExecuteTransformCommand { get; private set; } = null!;
    public ICommand LoadSampleCommand { get; private set; } = null!;
    public ICommand ActionCommand { get; private set; } = null!;

    /// <summary>
    /// Optional delegate to provide a file path for opening files (used for testing or custom dialog providers).
    /// </summary>
    public Func<string?>? OpenFileDialogProvider { get; set; }

    /// <summary>
    /// Loads input text directly from a file on disk, formats structured data if applicable, and updates history.
    /// </summary>
    /// <param name="filePath">The path to the file to load.</param>
    /// <returns>True if the file was loaded successfully; otherwise false.</returns>
    public bool LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        try
        {
            if (!File.Exists(filePath))
            {
                StatusMessage = $"File not found: {filePath}";
                return false;
            }

            string raw = File.ReadAllText(filePath);
            string formatted = TextBeautifier.Beautify(raw);
            InputText = formatted;

            string fileName = Path.GetFileName(filePath);
            long fileSize = new FileInfo(filePath).Length;
            RecordHistory(InputText, $"File: {fileName}");
            StatusMessage = $"Loaded file: {fileName} ({fileSize:N0} bytes)";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
            return false;
        }
    }

    // History Commands
    public ICommand HistoryBackCommand { get; private set; } = null!;
    public ICommand HistoryForwardCommand { get; private set; } = null!;
    public ICommand RestoreHistoryCommand { get; private set; } = null!;
    public ICommand DeleteHistoryCommand { get; private set; } = null!;
    public ICommand ClearHistoryCommand { get; private set; } = null!;
    public ICommand CopyHistoryCommand { get; private set; } = null!;
    public ICommand AppendHistoryCommand { get; private set; } = null!;
    public ICommand CreateSnapshotCommand { get; private set; } = null!;
    public ICommand ExportHistoryCommand { get; private set; } = null!;

    // Column Management Commands
    public ICommand SelectAllColumnsCommand { get; private set; } = null!;
    public ICommand DeselectAllColumnsCommand { get; private set; } = null!;
    public ICommand InvertColumnsCommand { get; private set; } = null!;

    public void RecordHistory(string text, string source = "Pasted")
    {
        if (string.IsNullOrEmpty(text) || _isNavigatingHistory) return;
        var item = _historyManager.AddEntry(text, source);
        if (item != null)
        {
            SelectedHistoryItem = item;
            UpdateHistoryProperties();
        }
    }

    private void UpdateHistoryProperties()
    {
        OnPropertyChanged(nameof(HistoryCount));
        OnPropertyChanged(nameof(HistoryTabHeader));
        OnPropertyChanged(nameof(CanHistoryBack));
        OnPropertyChanged(nameof(CanHistoryForward));
        OnPropertyChanged(nameof(SelectedHistoryItem));
        _historyView?.Refresh();
    }

    private void ApplyHistoryFilter()
    {
        if (_historyView == null) return;
        if (string.IsNullOrWhiteSpace(_historySearchQuery))
        {
            _historyView.Filter = null;
        }
        else
        {
            string query = _historySearchQuery.Trim();
            _historyView.Filter = obj =>
            {
                if (obj is InputHistoryItem item)
                {
                    return (item.FullText?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (item.Source?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (item.FormatName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (item.TimeDisplay?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
                }
                return false;
            };
        }
    }

    private void InitializeCommands()
    {
        ClearInputCommand = new RelayCommand(_ => InputText = string.Empty);
        LoadFileCommand = new RelayCommand(p =>
        {
            string? filePath = p as string;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                if (OpenFileDialogProvider != null)
                {
                    filePath = OpenFileDialogProvider();
                }
                else
                {
                    var dialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Title = "Open Input File",
                        Filter = "All Supported Files|*.txt;*.csv;*.tsv;*.tab;*.json;*.xml;*.html;*.htm;*.md;*.markdown;*.sql;*.log;*.dat;*.yaml;*.yml;*.ini;*.conf|Text Files (*.txt)|*.txt|Tabular Data (*.csv;*.tsv;*.tab)|*.csv;*.tsv;*.tab|JSON & XML Files (*.json;*.xml)|*.json;*.xml|Markdown (*.md;*.markdown)|*.md;*.markdown|HTML Files (*.html;*.htm)|*.html;*.htm|SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
                        CheckFileExists = true,
                        Multiselect = false
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        filePath = dialog.FileName;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                LoadFromFile(filePath);
            }
        });
        PasteInputCommand = new RelayCommand(_ =>
        {
            try
            {
                if (Clipboard.ContainsText(TextDataFormat.Html))
                {
                    string html = Clipboard.GetText(TextDataFormat.Html);
                    if (HtmlTableParser.IsHtmlTable(html))
                    {
                        string cleanTable = HtmlTableParser.ExtractTableHtml(html);
                        InputText = TextBeautifier.Beautify(cleanTable);
                        RecordHistory(InputText, "Pasted (HTML Table)");
                        StatusMessage = "Pasted table from clipboard";
                        return;
                    }
                }

                if (Clipboard.ContainsText())
                {
                    string raw = Clipboard.GetText();
                    InputText = TextBeautifier.Beautify(raw);
                    RecordHistory(InputText, "Pasted");
                    StatusMessage = "Pasted from clipboard";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Paste error: {ex.Message}";
            }
        });

        PasteTableCommand = PasteInputCommand;

        HistoryBackCommand = new RelayCommand(_ =>
        {
            if (_historyManager.CanGoBack)
            {
                _isNavigatingHistory = true;
                try
                {
                    var item = _historyManager.GoBack();
                    if (item != null)
                    {
                        InputText = item.FullText;
                        SelectedHistoryItem = item;
                        UpdateHistoryProperties();
                        StatusMessage = $"Restored state from {item.TimeDisplay} ({item.Source})";
                    }
                }
                finally
                {
                    _isNavigatingHistory = false;
                }
            }
        }, _ => _historyManager.CanGoBack);

        HistoryForwardCommand = new RelayCommand(_ =>
        {
            if (_historyManager.CanGoForward)
            {
                _isNavigatingHistory = true;
                try
                {
                    var item = _historyManager.GoForward();
                    if (item != null)
                    {
                        InputText = item.FullText;
                        SelectedHistoryItem = item;
                        UpdateHistoryProperties();
                        StatusMessage = $"Restored state from {item.TimeDisplay} ({item.Source})";
                    }
                }
                finally
                {
                    _isNavigatingHistory = false;
                }
            }
        }, _ => _historyManager.CanGoForward);

        RestoreHistoryCommand = new RelayCommand(p =>
        {
            var item = p as InputHistoryItem ?? SelectedHistoryItem ?? _historyManager.CurrentItem;
            if (item != null)
            {
                _isNavigatingHistory = true;
                try
                {
                    _historyManager.Restore(item);
                    InputText = item.FullText;
                    SelectedHistoryItem = item;
                    UpdateHistoryProperties();
                    StatusMessage = $"Restored snapshot from {item.TimeDisplay} ({item.Source})";
                }
                finally
                {
                    _isNavigatingHistory = false;
                }
            }
        });

        DeleteHistoryCommand = new RelayCommand(p =>
        {
            var item = p as InputHistoryItem ?? SelectedHistoryItem;
            if (item != null)
            {
                _historyManager.Delete(item);
                UpdateHistoryProperties();
                StatusMessage = "Removed item from history";
            }
        });

        ClearHistoryCommand = new RelayCommand(_ =>
        {
            _historyManager.Clear();
            UpdateHistoryProperties();
            StatusMessage = "History timeline cleared";
        });

        CopyHistoryCommand = new RelayCommand(p =>
        {
            var item = p as InputHistoryItem ?? SelectedHistoryItem;
            if (item != null && !string.IsNullOrEmpty(item.FullText))
            {
                Clipboard.SetText(item.FullText);
                StatusMessage = $"Copied {item.TimeDisplay} snapshot to clipboard";
            }
        });

        AppendHistoryCommand = new RelayCommand(p =>
        {
            var item = p as InputHistoryItem ?? SelectedHistoryItem;
            if (item != null && !string.IsNullOrEmpty(item.FullText))
            {
                if (string.IsNullOrEmpty(InputText))
                {
                    InputText = item.FullText;
                }
                else
                {
                    InputText = InputText + Environment.NewLine + item.FullText;
                }
                RecordHistory(InputText, "Appended from History");
                StatusMessage = $"Appended snapshot {item.TimeDisplay} to input";
            }
        });

        CreateSnapshotCommand = new RelayCommand(_ =>
        {
            if (!string.IsNullOrEmpty(InputText))
            {
                RecordHistory(InputText, "Snapshot");
                StatusMessage = "Saved current input to timeline";
            }
        });

        ExportHistoryCommand = new RelayCommand(_ =>
        {
            if (HistoryItems.Count == 0)
            {
                StatusMessage = "History is empty";
                return;
            }
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== TEXTFORGE INPUT HISTORY TIMELINE ===");
            sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Snapshots: {HistoryItems.Count}");
            sb.AppendLine(new string('=', 40));
            for (int i = 0; i < HistoryItems.Count; i++)
            {
                var h = HistoryItems[i];
                sb.AppendLine($"[#{i + 1}] {h.TimeDisplay} | {h.Source} | {h.FormatName} | {h.SizeDisplay}");
                sb.AppendLine(h.FullText);
                sb.AppendLine(new string('-', 40));
            }
            Clipboard.SetText(sb.ToString());
            StatusMessage = "Exported history timeline report to clipboard";
        });

        CopyOutputCommand = new RelayCommand(_ =>
        {
            if (!string.IsNullOrEmpty(OutputText))
            {
                Clipboard.SetText(OutputText);
                StatusMessage = "Copied output to clipboard";
            }
        });

        SendToInputCommand = new RelayCommand(_ =>
        {
            if (!string.IsNullOrEmpty(OutputText))
            {
                InputText = OutputText;
                RecordHistory(InputText, "Output → Input");
                StatusMessage = "Sent output to input";
            }
        });

        ExecuteTransformCommand = new RelayCommand(_ => ExecuteCurrentAction());

        SelectAllColumnsCommand = new RelayCommand(_ =>
        {
            foreach (var col in ColumnItems) col.IsSelected = true;
            StatusMessage = "All columns selected";
        });

        DeselectAllColumnsCommand = new RelayCommand(_ =>
        {
            foreach (var col in ColumnItems) col.IsSelected = false;
            StatusMessage = "All columns deselected";
        });

        InvertColumnsCommand = new RelayCommand(_ =>
        {
            foreach (var col in ColumnItems) col.IsSelected = !col.IsSelected;
            StatusMessage = "Column selection inverted";
        });

        LoadSampleCommand = new RelayCommand(p =>
        {
            string sampleType = p?.ToString() ?? "numbers";
            InputText = sampleType switch
            {
                "html" => "<table class=\"confluenceTable\">\n  <thead>\n    <tr>\n      <th>ID</th>\n      <th>Full Name</th>\n      <th>Department</th>\n      <th>Email</th>\n      <th>Status</th>\n    </tr>\n  </thead>\n  <tbody>\n    <tr>\n      <td>101</td>\n      <td>Alice Smith</td>\n      <td>Engineering</td>\n      <td>alice@example.com</td>\n      <td>Active</td>\n    </tr>\n    <tr>\n      <td>102</td>\n      <td>Bob Jones</td>\n      <td>Design</td>\n      <td>bob@example.com</td>\n      <td>Active</td>\n    </tr>\n    <tr>\n      <td>103</td>\n      <td>Charlie Brown</td>\n      <td>Engineering</td>\n      <td>charlie@example.com</td>\n      <td>Pending</td>\n    </tr>\n    <tr>\n      <td>104</td>\n      <td>Diana Prince</td>\n      <td>Marketing</td>\n      <td>diana@example.com</td>\n      <td>Inactive</td>\n    </tr>\n  </tbody>\n</table>",
                "numbers" => "101\n102\n103\n104\n105\n106\n107\n108\n109\n110",
                "csv" => "Id,Name,Role,Salary,Department\n1,Alice,Architect,120000,Engineering\n2,Bob,Developer,95000,Engineering\n3,Charlie,Designer,85000,Product\n4,Diana,Manager,110000,Sales\n5,Evan,DevOps,105000,Engineering",
                "tsv" => "OrderId\tCustomer\tProduct\tQuantity\tPrice\n1001\tAcme Corp\tWidget A\t5\t19.99\n1002\tGlobex\tGadget Pro\t2\t49.99\n1003\tSoylent\tWidget A\t10\t19.99\n1004\tInitech\tService Plan\t1\t99.00",
                "markdown" => "| ID | Server Name | IP Address | Environment | Status |\n|---|---|---|---|---|\n| 1 | web-prod-01 | 10.0.1.15 | Production | Online |\n| 2 | web-prod-02 | 10.0.1.16 | Production | Online |\n| 3 | db-prod-01 | 10.0.2.10 | Production | Online |\n| 4 | api-stage-01 | 10.0.3.5 | Staging | Maintenance |",
                "json" => "[\n  {\"id\": 1, \"name\": \"Development\", \"active\": true},\n  {\"id\": 2, \"name\": \"Staging\", \"active\": true},\n  {\"id\": 3, \"name\": \"Production\", \"active\": false}\n]",
                "yaml" => "- id: 1\n  name: Development\n  active: true\n  department: Engineering\n- id: 2\n  name: Staging\n  active: true\n  department: QA\n- id: 3\n  name: Production\n  active: false\n  department: Operations",
                "delimited" => "apple, banana, cherry, date, elderberry, fig, grape",
                "query" => "userId=42&view=summary&filter=active&pageSize=50&sortBy=createdAt&sortDir=desc",
                _ => "Item 1\nItem 2\nItem 3"
            };
            RecordHistory(InputText, $"Sample ({sampleType})");
            StatusMessage = $"Loaded sample: {sampleType}";
        });

        ActionCommand = new RelayCommand(p =>
        {
            string action = p?.ToString() ?? "SqlIn";
            _currentAction = action;
            ExecuteCurrentAction();
        });
    }

    private void AnalyzeInput()
    {
        // Update tabular preview with auto-detected headers
        _currentTable = TabularParser.DetectAndParse(_inputText);
        if (_currentTable != null && _currentTable.Columns.Count > 0)
        {
            _hasHeaders = _currentTable.HasHeaders;
            OnPropertyChanged(nameof(HasHeaders));
            UpdateDataTable(_currentTable);

            DetectedColumns.Clear();
            ColumnItems.Clear();

            for (int i = 0; i < _currentTable.Columns.Count; i++)
            {
                string col = _currentTable.Columns[i];
                DetectedColumns.Add(col);

                string sample = _currentTable.Rows.Count > 0 && i < _currentTable.Rows[0].Count ? _currentTable.Rows[0][i] : string.Empty;
                ColumnItems.Add(new ColumnItem
                {
                    Index = i,
                    Name = col,
                    SampleValue = sample,
                    IsSelected = true
                });
            }

            if (DetectedColumns.Count > 0)
            {
                if (string.IsNullOrEmpty(SelectedColumn) || !DetectedColumns.Contains(SelectedColumn))
                {
                    SelectedColumn = DetectedColumns[0];
                }
                if (string.IsNullOrEmpty(SelectedKeyColumn) || !DetectedColumns.Contains(SelectedKeyColumn))
                {
                    SelectedKeyColumn = DetectedColumns[0];
                }
                if (string.IsNullOrEmpty(SelectedValueColumn) || !DetectedColumns.Contains(SelectedValueColumn))
                {
                    SelectedValueColumn = DetectedColumns.Count > 1 ? DetectedColumns[1] : DetectedColumns[0];
                }
            }
        }
        else
        {
            PreviewDataTable = null;
            DetectedColumns.Clear();
            ColumnItems.Clear();
            SelectedColumn = null;
            SelectedKeyColumn = null;
            SelectedValueColumn = null;
        }

        Analysis = TextAnalyzer.Analyze(_inputText, _currentTable?.HasHeaders);
        OnPropertyChanged(nameof(HasTabularData));

        if (!HasTabularData)
        {
            SelectedCenterTabIndex = 1; // Analysis & Stats
        }
        else
        {
            SelectedCenterTabIndex = 0; // Table Grid View
        }

        UpdateTabHighlights();
    }

    private void UpdateTabHighlights()
    {
        if (string.IsNullOrWhiteSpace(_inputText))
        {
            IsPresetsTabHighlighted = false;
            IsLinesTabHighlighted = false;
            IsTabularTabHighlighted = false;
            IsCodeTabHighlighted = false;
            IsCaseEncTabHighlighted = false;
            return;
        }

        bool isTabular = Analysis.IsTabular;
        bool isMultiLine = Analysis.NonEmptyLineCount > 1 || Analysis.LineCount > 1;
        bool isDelimitedSingle = Analysis.Format == DetectedFormat.DelimitedSingleLine;
        bool isCodeOrStructured = Analysis.Format == DetectedFormat.Json ||
                                 Analysis.Format == DetectedFormat.Yaml ||
                                 Analysis.Format == DetectedFormat.SqlInClause ||
                                 Analysis.Format == DetectedFormat.KeyValuePairs ||
                                 IsCodeLikeContent(_inputText);
        bool isSingleLineOrToken = Analysis.NonEmptyLineCount <= 1 && !isTabular && !isDelimitedSingle;

        // 1. Tabular Tab
        IsTabularTabHighlighted = isTabular;

        // 2. Lines Tab (relevant for any multiline text, delimited lines, or lists of items)
        IsLinesTabHighlighted = !isTabular && (isMultiLine || isDelimitedSingle);

        // 3. Code Tab (relevant for JSON, SQL clauses, key-values, query strings, code-like content)
        IsCodeTabHighlighted = isCodeOrStructured;

        // 4. Case / Enc Tab (relevant for single line text, words/tokens, base64, url-encoded, beautifiable formats)
        IsCaseEncTabHighlighted = isSingleLineOrToken ||
                                  Analysis.Format == DetectedFormat.Base64 ||
                                  Analysis.Format == DetectedFormat.UrlEncoded ||
                                  TextBeautifier.CanBeautify(_inputText);

        // 5. Presets Tab (useful for all non-empty text, especially multiline, delimited, or tabular extractions)
        IsPresetsTabHighlighted = isMultiLine || isDelimitedSingle || isTabular;

        // Auto-select the most relevant sidebar tab
        if (isTabular)
        {
            SelectedSidebarTabIndex = 2; // Tabular
        }
        else if (isCodeOrStructured && (Analysis.Format == DetectedFormat.Json || Analysis.Format == DetectedFormat.Yaml || Analysis.Format == DetectedFormat.SqlInClause || Analysis.Format == DetectedFormat.KeyValuePairs))
        {
            SelectedSidebarTabIndex = 3; // Code
        }
        else if (isMultiLine || isDelimitedSingle)
        {
            SelectedSidebarTabIndex = 1; // Lines
        }
        else if (isSingleLineOrToken)
        {
            SelectedSidebarTabIndex = 4; // Case / Enc
        }
    }

    private static bool IsCodeLikeContent(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string trimmed = text.Trim();

        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.EndsWith(']')) ||
            (trimmed.StartsWith('<') && trimmed.EndsWith('>')) ||
            trimmed.StartsWith("---") ||
            (trimmed.StartsWith("- ") && (trimmed.Contains('\n') || trimmed.Contains('\r'))))
        {
            return true;
        }

        if (trimmed.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("INSERT INTO ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("UPDATE ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("DELETE FROM ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("IN (", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (trimmed.Contains('&') && trimmed.Contains('='))
        {
            return true;
        }

        return false;
    }

    private void OnHeadersOptionChanged()
    {
        if (string.IsNullOrWhiteSpace(_inputText)) return;

        _currentTable = TabularParser.DetectAndParse(_inputText, _hasHeaders);
        if (_currentTable != null && _currentTable.Columns.Count > 0)
        {
            UpdateDataTable(_currentTable);

            DetectedColumns.Clear();
            ColumnItems.Clear();

            for (int i = 0; i < _currentTable.Columns.Count; i++)
            {
                string col = _currentTable.Columns[i];
                DetectedColumns.Add(col);

                string sample = _currentTable.Rows.Count > 0 && i < _currentTable.Rows[0].Count ? _currentTable.Rows[0][i] : string.Empty;
                ColumnItems.Add(new ColumnItem
                {
                    Index = i,
                    Name = col,
                    SampleValue = sample,
                    IsSelected = true
                });
            }

            if (DetectedColumns.Count > 0)
            {
                if (string.IsNullOrEmpty(SelectedColumn) || !DetectedColumns.Contains(SelectedColumn))
                {
                    SelectedColumn = DetectedColumns[0];
                }
                if (string.IsNullOrEmpty(SelectedKeyColumn) || !DetectedColumns.Contains(SelectedKeyColumn))
                {
                    SelectedKeyColumn = DetectedColumns[0];
                }
                if (string.IsNullOrEmpty(SelectedValueColumn) || !DetectedColumns.Contains(SelectedValueColumn))
                {
                    SelectedValueColumn = DetectedColumns.Count > 1 ? DetectedColumns[1] : DetectedColumns[0];
                }
            }
        }
        else
        {
            PreviewDataTable = null;
            DetectedColumns.Clear();
            ColumnItems.Clear();
        }

        Analysis = TextAnalyzer.Analyze(_inputText, _hasHeaders);
        OnPropertyChanged(nameof(HasTabularData));
        UpdateTabHighlights();
        TriggerRealTime();
    }

    private void UpdateDataTable(TabularData table)
    {
        var dt = new DataTable();
        var uniqueNames = new HashSet<string>();

        for (int i = 0; i < table.Columns.Count; i++)
        {
            string colName = table.Columns[i];
            if (string.IsNullOrWhiteSpace(colName)) colName = $"Column_{i + 1}";
            string uniqueCol = colName;
            int counter = 2;
            while (uniqueNames.Contains(uniqueCol))
            {
                uniqueCol = $"{colName}_{counter++}";
            }
            uniqueNames.Add(uniqueCol);
            dt.Columns.Add(uniqueCol, typeof(string));
        }

        foreach (var row in table.Rows)
        {
            var dr = dt.NewRow();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                dr[i] = i < row.Count ? row[i] : string.Empty;
            }
            dt.Rows.Add(dr);
        }

        PreviewDataTable = dt;
    }

    private void TriggerRealTime(string? specificAction = null)
    {
        if (specificAction != null)
        {
            _currentAction = specificAction;
        }
        if (IsRealTimeTransform)
        {
            ExecuteCurrentAction(autoSendToInput: false);
        }
    }

    public void ExecuteCurrentAction(bool autoSendToInput = true)
    {
        if (string.IsNullOrEmpty(InputText))
        {
            OutputText = string.Empty;
            StatusMessage = "Ready (Input empty)";
            return;
        }

        try
        {
            OutputText = _currentAction switch
            {
                "SqlIn" => DeveloperTransformers.ToSqlInClause(InputText),
                "SqlInMultiLine" => DeveloperTransformers.ToSqlInClause(InputText, multiLine: true),
                "JoinLines" => LineTransformers.JoinLines(InputText, JoinDelimiter, JoinItemQuote, overallPrefix: JoinOverallPrefix, overallSuffix: JoinOverallSuffix, skipEmpty: SkipEmptyLines),
                "JoinComma" => LineTransformers.JoinLines(InputText, ", ", QuoteStyle.None),
                "QuoteLines" => LineTransformers.QuoteLines(InputText, QuoteStyle, CustomQuotePrefix, CustomQuoteSuffix, SkipEmptyLines, EscapeInnerQuotes),
                "QuoteSingle" => LineTransformers.QuoteLines(InputText, QuoteStyle.SingleQuotes),
                "QuoteDouble" => LineTransformers.QuoteLines(InputText, QuoteStyle.DoubleQuotes),
                "QuoteBackticks" => LineTransformers.QuoteLines(InputText, QuoteStyle.Backticks),
                "SplitLine" => LineTransformers.SplitLine(InputText, SplitDelimiter, SplitIsRegex, SplitTrimItems, SplitRemoveEmpty),
                "TrimLines" => LineTransformers.TrimLines(InputText, TrimStart, TrimEnd, RemoveEmptyLines, CollapseWhitespace),
                "SortLines" => LineTransformers.SortLines(InputText, SortOrder),
                "SortNatural" => LineTransformers.SortLines(InputText, SortOrder.NaturalNumericAsc),
                "SortAlphabetical" => LineTransformers.SortLines(InputText, SortOrder.AlphabeticalAsc),
                "Deduplicate" => LineTransformers.DeduplicateLines(InputText, DeduplicateMode, CaseSensitiveDistinct),
                "PrefixSuffix" => LineTransformers.AddPrefixSuffix(InputText, LinePrefix, LineSuffix, SkipEmptyLines, LinePrefixSkipFirst, LinePrefixSkipLast, LineSuffixSkipFirst, LineSuffixSkipLast),
                "NumberLines" => LineTransformers.NumberLines(InputText, NumberFormat, StartNumber, SkipEmptyLines),
                "FilterLines" => LineTransformers.FilterLines(InputText, FilterQuery, FilterIsRegex, FilterKeepMatching, FilterCaseSensitive),
                "ReplaceInLines" => LineTransformers.ReplaceInLines(InputText, LineFind, LineReplace, LineReplaceIsRegex, LineReplaceCaseSensitive, SkipEmptyLines),
                "RegexExtract" => LineTransformers.ExtractRegex(InputText, RegexExtractPattern, RegexCaptureGroup),

                // Tabular Whole Table Conversions
                "ToMarkdownTable" => ConvertTabular(t => TabularConverter.ToMarkdownTable(t)),
                "ToCsv" => ConvertTabular(t => TabularConverter.ToCsv(t, ',')),
                "ToTsv" => ConvertTabular(t => TabularConverter.ToTsv(t)),
                "ToYaml" or "ToYamlObjects" => ConvertTabular(t => TabularConverter.ToYaml(t)),
                "ToYamlArrays" => ConvertTabular(t => TabularConverter.ToYamlArrays(t)),
                "ToJsonObjects" => ConvertTabular(t => TabularConverter.ToJsonArrayOfObjects(t)),
                "ToJsonArrays" => ConvertTabular(t => TabularConverter.ToJsonArrayOfArrays(t)),
                "ToSqlInserts" => ConvertTabular(t => TabularConverter.ToSqlInsertStatements(t, SqlTableName)),
                "ToHtmlTable" => ConvertTabular(t => TabularConverter.ToHtmlTable(t)),
                "TransposeTable" => ConvertTabular(t => TabularConverter.ToCsv(t.Transpose(), t.Delimiter ?? ',')),
                "ExtractColumn" => ExtractSelectedColumn(),

                // Tabular Column Selection & Break Apart Transforms
                "ExtractSelectedToCsv" => ExtractSelectedColumnsAsTable(t => TabularConverter.ToCsv(t, ',')),
                "ExtractSelectedToTsv" => ExtractSelectedColumnsAsTable(t => TabularConverter.ToTsv(t)),
                "ExtractSelectedToMarkdown" => ExtractSelectedColumnsAsTable(t => TabularConverter.ToMarkdownTable(t)),
                "ExtractSelectedToJson" => ExtractSelectedColumnsAsTable(t => TabularConverter.ToJsonArrayOfObjects(t)),
                "ExtractSelectedToYaml" => ExtractSelectedColumnsAsTable(t => TabularConverter.ToYaml(t)),
                "ExtractSelectedToLines" => ExtractSelectedColumnsAsLines(),
                "ExtractSelectedToSqlIn" => ExtractSelectedColumnsAsSqlIn(),
                "ExtractSelectedToCodeArray" => ExtractSelectedColumnsAsCodeArray(),
                "KeepOnlySelectedColumns" => KeepOnlySelectedColumns(),
                "DropSelectedColumns" => DropSelectedColumns(),
                "SortTableByColumn" => SortTableBySelectedColumn(),
                "FilterTableByColumn" => FilterTableBySelectedColumn(),
                "TransformSelectedUpper" => TransformSelectedColumns(s => s.ToUpperInvariant()),
                "TransformSelectedLower" => TransformSelectedColumns(s => s.ToLowerInvariant()),
                "TransformSelectedTrim" => TransformSelectedColumns(s => s.Trim()),
                "TransformSelectedPrefixSuffix" => TransformSelectedColumns(s => $"{TableColumnPrefix}{s}{TableColumnSuffix}"),
                "TransformSelectedReplace" => TransformSelectedColumns(s => string.IsNullOrEmpty(TableColumnFind) ? s : s.Replace(TableColumnFind, TableColumnReplaceWith)),
                "TableToKeyValueJson" => GenerateKeyValue(t => TabularConverter.ToKeyValueJson(t, GetKeyColIdx(), GetValColIdx())),
                "TableToKeyValueYaml" => GenerateKeyValue(t => TabularConverter.ToKeyValueYaml(t, GetKeyColIdx(), GetValColIdx())),
                "TableToKeyValueQuery" => GenerateKeyValue(t => TabularConverter.ToKeyValueQueryString(t, GetKeyColIdx(), GetValColIdx())),

                // Developer & Code
                "ToCSharpArray" => DeveloperTransformers.ToCSharpArray(InputText),
                "ToCSharpList" => DeveloperTransformers.ToCSharpArray(InputText, asList: true),
                "ToTypeScriptArray" => DeveloperTransformers.ToTypeScriptArray(InputText),
                "ToPythonList" => DeveloperTransformers.ToPythonList(InputText),
                "ToJsonArray" => DeveloperTransformers.ToJsonArray(InputText),
                "ToYamlArray" or "ToYamlList" => DeveloperTransformers.ToYamlArray(InputText),
                "QueryStringToKv" => DeveloperTransformers.QueryStringToKeyValuePairs(InputText),
                "KvToQueryString" => DeveloperTransformers.KeyValuePairsToQueryString(InputText),
                "KvToJson" => DeveloperTransformers.KeyValuePairsToJson(InputText),
                "KvToYaml" => DeveloperTransformers.KeyValuePairsToYaml(InputText),
                "JsonToYaml" => DeveloperTransformers.JsonToYaml(InputText),
                "YamlToJson" => DeveloperTransformers.YamlToJson(InputText),

                // Case
                "CamelCase" => CaseTransformers.ChangeCase(InputText, TextCasing.CamelCase),
                "PascalCase" => CaseTransformers.ChangeCase(InputText, TextCasing.PascalCase),
                "SnakeCase" => CaseTransformers.ChangeCase(InputText, TextCasing.SnakeCase),
                "KebabCase" => CaseTransformers.ChangeCase(InputText, TextCasing.KebabCase),
                "ConstantCase" => CaseTransformers.ChangeCase(InputText, TextCasing.ConstantCase),
                "TitleCase" => CaseTransformers.ChangeCase(InputText, TextCasing.TitleCase),
                "UpperCase" => CaseTransformers.ChangeCase(InputText, TextCasing.UpperCase),
                "LowerCase" => CaseTransformers.ChangeCase(InputText, TextCasing.LowerCase),

                // Encodings
                "UrlEncode" => EncodingTransformers.UrlEncode(InputText),
                "UrlDecode" => EncodingTransformers.UrlDecode(InputText),
                "Base64Encode" => EncodingTransformers.Base64Encode(InputText),
                "Base64Decode" => EncodingTransformers.Base64Decode(InputText),
                "HtmlEncode" => EncodingTransformers.HtmlEncode(InputText),
                "HtmlDecode" => EncodingTransformers.HtmlDecode(InputText),
                "EscapeCSharp" => EncodingTransformers.EscapeCSharpString(InputText),
                "UnescapeCSharp" => EncodingTransformers.UnescapeCSharpString(InputText),
                "FormatJson" => EncodingTransformers.FormatJsonString(InputText),
                "FormatXml" => EncodingTransformers.FormatXmlString(InputText),
                "FormatYaml" => EncodingTransformers.FormatYamlString(InputText),
                "Beautify" => TextBeautifier.Beautify(InputText),
                "JwtDecode" => EncodingTransformers.JwtDecode(InputText),

                _ => InputText
            };

            StatusMessage = $"Executed: {_currentAction}";

            if (autoSendToInput && AutoSendOutputToInput && !string.IsNullOrEmpty(OutputText) && OutputText != InputText)
            {
                _isAutoSendingOutput = true;
                try
                {
                    InputText = OutputText;
                    RecordHistory(InputText, $"{_currentAction} → Input");
                }
                finally
                {
                    _isAutoSendingOutput = false;
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private List<int> GetSelectedColumnIndices()
    {
        var selected = ColumnItems.Where(c => c.IsSelected).Select(c => c.Index).ToList();
        if (selected.Count == 0 && _currentTable != null && _currentTable.Columns.Count > 0)
        {
            // Default to single selected column or first column
            int idx = GetSelectedColumnIndex();
            selected.Add(idx);
        }
        return selected;
    }

    private int GetSelectedColumnIndex()
    {
        if (_currentTable == null || _currentTable.Columns.Count == 0) return 0;
        if (!string.IsNullOrEmpty(SelectedColumn))
        {
            int idx = _currentTable.Columns.IndexOf(SelectedColumn);
            if (idx >= 0) return idx;
        }
        return 0;
    }

    private int GetKeyColIdx()
    {
        if (_currentTable == null || _currentTable.Columns.Count == 0) return 0;
        if (!string.IsNullOrEmpty(SelectedKeyColumn))
        {
            int idx = _currentTable.Columns.IndexOf(SelectedKeyColumn);
            if (idx >= 0) return idx;
        }
        return 0;
    }

    private int GetValColIdx()
    {
        if (_currentTable == null || _currentTable.Columns.Count == 0) return 0;
        if (!string.IsNullOrEmpty(SelectedValueColumn))
        {
            int idx = _currentTable.Columns.IndexOf(SelectedValueColumn);
            if (idx >= 0) return idx;
        }
        return _currentTable.Columns.Count > 1 ? 1 : 0;
    }

    private string ExtractSelectedColumnsAsTable(Func<TabularData, string> formatter)
    {
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || table.Columns.Count == 0) return InputText;

        var indices = GetSelectedColumnIndices();
        var subTable = table.SelectColumns(indices);
        return formatter(subTable);
    }

    private string ExtractSelectedColumnsAsLines()
    {
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || table.Columns.Count == 0) return InputText;

        var indices = GetSelectedColumnIndices();
        string delim = string.IsNullOrEmpty(TableExtractDelimiter) ? "\t" : TableExtractDelimiter;
        var lines = table.ExtractColumnsAsLines(indices, delim);
        return string.Join(Environment.NewLine, lines);
    }

    private string ExtractSelectedColumnsAsSqlIn()
    {
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || table.Columns.Count == 0) return DeveloperTransformers.ToSqlInClause(InputText);

        var indices = GetSelectedColumnIndices();
        int colIdx = indices.Count > 0 ? indices[0] : 0;
        var items = table.ExtractColumn(colIdx).Where(s => !string.IsNullOrWhiteSpace(s));
        return DeveloperTransformers.ToSqlInClause(string.Join(Environment.NewLine, items));
    }

    private string ExtractSelectedColumnsAsCodeArray()
    {
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || table.Columns.Count == 0) return DeveloperTransformers.ToCSharpArray(InputText);

        var indices = GetSelectedColumnIndices();
        int colIdx = indices.Count > 0 ? indices[0] : 0;
        var items = table.ExtractColumn(colIdx).Where(s => !string.IsNullOrWhiteSpace(s));
        return DeveloperTransformers.ToCSharpArray(string.Join(Environment.NewLine, items));
    }

    private string KeepOnlySelectedColumns()
    {
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || table.Columns.Count == 0) return InputText;

        var indices = GetSelectedColumnIndices();
        var subTable = table.SelectColumns(indices);
        return TabularConverter.ToMarkdownTable(subTable);
    }

    private string DropSelectedColumns()
    {
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || table.Columns.Count == 0) return InputText;

        var indices = GetSelectedColumnIndices();
        var subTable = table.DropColumns(indices);
        return TabularConverter.ToMarkdownTable(subTable);
    }

    private string SortTableBySelectedColumn()
    {
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || table.Columns.Count == 0) return InputText;

        int colIdx = GetSelectedColumnIndex();
        var sorted = table.SortByColumn(colIdx, TableSortOrder);
        return TabularConverter.ToMarkdownTable(sorted);
    }

    private string FilterTableBySelectedColumn()
    {
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || table.Columns.Count == 0) return InputText;

        int colIdx = GetSelectedColumnIndex();
        var filtered = table.FilterRows(colIdx, TableFilterQuery);
        return TabularConverter.ToMarkdownTable(filtered);
    }

    private string TransformSelectedColumns(Func<string, string> transform)
    {
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || table.Columns.Count == 0) return InputText;

        var indices = GetSelectedColumnIndices();
        var transformed = table.TransformColumns(indices, transform);
        return TabularConverter.ToMarkdownTable(transformed);
    }

    private string GenerateKeyValue(Func<TabularData, string> generator)
    {
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || table.Columns.Count == 0) return InputText;

        return generator(table);
    }

    private string ConvertTabular(Func<TabularData, string> converter)
    {
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || (table.Columns.Count == 0 && table.Rows.Count == 0))
        {
            // Try parsing lines as single column table
            var lines = InputText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            table = new TabularData
            {
                Columns = new List<string> { "Value" },
                Rows = lines.Select(l => new List<string> { l.Trim() }).ToList(),
                HasHeaders = true
            };
        }
        return converter(table);
    }

    private string ExtractSelectedColumn()
    {
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || table.Columns.Count == 0) return InputText;

        int idx = GetSelectedColumnIndex();
        var colValues = table.ExtractColumn(idx);
        return string.Join(Environment.NewLine, colValues.Where(v => !string.IsNullOrEmpty(v)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
