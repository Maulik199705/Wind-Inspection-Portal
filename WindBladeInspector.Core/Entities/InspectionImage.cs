namespace WindBladeInspector.Core.Entities;

/// <summary>
/// Represents a single image captued at a specific position on a blade.
/// </summary>
public class InspectionImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// URL or path to the image file.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Sequence order from root (0) to tip (N).
    /// </summary>
    public int SequenceOrder { get; set; }
    
    /// <summary>
    /// Approximate position in meters from the root.
    /// </summary>
    public double PositionMeters { get; set; }

    /// <summary>
    /// List of anomalies visible in this specific image.
    /// </summary>
    public List<Anomaly> Anomalies { get; set; } = new();
}
