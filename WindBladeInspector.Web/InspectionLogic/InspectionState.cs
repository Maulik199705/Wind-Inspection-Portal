using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Web.InspectionLogic;

public class InspectionState
{
    public InspectionProject? Project { get; set; }

    public Blade? SelectedBlade { get; set; }
    public InspectionImage? SelectedImage { get; set; }
    public Anomaly? SelectedAnomaly { get; set; }

    public string SelectedView { get; set; } = "PS";
    public List<InspectionImage> SelectedViewImages { get; set; } = new();

    public bool IsVisualMode { get; set; }
    public bool IsCanvasInitialized { get; set; }

    public string ImageUrl { get; set; } = "";
    public string CurrentMode { get; set; } = "inspect";

    public double ZoomLevel { get; set; } = 1.0;

    public double CurrentWidthPx { get; set; }
    public double CurrentHeightPx { get; set; }
    public double CurrentWidthPct { get; set; }
    public double CurrentHeightPct { get; set; }

    public Anomaly? CurrentAnomaly { get; set; }
    public int CurrentCanvasBoxIndex { get; set; }

    public object? DotNetRef { get; set; }
}
