namespace WindBladeInspector.Core.Entities;

/// <summary>
/// Represents a detected anomaly/defect on a blade.
/// </summary>
public class Anomaly
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>Anomaly ID for display (e.g., "1350A7#6FF").</summary>
    public string AnomalyId { get; set; } = string.Empty;
    
    /// <summary>Severity level (1-5): 1=Cosmetic, 5=Very Serious.</summary>
    public int Severity { get; set; } = 1;
    
    /// <summary>
    /// [LEGACY] Type of anomaly: Erosion, Crack, Pinholes, Peeling, Flaking, Discoloration, etc.
    /// Kept for backward compatibility. New code should use Classification property.
    /// </summary>
    public string Type { get; set; } = "Other";
    
    /// <summary>
    /// [NEW] Hierarchical defect classification following the standardized schema.
    /// Blade > Material > DefectType > Subtype OR AuxiliaryComponent > Component > DefectType
    /// </summary>
    public DefectClassification? Classification { get; set; }
    
    /// <summary>Distance from blade root in meters (Radius).</summary>
    public double RadiusMeters { get; set; }
    
    /// <summary>Blade side: PS_LE, PS_TE, SS_LE, SS_TE, PS, SS.</summary>
    public string BladeSide { get; set; } = "PS";
    
    /// <summary>Location on blade: Root, Middle, Tip.</summary>
    public string Location { get; set; } = "Middle";
    
    /// <summary>Area in square centimeters.</summary>
    public double AreaCm2 { get; set; }
    
    /// <summary>Width in centimeters.</summary>
    public double WidthCm { get; set; }
    
    /// <summary>Part number/identifier.</summary>
    public int Part { get; set; }
    
    /// <summary>Image coordinates for the annotation box.</summary>
    public ImageCoordinates Coordinates { get; set; } = new();
    
    /// <summary>Recommendation for this anomaly.</summary>
    public string Recommendation { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the defect type string, prioritizing Classification over legacy Type.
    /// </summary>
    public string GetDefectTypeDisplay()
    {
        if (Classification != null)
        {
            return Classification.GetFullPath();
        }
        return Type;
    }
}

/// <summary>
/// Stores image coordinates for an anomaly annotation.
/// </summary>
public class ImageCoordinates
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    
    // Reference dimensions for calculating percentages
    public double ReferenceWidth { get; set; }
    public double ReferenceHeight { get; set; }
}
