using System.Collections.Generic;

namespace Reframe.Core.Recipes;

public class RecipeStepExecutionResult
{
    public int StepIndex { get; set; }
    public string StepTitle { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public string OutputPreview { get; set; } = string.Empty;
    public double ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RecipeResult
{
    public bool Success { get; set; } = true;
    public string Output { get; set; } = string.Empty;
    public List<RecipeStepExecutionResult> StepResults { get; set; } = new();
    public double TotalTimeMs { get; set; }
    public string? ErrorMessage { get; set; }

    public static RecipeResult Ok(string output, List<RecipeStepExecutionResult> stepResults, double totalTimeMs)
    {
        return new RecipeResult
        {
            Success = true,
            Output = output,
            StepResults = stepResults,
            TotalTimeMs = totalTimeMs
        };
    }

    public static RecipeResult Failed(string errorMessage, string partialOutput, List<RecipeStepExecutionResult> stepResults, double totalTimeMs)
    {
        return new RecipeResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Output = partialOutput,
            StepResults = stepResults,
            TotalTimeMs = totalTimeMs
        };
    }
}
