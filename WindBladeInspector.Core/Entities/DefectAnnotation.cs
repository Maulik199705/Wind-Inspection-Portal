namespace WindBladeInspector.Core.Entities;

/// <summary>
/// Represents a defect annotation on a blade image with calculated metrics.
/// </summary>
public class DefectAnnotation
{
    /// <summary>X coordinate of the defect box (pixels).</summary>
    public double X { get; set; }
    
    /// <summary>Y coordinate of the defect box (pixels).</summary>
    public double Y { get; set; }
    
    /// <summary>Width of the defect box (pixels).</summary>
    public double Width { get; set; }
    
    /// <summary>Height of the defect box (pixels).</summary>
    public double Height { get; set; }
    
    /// <summary>Calculated distance from blade root in meters.</summary>
    public double DistanceFromRootMeters { get; set; }
    
    /// <summary>Calculated area in square centimeters.</summary>
    public double AreaInSquareCm { get; set; }
    
    /// <summary>Severity level (1-5): 1=Cosmetic, 5=Very Serious.</summary>
    public int Severity { get; set; } = 1;
    
    /// <summary>Type of defect: Erosion, Crack, Pinholes, Peeling, Flaking, Discoloration, Other.</summary>
    public string Type { get; set; } = "Other";
}
