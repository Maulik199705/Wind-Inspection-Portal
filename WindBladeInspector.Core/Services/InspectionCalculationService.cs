using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Core.Services;

/// <summary>
/// Service for calculating defect metrics based on calibration data.
/// </summary>
public class InspectionCalculationService
{
    /// <summary>
    /// Calculates real-world metrics for a defect annotation.
    /// </summary>
    /// <param name="bladeLength">Total blade length in meters (for reference).</param>
    /// <param name="pixelsPerMeter">Calibrated scale in pixels per meter.</param>
    /// <param name="rootX">X coordinate of the blade root (pixels).</param>
    /// <param name="rootY">Y coordinate of the blade root (pixels).</param>
    /// <param name="defectX">X coordinate of the defect center (pixels).</param>
    /// <param name="defectY">Y coordinate of the defect center (pixels).</param>
    /// <param name="defectWidth">Width of the defect box (pixels).</param>
    /// <param name="defectHeight">Height of the defect box (pixels).</param>
    /// <returns>DefectAnnotation with calculated distance and area.</returns>
    public DefectAnnotation CalculateDefectMetrics(
        double bladeLength,
        double pixelsPerMeter,
        double rootX,
        double rootY,
        double defectX,
        double defectY,
        double defectWidth,
        double defectHeight)
    {
        // Calculate meters per pixel (inverse of pixels per meter)
        double metersPerPixel = 1.0 / pixelsPerMeter;

        // Calculate defect center
        double centerX = defectX + (defectWidth / 2.0);
        double centerY = defectY + (defectHeight / 2.0);

        // Calculate Euclidean distance in pixels from root to defect center
        double distancePx = Math.Sqrt(
            Math.Pow(centerX - rootX, 2) + 
            Math.Pow(centerY - rootY, 2));

        // Convert to meters
        double distanceMeters = distancePx * metersPerPixel;

        // Calculate area: convert pixel dimensions to centimeters (1 meter = 100 cm)
        double widthCm = defectWidth * metersPerPixel * 100.0;
        double heightCm = defectHeight * metersPerPixel * 100.0;
        double areaCm2 = widthCm * heightCm;

        return new DefectAnnotation
        {
            X = defectX,
            Y = defectY,
            Width = defectWidth,
            Height = defectHeight,
            DistanceFromRootMeters = Math.Round(distanceMeters, 2),
            AreaInSquareCm = Math.Round(areaCm2, 2)
        };
    }
}
