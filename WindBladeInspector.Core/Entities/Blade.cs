namespace WindBladeInspector.Core.Entities;

/// <summary>
/// Represents a turbine blade with its anomalies.
/// </summary>
public class Blade
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>Blade serial/identifier (e.g., "A", "B", "C").</summary>
    public string SerialNumber { get; set; } = string.Empty;
    
    /// <summary>Blade length in meters.</summary>
    public double Length { get; set; }
    
    /// <summary>List of detected anomalies on this blade.</summary>
    public List<Anomaly> Anomalies { get; set; } = new();
    
    /// <summary>Overall blade condition (worst severity).</summary>
    public int Condition => Anomalies.Select(a => a.Severity).DefaultIfEmpty(1).Max();
    
    /// <summary>
    /// List of views/sides for this blade, each containing a sequence of images.
    /// </summary>
    public List<BladeView> Views { get; set; } = new();
}
