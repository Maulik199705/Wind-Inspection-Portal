using System;
using System.Collections.Generic;
using System.Linq;

namespace WindBladeInspector.Core.Entities;

/// <summary>
/// Represents coordinate data for polygon-based defect annotations.
/// Supports both legacy rectangular boxes and new 4-point polygons.
/// </summary>
public class ImageCoordinates
{
    /// <summary>[LEGACY] Top-left X coordinate for rectangular boxes.</summary>
    public double X { get; set; }

    /// <summary>[LEGACY] Top-left Y coordinate for rectangular boxes.</summary>
    public double Y { get; set; }

    /// <summary>[LEGACY] Width for rectangular boxes.</summary>
    public double Width { get; set; }

    /// <summary>[LEGACY] Height for rectangular boxes.</summary>
    public double Height { get; set; }

    /// <summary>
    /// [NEW] Polygon points (4 points for quadrilateral).
    /// Format: [x1, y1, x2, y2, x3, y3, x4, y4]
    /// </summary>
    public List<double> PolygonPoints { get; set; } = new();

    /// <summary>Reference image width (for scaling).</summary>
    public double ReferenceWidth { get; set; }

    /// <summary>Reference image height (for scaling).</summary>
    public double ReferenceHeight { get; set; }

    /// <summary>
    /// Check if this uses the new polygon format (4 points).
    /// </summary>
    public bool IsPolygon => PolygonPoints?.Count == 8;

    /// <summary>
    /// Calculate actual area in pixels using Shoelace formula for polygons,
    /// or simple width*height for legacy rectangles.
    /// </summary>
    public double GetAreaInPixels()
    {
        if (IsPolygon)
        {
            return CalculatePolygonArea();
        }
        return Width * Height;
    }

    /// <summary>
    /// Calculate polygon area using Shoelace formula.
    /// </summary>
    private double CalculatePolygonArea()
    {
        if (!IsPolygon) return 0;

        double area = 0;
        int n = 4; // 4 points

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            double x1 = PolygonPoints[i * 2];
            double y1 = PolygonPoints[i * 2 + 1];
            double x2 = PolygonPoints[j * 2];
            double y2 = PolygonPoints[j * 2 + 1];

            area += x1 * y2;
            area -= x2 * y1;
        }

        return Math.Abs(area / 2.0);
    }

    /// <summary>
    /// Get bounding box dimensions for display purposes.
    /// </summary>
    public (double width, double height) GetBoundingBoxDimensions()
    {
        if (IsPolygon)
        {
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            for (int i = 0; i < 4; i++)
            {
                double x = PolygonPoints[i * 2];
                double y = PolygonPoints[i * 2 + 1];
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }

            return (maxX - minX, maxY - minY);
        }

        return (Width, Height);
    }
}

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

    /// <summary>Image coordinates for the annotation (supports both rectangular and polygon).</summary>
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
            var defectType = Classification.GetDefectTypeString();
            var subtype = Classification.GetDefectSubtypeString();

            if (!string.IsNullOrEmpty(subtype) && subtype != "None")
            {
                return $"{defectType} - {subtype}";
            }
            return defectType;
        }

        return Type;
    }
}