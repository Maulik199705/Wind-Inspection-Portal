namespace WindBladeInspector.Core.Entities;

/// <summary>
/// Represents a summary of a turbine inspection for the dashboard.
/// </summary>
public class TurbineInspectionSummary
{
    public Guid Id { get; set; }
    public string TurbineName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending" or "Complete"
}
