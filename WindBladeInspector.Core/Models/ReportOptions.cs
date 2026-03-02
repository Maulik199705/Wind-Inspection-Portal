namespace WindBladeInspector.Core.Models;

/// <summary>
/// Optional metadata that can be passed to customise report output.
/// </summary>
public record ReportOptions
{
    /// <summary>Name of the inspector who produced the report.</summary>
    public string InspectorName { get; init; } = "QualiMax Inspector";

    /// <summary>Company name shown on the report.</summary>
    public string CompanyName { get; init; } = "QualiMax";

    /// <summary>Optional override for the report date (defaults to today).</summary>
    public DateTime? ReportDate { get; init; }

    /// <summary>Whether to include defect images in the report (future expansion).</summary>
    public bool IncludeImages { get; init; } = false;
}
