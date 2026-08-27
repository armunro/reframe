using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Reframe.Core.Actions;
using Reframe.Core.Analysis;
using Reframe.Core.Analysis.Analyzers;
using Reframe.Core.Analysis.Models;
using Reframe.Core.History;
using Reframe.Core.Recipes;
using Reframe.Core.RegexLab;
using Reframe.Core.Structured;
using Reframe.Core.Structured.Models;
using Reframe.Core.Structured.Parsers;
using Reframe.Core.Tabular;
using Reframe.Core.Tabular.Converters;
using Reframe.Core.Tabular.Models;
using Reframe.Core.Tabular.Parsers;
using Reframe.Core.Transformers;
using Reframe.Core.Transformers.Case;
using Reframe.Core.Transformers.Developer;
using Reframe.Core.Transformers.Encoding;
using Reframe.Core.Transformers.Formatting;
using Reframe.Core.Transformers.Line;
using Reframe.Highlighting;
using Reframe.Services;

namespace Reframe.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private string _inputText = string.Empty;
    private string _outputText = string.Empty;
    private string _statusMessage = "Ready";
    private bool _isRealTimeTransform = true;
    private bool _isWordWrap = false;
    private bool _autoSendOutputToInput = false;
    private bool _isAutoSendingOutput = false;
    private bool _watchClipboard = false;
    private string? _lastProcessedClipboardText;
    private IClipboardWatcher? _clipboardWatcher;
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
    private string _surrogateHeaders = "";
    private ObservableCollection<string> _detectedColumns = new();
    private ObservableCollection<ColumnItem> _columnItems = new();
    private string? _selectedColumn;
    private string? _selectedKeyColumn;
    private string? _selectedValueColumn;
    private bool _keyValueIncludeRestOfColumns = false;
    private string _tableExtractDelimiter = ", ";
    private string _tableColumnPrefix = "";
    private string _tableColumnSuffix = "";
    private string _tableColumnFind = "";
    private string _tableColumnReplaceWith = "";
    private string _tableFilterQuery = "";
    private SortOrder _tableSortOrder = SortOrder.NaturalNumericAsc;

    // Structured Data Options
    private ObservableCollection<StructuredDataNode> _structuredNodes = new();
    private StructuredDataNode? _selectedStructuredNode;
    private string _structuredFilterQuery = "";
    private string _structuredFilterKeyList = "";
    private string _structuredQueryPath = "";
    private string _structuredFormatDescription = "None";
    private string _structuredStatusText = "No structured data";
    private bool _hasStructuredData = false;
    private int _structuredNodeCount = 0;

    // History & Timeline Management
    private readonly InputHistoryManager _historyManager = new();
    private InputHistoryItem? _selectedHistoryItem;
    private string _historySearchQuery = string.Empty;
    private ICollectionView? _historyView;
    private bool _isNavigatingHistory = false;

    // Selected Operation ID for real-time mode
    private string _currentAction = "SqlIn";

    // Action Fuzzy Search & Command Palette
    private bool _isCommandPaletteOpen = false;
    private string _actionSearchQuery = string.Empty;
    private ObservableCollection<ActionItem> _filteredActions = new();
    private ActionItem? _selectedAction;

    // Recipes & Visual Pipelines
    private TransformationRecipe? _selectedRecipe;
    private RecipeStep? _selectedPipelineStep;
    private RecipeCatalogItem? _selectedCatalogItemToAdd;
    private string _newRecipeName = "My Custom Recipe";
    private string _newRecipeDescription = string.Empty;
    private string? _newRecipeHotkey;
    private string _pipelineStatusText = "Pipeline ready";
    private bool _isRecipesTabHighlighted;

    // Regex Lab & Live Match Inspector
    private readonly RegexLabEngine _regexLabEngine = new();
    private string _regexLabPattern = @"\b(?<user>[a-zA-Z0-9._%+-]+)@(?<domain>[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})\b";
    private bool _regexIgnoreCase = true;
    private bool _regexMultiline = false;
    private bool _regexSingleline = false;
    private bool _regexIgnoreWhitespace = false;
    private RegexLabResult _regexLabResult = RegexLabResult.Empty;
    private RegexPatternPreset? _selectedRegexPreset;
    private RegexMatchItem? _selectedRegexMatch;
    private ObservableCollection<RegexPatternPreset> _regexPresets = new(RegexLibraryCatalog.Presets);
    private string _regexPresetSearchQuery = string.Empty;
    private ICollectionView? _filteredRegexPresets;

    public ObservableCollection<TransformationRecipe> SavedRecipes { get; } = new();
    public ObservableCollection<RecipeStep> CurrentPipelineSteps { get; } = new();
    public IReadOnlyList<RecipeCatalogItem> CatalogItems { get; } = RecipeCatalog.GetAllCatalogItems();

    public MainViewModel()
    {
        InitializeCommands();
        InitializeRecipes();
        InitializeRegexLab();
        UpdateActionSearchResults();
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

    public bool WatchClipboard
    {
        get => _watchClipboard;
        set
        {
            if (_watchClipboard != value)
            {
                _watchClipboard = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsWatchingClipboard));
                if (value)
                {
                    StatusMessage = "Watching clipboard for new items";
                    _clipboardWatcher?.Start();
                }
                else
                {
                    StatusMessage = "Stopped watching clipboard";
                    _clipboardWatcher?.Stop();
                }
            }
        }
    }

    public bool IsWatchingClipboard => WatchClipboard;

    public bool IsCommandPaletteOpen
    {
        get => _isCommandPaletteOpen;
        set
        {
            if (_isCommandPaletteOpen != value)
            {
                _isCommandPaletteOpen = value;
                OnPropertyChanged();
                if (value)
                {
                    ActionSearchQuery = string.Empty;
                    UpdateActionSearchResults();
                }
            }
        }
    }

    public string ActionSearchQuery
    {
        get => _actionSearchQuery;
        set
        {
            if (_actionSearchQuery != value)
            {
                _actionSearchQuery = value;
                OnPropertyChanged();
                UpdateActionSearchResults();
            }
        }
    }

    public ObservableCollection<ActionItem> FilteredActions => _filteredActions;

    public ActionItem? SelectedAction
    {
        get => _selectedAction;
        set
        {
            if (_selectedAction != value)
            {
                _selectedAction = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasActionResults => _filteredActions.Count > 0;
    public string ActionResultsCountText => $"{_filteredActions.Count} actions";

    public IClipboardWatcher? ClipboardWatcher
    {
        get => _clipboardWatcher;
        set
        {
            if (_clipboardWatcher != value)
            {
                if (_clipboardWatcher != null)
                {
                    _clipboardWatcher.ClipboardChanged -= OnClipboardWatcherChanged;
                }
                _clipboardWatcher = value;
                if (_clipboardWatcher != null)
                {
                    _clipboardWatcher.ClipboardChanged += OnClipboardWatcherChanged;
                    if (WatchClipboard)
                    {
                        _clipboardWatcher.Start();
                    }
                }
            }
        }
    }

    private void OnClipboardWatcherChanged(object? sender, ClipboardChangedEventArgs e)
    {
        ProcessClipboardItem(e.Text, e.Html);
    }

    public void ProcessClipboardItem(string? text, string? html = null)
    {
        if (!WatchClipboard) return;

        try
        {
            string? newText = null;
            string source = "Clipboard Watch";

            if (!string.IsNullOrEmpty(html) && HtmlTableParser.IsHtmlTable(html))
            {
                string cleanTable = HtmlTableParser.ExtractTableHtml(html);
                newText = TextBeautifier.Beautify(cleanTable);
                source = "Clipboard Watch (HTML Table)";
            }
            else if (!string.IsNullOrEmpty(text))
            {
                newText = TextBeautifier.Beautify(text);
            }

            if (string.IsNullOrWhiteSpace(newText)) return;

            if (string.Equals(newText, _lastProcessedClipboardText, StringComparison.Ordinal) ||
                string.Equals(newText, InputText, StringComparison.Ordinal))
            {
                _lastProcessedClipboardText = newText;
                return;
            }

            _lastProcessedClipboardText = newText;
            InputText = newText;
            RecordHistory(InputText, source);
            StatusMessage = "Added new item from clipboard";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Clipboard watch error: {ex.Message}";
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
            DetectedFormat.SqlInClause or DetectedFormat.Sql => "SQL",
            _ => DetectSyntaxFromContent(text)
        };
    }

    private static string DetectSyntaxFromAction(string action, string text)
    {
        return action switch
        {
            "ToCsv" or "ExtractSelectedToCsv" => "CSV",
            "ToTsv" or "ExtractSelectedToTsv" or "ExtractRegexGroupsTable" => "TSV",
            "ToYaml" or "ToYamlObjects" or "ToYamlArrays" or "ToYamlArray" or "ToYamlList" or "KvToYaml" or "JsonToYaml" or "FormatYaml" or "TableToKeyValueYaml" or "TableToKeyValueYamlRest" or "ExtractSelectedToYaml" => "YAML",
            "SqlIn" or "SqlInMultiLine" or "ExtractSqlIn" or "ExtractSelectedToSqlIn" or "ToSqlInserts" => "SQL",
            "ToCSharpArray" or "ToCSharpList" or "EscapeCSharp" or "UnescapeCSharp" or "ExtractCSharpArray" or "ExtractSelectedToCodeArray" => "C#",
            "ToTypeScriptArray" => "TypeScript",
            "ToPythonList" => "Python",
            "ToJsonArray" or "ToJsonObjects" or "ToJsonArrays" or "ExtractSelectedToJson" or "KvToJson" or "TableToKeyValueJson" or "TableToKeyValueJsonRest" or "YamlToJson" or "FormatJson" or "JwtDecode" or "ExtractJsonMap" or "ExtractRegexGroupsJson" => "JSON",
            "FormatXml" => "XML",
            "ToMarkdownTable" or "ExtractSelectedToMarkdown" => "Markdown",
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
        if (DefaultTextAnalyzer.IsSql(text)) return "SQL";

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
                OnPropertyChanged(nameof(IsTableTabHighlighted));
            }
        }
    }

    public bool IsTableTabHighlighted => IsTabularTabHighlighted;

    private bool _isStructuredTabHighlighted;
    public bool IsStructuredTabHighlighted
    {
        get => _isStructuredTabHighlighted;
        private set
        {
            if (_isStructuredTabHighlighted != value)
            {
                _isStructuredTabHighlighted = value;
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

    public bool IsRecipesTabHighlighted
    {
        get => _isRecipesTabHighlighted;
        private set
        {
            if (_isRecipesTabHighlighted != value)
            {
                _isRecipesTabHighlighted = value;
                OnPropertyChanged();
            }
        }
    }

    public TransformationRecipe? SelectedRecipe
    {
        get => _selectedRecipe;
        set
        {
            if (_selectedRecipe != value)
            {
                _selectedRecipe = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedRecipe));
                OnPropertyChanged(nameof(CanDeleteSelectedRecipe));
            }
        }
    }

    public bool HasSelectedRecipe => _selectedRecipe != null;
    public bool CanDeleteSelectedRecipe => _selectedRecipe != null && !_selectedRecipe.IsBuiltIn;

    public RecipeStep? SelectedPipelineStep
    {
        get => _selectedPipelineStep;
        set
        {
            if (_selectedPipelineStep != value)
            {
                _selectedPipelineStep = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedPipelineStep));
            }
        }
    }

    public bool HasSelectedPipelineStep => _selectedPipelineStep != null;
    public bool HasPipelineSteps => CurrentPipelineSteps.Count > 0;
    public int PipelineStepCount => CurrentPipelineSteps.Count;

    public RecipeCatalogItem? SelectedCatalogItemToAdd
    {
        get => _selectedCatalogItemToAdd;
        set
        {
            if (_selectedCatalogItemToAdd != value)
            {
                _selectedCatalogItemToAdd = value;
                OnPropertyChanged();
            }
        }
    }

    public string NewRecipeName
    {
        get => _newRecipeName;
        set
        {
            if (_newRecipeName != value)
            {
                _newRecipeName = value;
                OnPropertyChanged();
            }
        }
    }

    public string NewRecipeDescription
    {
        get => _newRecipeDescription;
        set
        {
            if (_newRecipeDescription != value)
            {
                _newRecipeDescription = value;
                OnPropertyChanged();
            }
        }
    }

    public string? NewRecipeHotkey
    {
        get => _newRecipeHotkey;
        set
        {
            if (_newRecipeHotkey != value)
            {
                _newRecipeHotkey = value;
                OnPropertyChanged();
            }
        }
    }

    public string PipelineStatusText
    {
        get => _pipelineStatusText;
        set
        {
            if (_pipelineStatusText != value)
            {
                _pipelineStatusText = value;
                OnPropertyChanged();
            }
        }
    }

    // Regex Lab & Live Match Inspector Properties
    public string RegexLabPattern
    {
        get => _regexLabPattern;
        set
        {
            if (_regexLabPattern != value)
            {
                _regexLabPattern = value;
                OnPropertyChanged();
                UpdateRegexLab();
            }
        }
    }

    public bool RegexIgnoreCase
    {
        get => _regexIgnoreCase;
        set
        {
            if (_regexIgnoreCase != value)
            {
                _regexIgnoreCase = value;
                OnPropertyChanged();
                UpdateRegexLab();
            }
        }
    }

    public bool RegexMultiline
    {
        get => _regexMultiline;
        set
        {
            if (_regexMultiline != value)
            {
                _regexMultiline = value;
                OnPropertyChanged();
                UpdateRegexLab();
            }
        }
    }

    public bool RegexSingleline
    {
        get => _regexSingleline;
        set
        {
            if (_regexSingleline != value)
            {
                _regexSingleline = value;
                OnPropertyChanged();
                UpdateRegexLab();
            }
        }
    }

    public bool RegexIgnoreWhitespace
    {
        get => _regexIgnoreWhitespace;
        set
        {
            if (_regexIgnoreWhitespace != value)
            {
                _regexIgnoreWhitespace = value;
                OnPropertyChanged();
                UpdateRegexLab();
            }
        }
    }

    public RegexLabResult RegexLabResult => _regexLabResult;
    public DataTable? RegexGroupDataTable => _regexLabResult.GroupTable;
    public IReadOnlyList<RegexMatchItem> RegexMatches => _regexLabResult.Matches;
    public bool HasRegexMatches => _regexLabResult.Matches.Count > 0;
    public bool RegexHasError => !_regexLabResult.IsValid;
    public string RegexErrorMessage => _regexLabResult.ErrorMessage ?? string.Empty;
    public int RegexMatchCount => _regexLabResult.TotalMatches;
    public int RegexGroupCount => _regexLabResult.TotalGroups;
    public double RegexExecutionTimeMs => _regexLabResult.ExecutionTimeMs;

    public string RegexStatusMessage
    {
        get
        {
            if (string.IsNullOrEmpty(_regexLabPattern))
                return "Enter a regular expression pattern to evaluate matches.";
            if (!_regexLabResult.IsValid)
                return $"Syntax Error: {_regexLabResult.ErrorMessage}";
            if (_regexLabResult.Matches.Count == 0)
                return $"0 matches found in {RegexExecutionTimeMs:F2}ms";
            return $"{_regexLabResult.Matches.Count} {(_regexLabResult.Matches.Count == 1 ? "match" : "matches")} ({_regexLabResult.TotalGroups} groups captured) in {RegexExecutionTimeMs:F2}ms";
        }
    }

    public RegexMatchItem? SelectedRegexMatch
    {
        get => _selectedRegexMatch;
        set
        {
            if (_selectedRegexMatch != value)
            {
                _selectedRegexMatch = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<RegexPatternPreset> RegexPresets => _regexPresets;

    public RegexPatternPreset? SelectedRegexPreset
    {
        get => _selectedRegexPreset;
        set
        {
            if (_selectedRegexPreset != value)
            {
                _selectedRegexPreset = value;
                OnPropertyChanged();
                if (value != null)
                {
                    ApplyRegexPreset(value, loadSample: false);
                }
            }
        }
    }

    public string RegexPresetSearchQuery
    {
        get => _regexPresetSearchQuery;
        set
        {
            if (_regexPresetSearchQuery != value)
            {
                _regexPresetSearchQuery = value;
                OnPropertyChanged();
                _filteredRegexPresets?.Refresh();
            }
        }
    }

    public ICollectionView FilteredRegexPresets => _filteredRegexPresets ?? CollectionViewSource.GetDefaultView(_regexPresets);

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

    public string SurrogateHeaders
    {
        get => _surrogateHeaders;
        set
        {
            if (_surrogateHeaders != value)
            {
                _surrogateHeaders = value;
                OnPropertyChanged();
                OnSurrogateHeadersChanged();
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

    public bool KeyValueIncludeRestOfColumns
    {
        get => _keyValueIncludeRestOfColumns;
        set
        {
            if (_keyValueIncludeRestOfColumns != value)
            {
                _keyValueIncludeRestOfColumns = value;
                OnPropertyChanged();
                TriggerRealTime();
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

    // Structured Data Properties
    public ObservableCollection<StructuredDataNode> StructuredNodes => _structuredNodes;

    public StructuredDataNode? SelectedStructuredNode
    {
        get => _selectedStructuredNode;
        set
        {
            if (_selectedStructuredNode != value)
            {
                _selectedStructuredNode = value;
                OnPropertyChanged();
            }
        }
    }

    public string StructuredFilterQuery
    {
        get => _structuredFilterQuery;
        set
        {
            if (_structuredFilterQuery != value)
            {
                _structuredFilterQuery = value;
                OnPropertyChanged();
                ApplyStructuredFilter();
            }
        }
    }

    public string StructuredFilterKeyList
    {
        get => _structuredFilterKeyList;
        set
        {
            if (_structuredFilterKeyList != value)
            {
                _structuredFilterKeyList = value;
                OnPropertyChanged();
            }
        }
    }

    public string StructuredQueryPath
    {
        get => _structuredQueryPath;
        set
        {
            if (_structuredQueryPath != value)
            {
                _structuredQueryPath = value;
                OnPropertyChanged();
            }
        }
    }

    public string StructuredFormatDescription
    {
        get => _structuredFormatDescription;
        private set
        {
            if (_structuredFormatDescription != value)
            {
                _structuredFormatDescription = value;
                OnPropertyChanged();
            }
        }
    }

    public string StructuredStatusText
    {
        get => _structuredStatusText;
        private set
        {
            if (_structuredStatusText != value)
            {
                _structuredStatusText = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasStructuredData
    {
        get => _hasStructuredData;
        private set
        {
            if (_hasStructuredData != value)
            {
                _hasStructuredData = value;
                OnPropertyChanged();
            }
        }
    }

    public int StructuredNodeCount
    {
        get => _structuredNodeCount;
        private set
        {
            if (_structuredNodeCount != value)
            {
                _structuredNodeCount = value;
                OnPropertyChanged();
            }
        }
    }

    // Structured Data Commands
    public ICommand ExpandAllStructuredNodesCommand { get; private set; } = null!;
    public ICommand CollapseAllStructuredNodesCommand { get; private set; } = null!;
    public ICommand CopyStructuredPathCommand { get; private set; } = null!;
    public ICommand CopyStructuredValueCommand { get; private set; } = null!;
    public ICommand CopyStructuredSubtreeCommand { get; private set; } = null!;
    public ICommand ExtractSelectedStructuredNodeCommand { get; private set; } = null!;

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

    // Action Fuzzy Search & Command Palette Commands
    public ICommand OpenCommandPaletteCommand { get; private set; } = null!;
    public ICommand CloseCommandPaletteCommand { get; private set; } = null!;
    public ICommand ToggleCommandPaletteCommand { get; private set; } = null!;
    public ICommand ExecuteActionItemCommand { get; private set; } = null!;
    public ICommand SelectNextActionCommand { get; private set; } = null!;
    public ICommand SelectPreviousActionCommand { get; private set; } = null!;
    public ICommand ExecuteSelectedActionCommand { get; private set; } = null!;

    // Recipe & Pipeline Commands
    public ICommand ExecuteRecipeCommand { get; private set; } = null!;
    public ICommand ExecuteCurrentPipelineCommand { get; private set; } = null!;
    public ICommand AddStepToPipelineCommand { get; private set; } = null!;
    public ICommand RemoveStepFromPipelineCommand { get; private set; } = null!;
    public ICommand MoveStepUpCommand { get; private set; } = null!;
    public ICommand MoveStepDownCommand { get; private set; } = null!;
    public ICommand ClearPipelineCommand { get; private set; } = null!;
    public ICommand LoadRecipeToPipelineCommand { get; private set; } = null!;
    public ICommand SavePipelineAsRecipeCommand { get; private set; } = null!;
    public ICommand DeleteRecipeCommand { get; private set; } = null!;
    public ICommand DuplicateRecipeCommand { get; private set; } = null!;
    public ICommand ExportRecipeCommand { get; private set; } = null!;
    public ICommand ExportAllRecipesCommand { get; private set; } = null!;
    public ICommand ImportRecipesCommand { get; private set; } = null!;

    // Regex Lab & Live Match Inspector Commands
    public ICommand ApplyRegexPresetCommand { get; private set; } = null!;
    public ICommand LoadRegexSampleTextCommand { get; private set; } = null!;
    public ICommand ExtractRegexMatchesCommand { get; private set; } = null!;
    public ICommand ExtractRegexGroupsTableCommand { get; private set; } = null!;
    public ICommand ExtractRegexGroupsJsonCommand { get; private set; } = null!;
    public ICommand ClearRegexPatternCommand { get; private set; } = null!;
    public ICommand OpenRegexLabCommand { get; private set; } = null!;

    public void UpdateActionSearchResults()
    {
        var matches = ActionRegistry.Search(_actionSearchQuery);
        _filteredActions.Clear();
        foreach (var match in matches)
        {
            _filteredActions.Add(match);
        }
        SelectedAction = _filteredActions.FirstOrDefault();
        OnPropertyChanged(nameof(HasActionResults));
        OnPropertyChanged(nameof(ActionResultsCountText));
    }

    public void ExecuteActionItem(ActionItem? item)
    {
        if (item == null) return;
        IsCommandPaletteOpen = false;

        if (item.TargetSidebarTab.HasValue)
        {
            SelectedSidebarTabIndex = item.TargetSidebarTab.Value;
        }

        if (item.Id.StartsWith("Recipe:", StringComparison.OrdinalIgnoreCase))
        {
            string recipeId = item.Id.Substring("Recipe:".Length);
            var recipe = SavedRecipes.FirstOrDefault(r => string.Equals(r.Id, recipeId, StringComparison.OrdinalIgnoreCase) || string.Equals(r.Name, recipeId, StringComparison.OrdinalIgnoreCase));
            if (recipe != null)
            {
                ExecuteRecipe(recipe);
            }
            return;
        }

        if (string.Equals(item.Id, "ExecuteActivePipeline", StringComparison.OrdinalIgnoreCase))
        {
            ExecuteCurrentPipeline();
            return;
        }

        if (item.Id.StartsWith("RegexPreset:", StringComparison.OrdinalIgnoreCase))
        {
            string presetId = item.Id.Substring("RegexPreset:".Length);
            var preset = RegexLibraryCatalog.FindById(presetId);
            if (preset != null)
            {
                SelectedCenterTabIndex = 3;
                ApplyRegexPreset(preset, loadSample: false);
            }
            return;
        }

        if (string.Equals(item.Id, "OpenRegexLab", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.Id, "ShowRegexLabTab", StringComparison.OrdinalIgnoreCase))
        {
            SelectedCenterTabIndex = 3;
            return;
        }

        if (string.Equals(item.Id, "ExtractRegexMatches", StringComparison.OrdinalIgnoreCase))
        {
            ExtractRegexMatchesCommand.Execute(null);
            return;
        }

        if (string.Equals(item.Id, "ExtractRegexGroupsTable", StringComparison.OrdinalIgnoreCase))
        {
            ExtractRegexGroupsTableCommand.Execute(null);
            return;
        }

        if (string.Equals(item.Id, "ExtractRegexGroupsJson", StringComparison.OrdinalIgnoreCase))
        {
            ExtractRegexGroupsJsonCommand.Execute(null);
            return;
        }

        switch (item.Id)
        {
            case "SendOutputToInput":
                if (!string.IsNullOrEmpty(OutputText))
                {
                    InputText = OutputText;
                    RecordHistory(InputText, "Output ➔ Input");
                    StatusMessage = "Copied output to input";
                }
                break;

            case "LoadFile":
                LoadFileCommand.Execute(null);
                break;

            case "ClearInput":
                ClearInputCommand.Execute(null);
                break;

            case "CreateSnapshot":
                CreateSnapshotCommand.Execute(null);
                break;

            case "HistoryBack":
                if (HistoryBackCommand.CanExecute(null))
                    HistoryBackCommand.Execute(null);
                break;

            case "HistoryForward":
                if (HistoryForwardCommand.CanExecute(null))
                    HistoryForwardCommand.Execute(null);
                break;

            case "ToggleRealTime":
                IsRealTimeTransform = !IsRealTimeTransform;
                StatusMessage = IsRealTimeTransform ? "Real-time transformation enabled" : "Real-time transformation paused";
                break;

            case "ToggleWatchClipboard":
                WatchClipboard = !WatchClipboard;
                break;

            case "ToggleAutoSend":
                AutoSendOutputToInput = !AutoSendOutputToInput;
                StatusMessage = AutoSendOutputToInput ? "Auto Output ➔ Input enabled" : "Auto Output ➔ Input disabled";
                break;

            case "ToggleWordWrap":
                IsWordWrap = !IsWordWrap;
                StatusMessage = IsWordWrap ? "Word wrap enabled" : "Word wrap disabled";
                break;

            case "ShowLinesTab":
                SelectedSidebarTabIndex = 0;
                break;
            case "ShowTabularTab":
                SelectedSidebarTabIndex = 1;
                break;
            case "ShowStructuredTab":
                SelectedSidebarTabIndex = 2;
                break;
            case "ShowCodeTab":
                SelectedSidebarTabIndex = 3;
                break;
            case "ShowCaseEncTab":
                SelectedSidebarTabIndex = 4;
                break;
            case "ShowRecipesTab":
                SelectedSidebarTabIndex = 5;
                break;
            case "ShowHistoryTab":
                SelectedSidebarTabIndex = 5;
                break;

            default:
                _currentAction = item.Id;
                ExecuteCurrentAction();
                break;
        }
    }

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
                _lastProcessedClipboardText = item.FullText;
                try { Clipboard.SetText(item.FullText); } catch {}
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
            sb.AppendLine("=== REFRAME INPUT HISTORY TIMELINE ===");
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
            var fullExport = sb.ToString();
            _lastProcessedClipboardText = fullExport;
            try { Clipboard.SetText(fullExport); } catch {}
            StatusMessage = "Exported history timeline report to clipboard";
        });

        CopyOutputCommand = new RelayCommand(_ =>
        {
            if (!string.IsNullOrEmpty(OutputText))
            {
                _lastProcessedClipboardText = OutputText;
                try { Clipboard.SetText(OutputText); } catch {}
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

        // Structured Data Commands
        ExpandAllStructuredNodesCommand = new RelayCommand(_ => ExpandAllStructuredNodes());
        CollapseAllStructuredNodesCommand = new RelayCommand(_ => CollapseAllStructuredNodes());
        CopyStructuredPathCommand = new RelayCommand(_ =>
        {
            if (SelectedStructuredNode != null && !string.IsNullOrEmpty(SelectedStructuredNode.Path))
            {
                _lastProcessedClipboardText = SelectedStructuredNode.Path;
                try { Clipboard.SetText(SelectedStructuredNode.Path); } catch {}
                StatusMessage = $"Copied path: {SelectedStructuredNode.Path}";
            }
            else
            {
                StatusMessage = "No structured node selected to copy path";
            }
        });
        CopyStructuredValueCommand = new RelayCommand(_ =>
        {
            if (SelectedStructuredNode != null)
            {
                string val = SelectedStructuredNode.Value ?? SelectedStructuredNode.DisplayValue;
                _lastProcessedClipboardText = val;
                try { Clipboard.SetText(val); } catch {}
                StatusMessage = "Copied node value to clipboard";
            }
            else
            {
                StatusMessage = "No structured node selected to copy value";
            }
        });
        CopyStructuredSubtreeCommand = new RelayCommand(_ =>
        {
            if (SelectedStructuredNode != null)
            {
                string val = SelectedStructuredNode.DisplayValue;
                _lastProcessedClipboardText = val;
                try { Clipboard.SetText(val); } catch {}
                StatusMessage = "Copied subtree to clipboard";
            }
        });
        ExtractSelectedStructuredNodeCommand = new RelayCommand(_ =>
        {
            if (SelectedStructuredNode != null)
            {
                OutputText = SelectedStructuredNode.Value ?? SelectedStructuredNode.DisplayValue;
                StatusMessage = $"Extracted node: {SelectedStructuredNode.Name}";
            }
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
                "json" or "structured_json" => "{\n  \"store\": {\n    \"name\": \"City Bookstore\",\n    \"isOpen\": true,\n    \"founded\": 1998,\n    \"location\": {\n      \"city\": \"Seattle\",\n      \"state\": \"WA\",\n      \"zip\": \"98101\"\n    },\n    \"books\": [\n      {\n        \"id\": 1,\n        \"title\": \"Designing Data-Intensive Applications\",\n        \"author\": \"Martin Kleppmann\",\n        \"price\": 39.99,\n        \"inStock\": true,\n        \"tags\": [\"database\", \"distributed systems\", \"architecture\"]\n      },\n      {\n        \"id\": 2,\n        \"title\": \"Clean Architecture\",\n        \"author\": \"Robert C. Martin\",\n        \"price\": 32.50,\n        \"inStock\": false,\n        \"tags\": [\"software design\", \"best practices\"]\n      }\n    ]\n  }\n}",
                "yaml" => "- id: 1\n  name: Development\n  active: true\n  department: Engineering\n- id: 2\n  name: Staging\n  active: true\n  department: QA\n- id: 3\n  name: Production\n  active: false\n  department: Operations",
                "structured_yaml" => "server:\n  host: api.reframe.dev\n  port: 8443\n  ssl:\n    enabled: true\n    certificate: /etc/ssl/certs/forge.crt\ndatabase:\n  provider: postgresql\n  connectionString: Server=db.internal;Port=5432;Database=reframe;User Id=app;\n  pool:\n    min: 5\n    max: 50\nfeatures:\n  - realTimeTransform\n  - syntaxHighlighting\n  - tabularView\n  - structuredTreeView\nendpoints:\n  - path: /api/v1/transform\n    rateLimit: 1000\n    authRequired: true\n  - path: /api/v1/health\n    rateLimit: 100\n    authRequired: false",
                "xml" or "structured_xml" => "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<catalog>\n  <book id=\"bk101\">\n    <author>Gambardella, Matthew</author>\n    <title>XML Developer's Guide</title>\n    <genre>Computer</genre>\n    <price>44.95</price>\n    <publish_date>2000-10-01</publish_date>\n    <description>An in-depth look at creating applications with XML.</description>\n  </book>\n  <book id=\"bk102\">\n    <author>Ralls, Kim</author>\n    <title>Midnight Rain</title>\n    <genre>Fantasy</genre>\n    <price>5.95</price>\n    <publish_date>2000-12-16</publish_date>\n    <description>A former architect battles corporate zombies.</description>\n  </book>\n  <book id=\"bk103\">\n    <author>Corets, Eva</author>\n    <title>Maeve Ascendant</title>\n    <genre>Fantasy</genre>\n    <price>5.95</price>\n    <publish_date>2000-11-17</publish_date>\n    <description>After the collapse of a nanotechnology society the young survivors lay the foundation for a new society.</description>\n  </book>\n</catalog>",
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

        // Action Fuzzy Search & Command Palette Commands
        OpenCommandPaletteCommand = new RelayCommand(_ =>
        {
            IsCommandPaletteOpen = true;
        });

        CloseCommandPaletteCommand = new RelayCommand(_ =>
        {
            IsCommandPaletteOpen = false;
        });

        ToggleCommandPaletteCommand = new RelayCommand(_ =>
        {
            IsCommandPaletteOpen = !IsCommandPaletteOpen;
        });

        ExecuteActionItemCommand = new RelayCommand(p =>
        {
            if (p is ActionItem item)
            {
                ExecuteActionItem(item);
            }
            else if (p is string actionId)
            {
                var found = ActionRegistry.AllActions.FirstOrDefault(a => string.Equals(a.Id, actionId, StringComparison.OrdinalIgnoreCase));
                if (found != null)
                {
                    ExecuteActionItem(found);
                }
                else
                {
                    _currentAction = actionId;
                    ExecuteCurrentAction();
                }
            }
        });

        SelectNextActionCommand = new RelayCommand(_ =>
        {
            if (_filteredActions.Count == 0) return;
            int idx = SelectedAction != null ? _filteredActions.IndexOf(SelectedAction) : -1;
            idx = (idx + 1) % _filteredActions.Count;
            SelectedAction = _filteredActions[idx];
        });

        SelectPreviousActionCommand = new RelayCommand(_ =>
        {
            if (_filteredActions.Count == 0) return;
            int idx = SelectedAction != null ? _filteredActions.IndexOf(SelectedAction) : -1;
            idx = idx <= 0 ? _filteredActions.Count - 1 : idx - 1;
            SelectedAction = _filteredActions[idx];
        });

        ExecuteSelectedActionCommand = new RelayCommand(_ =>
        {
            if (SelectedAction != null)
            {
                ExecuteActionItem(SelectedAction);
            }
        });

        // Recipe & Pipeline Commands
        ExecuteRecipeCommand = new RelayCommand(p =>
        {
            TransformationRecipe? recipe = p as TransformationRecipe ?? SelectedRecipe;
            if (recipe != null)
            {
                ExecuteRecipe(recipe);
            }
        });

        ExecuteCurrentPipelineCommand = new RelayCommand(_ =>
        {
            ExecuteCurrentPipeline();
        });

        AddStepToPipelineCommand = new RelayCommand(p =>
        {
            if (p is RecipeCatalogItem catItem)
            {
                AddCatalogItemToPipeline(catItem);
            }
            else if (p is string actionId)
            {
                AddStepToPipeline(actionId);
            }
            else if (SelectedCatalogItemToAdd != null)
            {
                AddCatalogItemToPipeline(SelectedCatalogItemToAdd);
            }
        });

        RemoveStepFromPipelineCommand = new RelayCommand(p =>
        {
            RecipeStep? step = p as RecipeStep ?? SelectedPipelineStep;
            if (step != null)
            {
                RemovePipelineStep(step);
            }
        });

        MoveStepUpCommand = new RelayCommand(p =>
        {
            RecipeStep? step = p as RecipeStep ?? SelectedPipelineStep;
            if (step != null)
            {
                MovePipelineStepUp(step);
            }
        });

        MoveStepDownCommand = new RelayCommand(p =>
        {
            RecipeStep? step = p as RecipeStep ?? SelectedPipelineStep;
            if (step != null)
            {
                MovePipelineStepDown(step);
            }
        });

        ClearPipelineCommand = new RelayCommand(_ =>
        {
            ClearPipeline();
        });

        LoadRecipeToPipelineCommand = new RelayCommand(p =>
        {
            TransformationRecipe? recipe = p as TransformationRecipe ?? SelectedRecipe;
            if (recipe != null)
            {
                LoadRecipeToPipeline(recipe);
            }
        });

        SavePipelineAsRecipeCommand = new RelayCommand(_ =>
        {
            SavePipelineAsRecipe(NewRecipeName, NewRecipeDescription, NewRecipeHotkey);
        });

        DeleteRecipeCommand = new RelayCommand(p =>
        {
            TransformationRecipe? recipe = p as TransformationRecipe ?? SelectedRecipe;
            if (recipe != null)
            {
                DeleteRecipe(recipe);
            }
        });

        DuplicateRecipeCommand = new RelayCommand(p =>
        {
            TransformationRecipe? recipe = p as TransformationRecipe ?? SelectedRecipe;
            if (recipe != null)
            {
                DuplicateRecipe(recipe);
            }
        });

        ExportRecipeCommand = new RelayCommand(p =>
        {
            TransformationRecipe? recipe = p as TransformationRecipe ?? SelectedRecipe;
            if (recipe != null)
            {
                ExportRecipe(recipe);
            }
        });

        ExportAllRecipesCommand = new RelayCommand(_ =>
        {
            ExportAllRecipes();
        });

        ImportRecipesCommand = new RelayCommand(p =>
        {
            string? json = p as string;
            ImportRecipes(json);
        });

        // Regex Lab & Live Match Inspector Commands
        ApplyRegexPresetCommand = new RelayCommand(p =>
        {
            if (p is RegexPatternPreset preset)
            {
                ApplyRegexPreset(preset);
            }
            else if (SelectedRegexPreset != null)
            {
                ApplyRegexPreset(SelectedRegexPreset);
            }
        });

        LoadRegexSampleTextCommand = new RelayCommand(p =>
        {
            var preset = p as RegexPatternPreset ?? SelectedRegexPreset;
            if (preset != null && !string.IsNullOrEmpty(preset.SampleText))
            {
                InputText = preset.SampleText;
                RecordHistory(InputText, $"Sample ({preset.Name})");
                StatusMessage = $"Loaded sample for {preset.Name}";
            }
        });

        ExtractRegexMatchesCommand = new RelayCommand(_ =>
        {
            if (_regexLabResult.Matches.Count > 0)
            {
                OutputText = _regexLabEngine.ExtractMatches(_regexLabResult);
                StatusMessage = $"Extracted {_regexLabResult.Matches.Count} matches to output";
            }
            else
            {
                OutputText = string.Empty;
                StatusMessage = "No matches to extract";
            }
        });

        ExtractRegexGroupsTableCommand = new RelayCommand(_ =>
        {
            if (_regexLabResult.Matches.Count > 0)
            {
                OutputText = _regexLabEngine.ExtractGroupsAsDelimited(_regexLabResult, "\t");
                StatusMessage = $"Extracted {_regexLabResult.Matches.Count} match rows as TSV table to output";
            }
            else
            {
                OutputText = string.Empty;
                StatusMessage = "No match groups to extract";
            }
        });

        ExtractRegexGroupsJsonCommand = new RelayCommand(_ =>
        {
            if (_regexLabResult.Matches.Count > 0)
            {
                OutputText = _regexLabEngine.ExtractGroupsAsJson(_regexLabResult, true);
                StatusMessage = $"Extracted {_regexLabResult.Matches.Count} matches as JSON array to output";
            }
            else
            {
                OutputText = "[]";
                StatusMessage = "No match groups to extract";
            }
        });

        ClearRegexPatternCommand = new RelayCommand(_ =>
        {
            RegexLabPattern = string.Empty;
        });

        OpenRegexLabCommand = new RelayCommand(_ =>
        {
            SelectedCenterTabIndex = 3;
        });
    }

    public void InitializeRecipes()
    {
        var presets = RecipeStorage.LoadUserPresets();
        SavedRecipes.Clear();
        foreach (var p in presets)
        {
            SavedRecipes.Add(p);
        }
        SelectedRecipe = SavedRecipes.FirstOrDefault();
        if (SelectedRecipe != null)
        {
            LoadRecipeToPipeline(SelectedRecipe);
        }
        SelectedCatalogItemToAdd = CatalogItems.FirstOrDefault();
        UpdateDynamicRecipeActions();
    }

    public void UpdateDynamicRecipeActions()
    {
        var actions = new List<ActionItem>();
        foreach (var recipe in SavedRecipes)
        {
            var keywords = new List<string>
            {
                "recipe",
                "pipeline",
                "preset",
                recipe.Name.ToLowerInvariant(),
                recipe.Category.ToLowerInvariant()
            };
            keywords.AddRange(recipe.Tags);
            foreach (var step in recipe.Steps)
            {
                keywords.Add(step.Title.ToLowerInvariant());
            }

            actions.Add(new ActionItem(
                id: $"Recipe:{recipe.Id}",
                title: $"Recipe: {recipe.Name}",
                category: "Recipes & Pipelines",
                description: string.IsNullOrEmpty(recipe.Description) ? recipe.StepSummary : recipe.Description,
                keywords: keywords,
                icon: "⚡",
                shortcut: recipe.Hotkey,
                targetSidebarTab: 5));
        }
        ActionRegistry.SetDynamicActions(actions);
    }

    public void ExecuteRecipe(TransformationRecipe recipe)
    {
        if (recipe == null) return;

        var result = RecipeEngine.Instance.Execute(recipe, InputText);
        OutputText = result.Output;

        if (result.Success)
        {
            StatusMessage = $"Recipe '{recipe.Name}' executed in {result.TotalTimeMs:F1}ms ({result.StepResults.Count} steps)";
            PipelineStatusText = $"{recipe.Name}: {result.StepResults.Count} steps, {result.TotalTimeMs:F1}ms";
        }
        else
        {
            StatusMessage = $"Recipe '{recipe.Name}' error: {result.ErrorMessage}";
            PipelineStatusText = $"Error: {result.ErrorMessage}";
        }

        if (recipe.AutoSendToInput && !string.IsNullOrEmpty(result.Output))
        {
            InputText = result.Output;
        }

        RecordHistory(result.Output, $"Recipe: {recipe.Name}");
    }

    public void ExecuteCurrentPipeline()
    {
        if (CurrentPipelineSteps.Count == 0)
        {
            StatusMessage = "Visual pipeline has no steps";
            PipelineStatusText = "Add steps to pipeline to execute";
            return;
        }

        var result = RecipeEngine.Instance.ExecuteSteps(CurrentPipelineSteps, InputText);
        OutputText = result.Output;

        if (result.Success)
        {
            StatusMessage = $"Pipeline executed in {result.TotalTimeMs:F1}ms ({CurrentPipelineSteps.Count} steps)";
            PipelineStatusText = $"Executed in {result.TotalTimeMs:F1}ms ({CurrentPipelineSteps.Count} steps)";
        }
        else
        {
            StatusMessage = $"Pipeline error: {result.ErrorMessage}";
            PipelineStatusText = $"Error: {result.ErrorMessage}";
        }

        if (AutoSendOutputToInput && !string.IsNullOrEmpty(result.Output))
        {
            InputText = result.Output;
        }

        RecordHistory(result.Output, $"Pipeline ({CurrentPipelineSteps.Count} steps)");
    }

    public void AddStepToPipeline(string actionId)
    {
        var item = RecipeCatalog.FindCatalogItem(actionId);
        if (item != null)
        {
            AddCatalogItemToPipeline(item);
        }
        else
        {
            var step = new RecipeStep(actionId, actionId, "Custom", "", "⚡");
            CurrentPipelineSteps.Add(step);
            SelectedPipelineStep = step;
            OnPropertyChanged(nameof(HasPipelineSteps));
            OnPropertyChanged(nameof(PipelineStepCount));
            PipelineStatusText = $"{CurrentPipelineSteps.Count} steps in pipeline";
        }
    }

    public void AddCatalogItemToPipeline(RecipeCatalogItem item)
    {
        if (item == null) return;
        var step = item.CreateStep();
        CurrentPipelineSteps.Add(step);
        SelectedPipelineStep = step;
        OnPropertyChanged(nameof(HasPipelineSteps));
        OnPropertyChanged(nameof(PipelineStepCount));
        PipelineStatusText = $"{CurrentPipelineSteps.Count} steps in pipeline";
        StatusMessage = $"Added '{step.Title}' to pipeline";
    }

    public void RemovePipelineStep(RecipeStep step)
    {
        if (step == null) return;
        int idx = CurrentPipelineSteps.IndexOf(step);
        CurrentPipelineSteps.Remove(step);
        if (CurrentPipelineSteps.Count > 0)
        {
            int nextIdx = Math.Min(idx, CurrentPipelineSteps.Count - 1);
            SelectedPipelineStep = CurrentPipelineSteps[nextIdx];
        }
        else
        {
            SelectedPipelineStep = null;
        }
        OnPropertyChanged(nameof(HasPipelineSteps));
        OnPropertyChanged(nameof(PipelineStepCount));
        PipelineStatusText = $"{CurrentPipelineSteps.Count} steps in pipeline";
    }

    public void MovePipelineStepUp(RecipeStep step)
    {
        if (step == null) return;
        int idx = CurrentPipelineSteps.IndexOf(step);
        if (idx > 0)
        {
            CurrentPipelineSteps.Move(idx, idx - 1);
            SelectedPipelineStep = step;
        }
    }

    public void MovePipelineStepDown(RecipeStep step)
    {
        if (step == null) return;
        int idx = CurrentPipelineSteps.IndexOf(step);
        if (idx >= 0 && idx < CurrentPipelineSteps.Count - 1)
        {
            CurrentPipelineSteps.Move(idx, idx + 1);
            SelectedPipelineStep = step;
        }
    }

    public void ClearPipeline()
    {
        CurrentPipelineSteps.Clear();
        SelectedPipelineStep = null;
        OnPropertyChanged(nameof(HasPipelineSteps));
        OnPropertyChanged(nameof(PipelineStepCount));
        PipelineStatusText = "Pipeline cleared (0 steps)";
        StatusMessage = "Cleared visual pipeline";
    }

    public void LoadRecipeToPipeline(TransformationRecipe recipe)
    {
        if (recipe == null) return;
        CurrentPipelineSteps.Clear();
        foreach (var step in recipe.Steps)
        {
            CurrentPipelineSteps.Add(step.Clone());
        }
        NewRecipeName = recipe.Name;
        NewRecipeDescription = recipe.Description;
        NewRecipeHotkey = recipe.Hotkey;
        SelectedRecipe = recipe;
        PipelineStatusText = $"Loaded '{recipe.Name}' ({CurrentPipelineSteps.Count} steps)";
        StatusMessage = $"Loaded recipe '{recipe.Name}' into pipeline builder";
        OnPropertyChanged(nameof(HasPipelineSteps));
        OnPropertyChanged(nameof(PipelineStepCount));
    }

    public void SavePipelineAsRecipe(string name, string description = "", string? hotkey = null)
    {
        if (CurrentPipelineSteps.Count == 0)
        {
            StatusMessage = "Cannot save empty pipeline as recipe";
            return;
        }

        string recipeName = string.IsNullOrWhiteSpace(name) ? "My Custom Recipe" : name.Trim();
        var newRecipe = new TransformationRecipe(
            name: recipeName,
            description: description,
            category: "Custom",
            hotkey: hotkey,
            steps: CurrentPipelineSteps.Select(s => s.Clone()),
            isBuiltIn: false);

        SavedRecipes.Add(newRecipe);
        SelectedRecipe = newRecipe;
        RecipeStorage.SaveUserPresets(SavedRecipes);
        UpdateDynamicRecipeActions();
        StatusMessage = $"Saved custom recipe '{newRecipe.Name}'";
        PipelineStatusText = $"Saved as '{newRecipe.Name}'";
    }

    public void DeleteRecipe(TransformationRecipe recipe)
    {
        if (recipe == null || recipe.IsBuiltIn) return;
        SavedRecipes.Remove(recipe);
        if (SelectedRecipe == recipe)
        {
            SelectedRecipe = SavedRecipes.FirstOrDefault();
        }
        RecipeStorage.SaveUserPresets(SavedRecipes);
        UpdateDynamicRecipeActions();
        StatusMessage = $"Deleted recipe '{recipe.Name}'";
    }

    public void DuplicateRecipe(TransformationRecipe recipe)
    {
        if (recipe == null) return;
        var copy = recipe.Clone(asNewCustom: true);
        SavedRecipes.Add(copy);
        SelectedRecipe = copy;
        LoadRecipeToPipeline(copy);
        RecipeStorage.SaveUserPresets(SavedRecipes);
        UpdateDynamicRecipeActions();
        StatusMessage = $"Duplicated recipe as '{copy.Name}'";
    }

    public string ExportRecipe(TransformationRecipe recipe)
    {
        if (recipe == null) return string.Empty;
        string json = RecipeStorage.ExportToJson(recipe);
        try
        {
            _lastProcessedClipboardText = json;
            Clipboard.SetText(json);
        }
        catch { }
        StatusMessage = $"Exported recipe '{recipe.Name}' to clipboard (JSON)";
        return json;
    }

    public string ExportAllRecipes()
    {
        string json = RecipeStorage.ExportAllToJson(SavedRecipes);
        try
        {
            _lastProcessedClipboardText = json;
            Clipboard.SetText(json);
        }
        catch { }
        StatusMessage = $"Exported {SavedRecipes.Count} recipes to clipboard (JSON)";
        return json;
    }

    public int ImportRecipes(string? json)
    {
        string toImport = json ?? string.Empty;
        if (string.IsNullOrWhiteSpace(toImport))
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    toImport = Clipboard.GetText();
                }
            }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(toImport))
        {
            StatusMessage = "No JSON found to import";
            return 0;
        }

        var imported = RecipeStorage.ImportFromJson(toImport);
        if (imported.Count == 0)
        {
            StatusMessage = "No valid recipes could be imported from JSON";
            return 0;
        }

        int count = 0;
        foreach (var r in imported)
        {
            r.IsBuiltIn = false;
            SavedRecipes.Add(r);
            count++;
        }

        SelectedRecipe = imported.LastOrDefault();
        if (SelectedRecipe != null)
        {
            LoadRecipeToPipeline(SelectedRecipe);
        }

        RecipeStorage.SaveUserPresets(SavedRecipes);
        UpdateDynamicRecipeActions();
        StatusMessage = $"Successfully imported {count} recipe(s)";
        return count;
    }

    // -------------------------------------------------------------
    // Regex Lab & Live Match Inspector Methods
    // -------------------------------------------------------------
    public void InitializeRegexLab()
    {
        _regexPresets = new ObservableCollection<RegexPatternPreset>(RegexLibraryCatalog.Presets);
        _filteredRegexPresets = CollectionViewSource.GetDefaultView(_regexPresets);
        _filteredRegexPresets.Filter = FilterRegexPresetItem;
        _selectedRegexPreset = _regexPresets.FirstOrDefault();
        if (_selectedRegexPreset != null)
        {
            _regexLabPattern = _selectedRegexPreset.Pattern;
            _regexIgnoreCase = (_selectedRegexPreset.DefaultOptions & RegexOptions.IgnoreCase) != 0;
            _regexMultiline = (_selectedRegexPreset.DefaultOptions & RegexOptions.Multiline) != 0;
            _regexSingleline = (_selectedRegexPreset.DefaultOptions & RegexOptions.Singleline) != 0;
            _regexIgnoreWhitespace = (_selectedRegexPreset.DefaultOptions & RegexOptions.IgnorePatternWhitespace) != 0;
        }
        UpdateRegexLab();
    }

    private bool FilterRegexPresetItem(object obj)
    {
        if (obj is not RegexPatternPreset preset) return false;
        if (string.IsNullOrWhiteSpace(_regexPresetSearchQuery)) return true;

        string query = _regexPresetSearchQuery.Trim();
        return preset.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               preset.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               preset.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               preset.Pattern.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    public void ApplyRegexPreset(RegexPatternPreset preset, bool loadSample = false)
    {
        _selectedRegexPreset = preset;
        _regexLabPattern = preset.Pattern;
        _regexIgnoreCase = (preset.DefaultOptions & RegexOptions.IgnoreCase) != 0;
        _regexMultiline = (preset.DefaultOptions & RegexOptions.Multiline) != 0;
        _regexSingleline = (preset.DefaultOptions & RegexOptions.Singleline) != 0;
        _regexIgnoreWhitespace = (preset.DefaultOptions & RegexOptions.IgnorePatternWhitespace) != 0;

        OnPropertyChanged(nameof(SelectedRegexPreset));
        OnPropertyChanged(nameof(RegexLabPattern));
        OnPropertyChanged(nameof(RegexIgnoreCase));
        OnPropertyChanged(nameof(RegexMultiline));
        OnPropertyChanged(nameof(RegexSingleline));
        OnPropertyChanged(nameof(RegexIgnoreWhitespace));

        if (loadSample && !string.IsNullOrEmpty(preset.SampleText))
        {
            InputText = preset.SampleText;
            RecordHistory(InputText, $"Sample ({preset.Name})");
        }

        UpdateRegexLab();
        StatusMessage = $"Loaded regex pattern: {preset.Name}";
    }

    public void UpdateRegexLab()
    {
        var options = RegexOptions.None;
        if (_regexIgnoreCase) options |= RegexOptions.IgnoreCase;
        if (_regexMultiline) options |= RegexOptions.Multiline;
        if (_regexSingleline) options |= RegexOptions.Singleline;
        if (_regexIgnoreWhitespace) options |= RegexOptions.IgnorePatternWhitespace;

        _regexLabResult = _regexLabEngine.Evaluate(_inputText, _regexLabPattern, options);
        _selectedRegexMatch = _regexLabResult.Matches.FirstOrDefault();

        OnPropertyChanged(nameof(RegexLabResult));
        OnPropertyChanged(nameof(RegexGroupDataTable));
        OnPropertyChanged(nameof(RegexMatches));
        OnPropertyChanged(nameof(SelectedRegexMatch));
        OnPropertyChanged(nameof(RegexStatusMessage));
        OnPropertyChanged(nameof(RegexHasError));
        OnPropertyChanged(nameof(RegexErrorMessage));
        OnPropertyChanged(nameof(RegexMatchCount));
        OnPropertyChanged(nameof(RegexGroupCount));
        OnPropertyChanged(nameof(RegexExecutionTimeMs));
        OnPropertyChanged(nameof(HasRegexMatches));
    }

    public void ExpandAllStructuredNodes()
    {
        foreach (var node in _structuredNodes)
        {
            node.ExpandAll();
        }
        StatusMessage = "Expanded all structured tree nodes";
    }

    public void CollapseAllStructuredNodes()
    {
        foreach (var node in _structuredNodes)
        {
            node.CollapseAll();
        }
        StatusMessage = "Collapsed all structured tree nodes";
    }

    public void ApplyStructuredFilter()
    {
        foreach (var node in _structuredNodes)
        {
            node.ApplyFilter(_structuredFilterQuery);
        }
    }

    private void AnalyzeStructuredData()
    {
        var parseResult = StructuredDataParser.Parse(_inputText);
        _structuredNodes.Clear();
        if (parseResult.Success && parseResult.RootNodes.Count > 0)
        {
            foreach (var node in parseResult.RootNodes)
            {
                _structuredNodes.Add(node);
            }
            HasStructuredData = true;
            StructuredFormatDescription = parseResult.Format;
            StructuredNodeCount = parseResult.TotalNodeCount;
            StructuredStatusText = $"{parseResult.Format} ({parseResult.TotalNodeCount} nodes)";
            if (!string.IsNullOrWhiteSpace(StructuredFilterQuery))
            {
                ApplyStructuredFilter();
            }
        }
        else
        {
            HasStructuredData = false;
            StructuredFormatDescription = "None";
            StructuredNodeCount = 0;
            StructuredStatusText = "No structured data detected";
        }
    }

    private void AnalyzeInput()
    {
        // Update tabular preview with auto-detected headers
        _currentTable = TabularParser.DetectAndParse(_inputText);
        if (_currentTable != null)
        {
            _hasHeaders = _currentTable.HasHeaders;
            OnPropertyChanged(nameof(HasHeaders));

            if (!string.IsNullOrWhiteSpace(_surrogateHeaders))
            {
                var customHeaders = TabularParser.ParseHeaderList(_surrogateHeaders, _currentTable.Delimiter);
                if (customHeaders.Count > 0)
                {
                    _currentTable.OverrideHeaders(customHeaders);
                }
            }
        }

        UpdateColumnsAndPreviewFromCurrentTable();

        AnalyzeStructuredData();
        UpdateRegexLab();

        if (SelectedCenterTabIndex == 3)
        {
            // Keep user on Regex Lab tab if currently active
        }
        else if (HasTabularData)
        {
            SelectedCenterTabIndex = 0; // Table Grid View
        }
        else if (HasStructuredData)
        {
            SelectedCenterTabIndex = 1; // Structured Tree View
        }
        else
        {
            SelectedCenterTabIndex = 2; // Analysis & Stats
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
            IsStructuredTabHighlighted = false;
            IsCodeTabHighlighted = false;
            IsCaseEncTabHighlighted = false;
            SelectedSidebarTabIndex = 0;
            return;
        }

        bool isTabular = Analysis.IsTabular;
        bool isMultiLine = Analysis.NonEmptyLineCount > 1 || Analysis.LineCount > 1;
        bool isDelimitedSingle = Analysis.Format == DetectedFormat.DelimitedSingleLine;
        bool isStructured = Analysis.Format == DetectedFormat.Json ||
                            Analysis.Format == DetectedFormat.Yaml ||
                            Analysis.Format == DetectedFormat.Xml ||
                            _hasStructuredData;
        bool isCode = Analysis.Format == DetectedFormat.SqlInClause ||
                      Analysis.Format == DetectedFormat.Sql ||
                      Analysis.Format == DetectedFormat.KeyValuePairs ||
                      IsCodeLikeContent(_inputText);
        bool isSingleLineOrToken = Analysis.NonEmptyLineCount <= 1 && !isTabular && !isDelimitedSingle && !isStructured;

        // 1. Tabular Tab
        IsTabularTabHighlighted = isTabular;

        // 2. Lines Tab (relevant for any multiline text, delimited lines, or lists of items)
        IsLinesTabHighlighted = !isTabular && !isStructured && (isMultiLine || isDelimitedSingle);

        // 3. Structured Tab (relevant for JSON, YAML, XML)
        IsStructuredTabHighlighted = isStructured;

        // 4. Code Tab (relevant for SQL clauses, key-values, query strings, code-like content)
        IsCodeTabHighlighted = isCode || isStructured;

        // 5. Case / Enc Tab (relevant for single line text, words/tokens, base64, url-encoded, beautifiable formats)
        IsCaseEncTabHighlighted = isSingleLineOrToken ||
                                  Analysis.Format == DetectedFormat.Base64 ||
                                  Analysis.Format == DetectedFormat.UrlEncoded ||
                                  TextBeautifier.CanBeautify(_inputText);

        // 6. Presets Tab (useful for all non-empty text, especially multiline, delimited, or tabular extractions)
        IsPresetsTabHighlighted = isMultiLine || isDelimitedSingle || isTabular || isStructured;

        // Auto-select the most relevant sidebar tab (0: Lines, 1: Table, 2: Structured, 3: Code, 4: Case & Enc)
        if (isTabular)
        {
            SelectedSidebarTabIndex = 1; // Tabular / Table
        }
        else if (isStructured)
        {
            SelectedSidebarTabIndex = 2; // Structured
        }
        else if (isCode && (Analysis.Format == DetectedFormat.SqlInClause || Analysis.Format == DetectedFormat.Sql || Analysis.Format == DetectedFormat.KeyValuePairs))
        {
            SelectedSidebarTabIndex = 3; // Code
        }
        else if (isMultiLine || isDelimitedSingle)
        {
            SelectedSidebarTabIndex = 0; // Lines
        }
        else if (isSingleLineOrToken)
        {
            SelectedSidebarTabIndex = 4; // Case / Enc
        }
        else
        {
            SelectedSidebarTabIndex = 0; // Lines
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

        if (DefaultTextAnalyzer.IsSql(trimmed))
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
        if (_currentTable != null && !string.IsNullOrWhiteSpace(_surrogateHeaders))
        {
            var customHeaders = TabularParser.ParseHeaderList(_surrogateHeaders, _currentTable.Delimiter);
            if (customHeaders.Count > 0)
            {
                _currentTable.OverrideHeaders(customHeaders);
            }
        }

        UpdateColumnsAndPreviewFromCurrentTable();
        TriggerRealTime();
    }

    private void OnSurrogateHeadersChanged()
    {
        if (string.IsNullOrWhiteSpace(_inputText)) return;

        _currentTable = TabularParser.DetectAndParse(_inputText, _hasHeaders);
        if (_currentTable != null && !string.IsNullOrWhiteSpace(_surrogateHeaders))
        {
            var customHeaders = TabularParser.ParseHeaderList(_surrogateHeaders, _currentTable.Delimiter);
            if (customHeaders.Count > 0)
            {
                _currentTable.OverrideHeaders(customHeaders);
            }
        }

        UpdateColumnsAndPreviewFromCurrentTable();
        TriggerRealTime();
    }

    private void UpdateColumnsAndPreviewFromCurrentTable()
    {
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
            SelectedColumn = null;
            SelectedKeyColumn = null;
            SelectedValueColumn = null;
        }

        Analysis = TextAnalyzer.Analyze(_inputText, _currentTable?.HasHeaders ?? _hasHeaders);
        OnPropertyChanged(nameof(HasTabularData));
        UpdateTabHighlights();
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
                "ApplySurrogateHeaders" or "OverrideTableHeaders" => PrependSurrogateHeaderAction(),
                "GenerateSurrogateHeaders" => GenerateSurrogateHeadersAction(),
                "ClearSurrogateHeaders" => ClearSurrogateHeadersAction(),
                "PrependSurrogateHeader" or "AddSurrogateHeader" => PrependSurrogateHeaderAction(),

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
                "TableToKeyValueJson" => GenerateKeyValue(t => TabularConverter.ToKeyValueJson(t, GetKeyColIdx(), GetValColIdx(), KeyValueIncludeRestOfColumns)),
                "TableToKeyValueYaml" => GenerateKeyValue(t => TabularConverter.ToKeyValueYaml(t, GetKeyColIdx(), GetValColIdx(), KeyValueIncludeRestOfColumns)),
                "TableToKeyValueQuery" => GenerateKeyValue(t => TabularConverter.ToKeyValueQueryString(t, GetKeyColIdx(), GetValColIdx(), KeyValueIncludeRestOfColumns)),
                "TableToKeyValueJsonRest" => GenerateKeyValue(t => TabularConverter.ToKeyValueJson(t, GetKeyColIdx(), includeRestOfColumns: true)),
                "TableToKeyValueYamlRest" => GenerateKeyValue(t => TabularConverter.ToKeyValueYaml(t, GetKeyColIdx(), includeRestOfColumns: true)),
                "TableToKeyValueQueryRest" => GenerateKeyValue(t => TabularConverter.ToKeyValueQueryString(t, GetKeyColIdx(), includeRestOfColumns: true)),

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
                "DotCase" => CaseTransformers.ChangeCase(InputText, TextCasing.DotCase),
                "PathCase" => CaseTransformers.ChangeCase(InputText, TextCasing.PathCase),
                "ExtractEmails" => LineTransformers.ExtractRegex(InputText, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"),
                "ExtractUrls" => LineTransformers.ExtractRegex(InputText, @"https?:\/\/[^\s/$.?#].[^\s]*"),
                "ExtractIps" => LineTransformers.ExtractRegex(InputText, @"\b(?:\d{1,3}\.){3}\d{1,3}\b"),
                "ExtractGuids" => LineTransformers.ExtractRegex(InputText, @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b"),
                "ExtractNumbers" => LineTransformers.ExtractRegex(InputText, @"[-+]?\d*\.?\d+"),
                "ExtractRegexMatches" => _regexLabEngine.ExtractMatches(_regexLabResult),
                "ExtractRegexGroupsTable" => _regexLabEngine.ExtractGroupsAsDelimited(_regexLabResult, "\t"),
                "ExtractRegexGroupsJson" => _regexLabEngine.ExtractGroupsAsJson(_regexLabResult, true),

                // Encodings & Formatting
                "UrlEncode" => EncodingTransformers.UrlEncode(InputText),
                "UrlDecode" => EncodingTransformers.UrlDecode(InputText),
                "Base64Encode" => EncodingTransformers.Base64Encode(InputText),
                "Base64Decode" => EncodingTransformers.Base64Decode(InputText),
                "HtmlEncode" => EncodingTransformers.HtmlEncode(InputText),
                "HtmlDecode" => EncodingTransformers.HtmlDecode(InputText),
                "EscapeCSharp" => EncodingTransformers.EscapeCSharpString(InputText),
                "UnescapeCSharp" => EncodingTransformers.UnescapeCSharpString(InputText),
                "FormatJson" or "BeautifyJson" => EncodingTransformers.FormatJsonString(InputText),
                "FormatXml" or "BeautifyXml" => EncodingTransformers.FormatXmlString(InputText),
                "FormatYaml" or "BeautifyYaml" => EncodingTransformers.FormatYamlString(InputText),
                "MinifyJson" => EncodingTransformers.MinifyJson(InputText),
                "MinifyXml" => EncodingTransformers.MinifyXml(InputText),
                "Beautify" => TextBeautifier.Beautify(InputText),
                "JwtDecode" => EncodingTransformers.JwtDecode(InputText),

                // Structured Data Conversions & Operations
                "XmlToJson" => EncodingTransformers.XmlToJson(InputText),
                "JsonToXml" => EncodingTransformers.JsonToXml(InputText),
                "XmlToYaml" => EncodingTransformers.XmlToYaml(InputText),
                "YamlToXml" => EncodingTransformers.YamlToXml(InputText),
                "FlattenStructured" => EncodingTransformers.FlattenStructured(InputText),
                "FlattenToFlatJson" => EncodingTransformers.FlattenToFlatJson(InputText),
                "UnflattenStructured" or "UnflattenToJson" => EncodingTransformers.UnflattenStructured(InputText, "JSON"),
                "UnflattenToYaml" => EncodingTransformers.UnflattenStructured(InputText, "YAML"),
                "SortStructuredKeys" or "SortStructuredKeysAsc" => EncodingTransformers.SortStructuredKeys(InputText, false),
                "SortStructuredKeysDesc" => EncodingTransformers.SortStructuredKeys(InputText, true),
                "ExtractStructuredPaths" => EncodingTransformers.ExtractStructuredPaths(InputText),
                "ExtractStructuredKeys" => EncodingTransformers.ExtractStructuredKeys(InputText),
                "ExtractStructuredValues" => EncodingTransformers.ExtractStructuredValues(InputText),
                "StructuredCamelCase" => EncodingTransformers.ConvertStructuredKeysCase(InputText, TextCasing.CamelCase),
                "StructuredPascalCase" => EncodingTransformers.ConvertStructuredKeysCase(InputText, TextCasing.PascalCase),
                "StructuredSnakeCase" => EncodingTransformers.ConvertStructuredKeysCase(InputText, TextCasing.SnakeCase),
                "StructuredKebabCase" => EncodingTransformers.ConvertStructuredKeysCase(InputText, TextCasing.KebabCase),
                "StructuredConstantCase" => EncodingTransformers.ConvertStructuredKeysCase(InputText, TextCasing.ConstantCase),
                "PickStructuredKeys" => EncodingTransformers.PickStructuredKeys(InputText, StructuredFilterKeyList),
                "OmitStructuredKeys" => EncodingTransformers.OmitStructuredKeys(InputText, StructuredFilterKeyList),
                "RemoveNullsAndEmpty" => EncodingTransformers.RemoveNullsAndEmpty(InputText),
                "QueryStructuredPath" => EncodingTransformers.QueryStructuredPath(InputText, StructuredQueryPath),
                "QueryXPath" or "QueryStructuredXPath" => EncodingTransformers.QueryXPath(InputText, StructuredQueryPath),
                "ExtractXPathValues" => EncodingTransformers.ExtractXPathValues(InputText, StructuredQueryPath),
                "ExtractXPathAttributes" => EncodingTransformers.ExtractXPathAttributes(InputText, StructuredQueryPath),
                "StructuredToCsv" => EncodingTransformers.StructuredToCsv(InputText, ','),
                "StructuredToTsv" => EncodingTransformers.StructuredToTsv(InputText),
                "StructuredToMarkdown" => EncodingTransformers.StructuredToMarkdown(InputText),
                "ToTypeScriptInterfaces" => EncodingTransformers.ToTypeScriptInterfaces(InputText),
                "ToCSharpClasses" => EncodingTransformers.ToCSharpClasses(InputText),
                "ToJsonSchema" => EncodingTransformers.ToJsonSchema(InputText),

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

    private string PrependSurrogateHeaderAction()
    {
        var table = _currentTable?.Clone() ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || (table.Columns.Count == 0 && table.Rows.Count == 0)) return InputText;

        if (!string.IsNullOrWhiteSpace(_surrogateHeaders))
        {
            var customHeaders = TabularParser.ParseHeaderList(_surrogateHeaders, table.Delimiter);
            if (customHeaders.Count > 0)
            {
                table.OverrideHeaders(customHeaders);
            }
        }

        table.HasHeaders = true;
        if (table.Delimiter == '\t')
        {
            return TabularConverter.ToTsv(table);
        }
        else if (table.Delimiter == '|')
        {
            return TabularConverter.ToMarkdownTable(table);
        }
        else
        {
            return TabularConverter.ToCsv(table, table.Delimiter ?? ',');
        }
    }

    private string GenerateSurrogateHeadersAction()
    {
        int colCount = 0;
        if (_currentTable != null && _currentTable.Columns.Count > 0)
        {
            colCount = _currentTable.Columns.Count;
        }
        else if (_currentTable != null && _currentTable.Rows.Count > 0)
        {
            colCount = _currentTable.Rows.Max(r => r.Count);
        }
        else
        {
            var detected = TabularParser.DetectAndParse(InputText, _hasHeaders);
            colCount = detected?.Columns.Count ?? 3;
        }

        if (colCount <= 0) colCount = 3;
        var headers = TabularParser.GenerateSurrogateHeaders(colCount, "Col");
        SurrogateHeaders = string.Join(", ", headers);
        return PrependSurrogateHeaderAction();
    }

    private string ClearSurrogateHeadersAction()
    {
        SurrogateHeaders = string.Empty;
        var table = _currentTable ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
        if (table == null || (table.Columns.Count == 0 && table.Rows.Count == 0)) return InputText;
        if (table.Delimiter == '\t')
        {
            return TabularConverter.ToTsv(table);
        }
        else if (table.Delimiter == '|')
        {
            return TabularConverter.ToMarkdownTable(table);
        }
        else
        {
            return TabularConverter.ToCsv(table, table.Delimiter ?? ',');
        }
    }

    private string ConvertTabular(Func<TabularData, string> converter)
    {
        var table = _currentTable?.Clone() ?? TabularParser.DetectAndParse(InputText, _hasHeaders);
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
        if (!string.IsNullOrWhiteSpace(_surrogateHeaders))
        {
            var customHeaders = TabularParser.ParseHeaderList(_surrogateHeaders, table.Delimiter);
            if (customHeaders.Count > 0)
            {
                table.OverrideHeaders(customHeaders);
            }
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
