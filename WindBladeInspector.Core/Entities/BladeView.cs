namespace WindBladeInspector.Core.Entities;

/// <summary>
/// Represents a specific view/side of the blade (e.g., Pressure Side, Leading Edge).
/// </summary>
public class BladeView
{
    /// <summary>
    /// Side identifier: "PS" (Pressure Side), "SS" (Suction Side), "LE" (Leading Edge), "TE" (Trailing Edge).
    /// </summary>
    public string Side { get; set; } = string.Empty;

    /// <summary>
    /// Sequence of images covering this side from root to tip.
    /// </summary>
    public List<InspectionImage> Images { get; set; } = new();
}
