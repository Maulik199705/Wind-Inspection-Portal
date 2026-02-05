namespace WindBladeInspector.Core.Entities;

/// <summary>
/// Represents an inspection project for a wind turbine.
/// </summary>
public class InspectionProject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>Wind park/farm name.</summary>
    public string ParkName { get; set; } = string.Empty;
    
    /// <summary>Turbine identifier (e.g., "SG114-50").</summary>
    public string TurbineId { get; set; } = string.Empty;
    
    /// <summary>Turbine model (e.g., "SG114 MY17").</summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>Data capture status: "Complete", "In Progress", "Pending".</summary>
    public string DataCaptureStatus { get; set; } = "Pending";
    
    /// <summary>Analysis status: "Complete", "In Progress", "Pending".</summary>
    public string AnalysisStatus { get; set; } = "Pending";
    
    /// <summary>Inspection date.</summary>
    public DateTime InspectionDate { get; set; } = DateTime.Now;
    
    /// <summary>List of blades for this turbine.</summary>
    public List<Blade> Blades { get; set; } = new();
    
    /// <summary>Total anomaly count across all blades.</summary>
    public int TotalAnomalies => Blades.Sum(b => b.Anomalies?.Count ?? 0);
    
    /// <summary>Overall condition score (1-5, based on worst anomaly).</summary>
    public int OverallCondition => Blades.SelectMany(b => b.Anomalies ?? new())
                                         .Select(a => a.Severity)
                                         .DefaultIfEmpty(1)
                                         .Max();
}
