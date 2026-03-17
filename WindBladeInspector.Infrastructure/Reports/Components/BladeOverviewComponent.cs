using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using WindBladeInspector.Core.Entities;
using WindBladeInspector.Core.Enums;
using WindBladeInspector.Infrastructure.Reports;

namespace WindBladeInspector.Infrastructure.Reports.Components;

internal sealed class BladeOverviewComponent
{
    private readonly Blade _blade;
    private readonly List<PdfReportGenerationService.DefectEntry> _defects;
    private readonly byte[]? _schematicBytes;

    private static string SeverityColor(int s) => s switch
    {
        1 => "#4CAF50",
        2 => "#8BC34A",
        3 => "#FFC107",
        4 => "#FF9800",
        5 => "#F44336",
        _ => "#AAAAAA"
    };

    public BladeOverviewComponent(Blade blade, List<PdfReportGenerationService.DefectEntry> defects, byte[]? schematicBytes = null)
    {
        _blade = blade;
        _defects = defects;
        _schematicBytes = schematicBytes;
    }

    public void Compose(IContainer container)
    {
        container.Column(col =>
        {
            // ── Title ──────────────────────────────────────────────────────
            col.Item().PaddingBottom(16)
               .AlignCenter() // Center the title
               .Text($"Blade {_blade.SerialNumber} Overview")
               .FontSize(14).Bold().FontColor("#4CAF50").Underline(); // Green and bold

            // ── Blade schematic image or fallback ──────────────────────────
            col.Item().PaddingBottom(24).AlignCenter().Element(c =>
            {
                if (_schematicBytes != null)
                {
                    c.MaxHeight(180).AlignCenter()
                     .Image(_schematicBytes, ImageScaling.FitArea);
                }
                else
                {
                    c.Height(120).Border(1).BorderColor("#DDDDDD").Background("#FAFAFA")
                     .AlignCenter().AlignMiddle()
                     .Text("BLADE SCHEMATIC\nLE  |  PS  |  TE  |  SS")
                     .FontSize(11).FontColor("#AAAAAA").Bold();
                }
            });

            // ── No defects message ─────────────────────────────────────────
            if (_defects.Count == 0)
            {
                col.Item().Padding(12).Border(1).BorderColor("#EEEEEE")
                   .AlignCenter().AlignMiddle()
                   .Text("No defects recorded for this blade.")
                   .FontSize(10).FontColor("#999999").Italic();
                return;
            }

            // ── Defect table ───────────────────────────────────────────────
            col.Item().AlignCenter().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1.2f); // Damage ID
                    cols.RelativeColumn(0.8f); // Blade side
                    cols.RelativeColumn(1.4f); // Material
                    cols.RelativeColumn(1.6f); // Damage Type
                    cols.RelativeColumn(1.4f); // Subtype
                    cols.ConstantColumn(55);   // Severity
                });

                // Header cells
                void HeaderCell(string text) =>
                    table.Cell()
                         .BorderBottom(2).BorderColor("#333333")
                         .Background("#F5F5F5")
                         .Padding(6)
                         .AlignCenter() // Center-align header text
                         .Text(text).Bold().FontSize(9).FontColor("#000000");

                HeaderCell("Damage ID");
                HeaderCell("Blade side");
                HeaderCell("Material");
                HeaderCell("Damage Type");
                HeaderCell("Subtype");
                HeaderCell("Severity");

                // Data rows
                foreach (var d in _defects)
                {
                    void DataCell(string text) =>
                        table.Cell()
                             .BorderBottom(1).BorderColor("#EEEEEE")
                             .Padding(6)
                             .AlignCenter() // Center-align data text
                             .Text(text).FontSize(9).FontColor("#333333");

                    string material = ResolveMaterial(d);

                    string subtype = d.Anomaly.Classification != null
                        ? (d.Anomaly.Classification.GetDefectSubtypeString() ?? "—")
                        : (string.IsNullOrWhiteSpace(d.Anomaly.Recommendation) ? "—" : d.Anomaly.Recommendation);

                    DataCell(d.DefectId);
                    DataCell(d.View);
                    DataCell(material);
                    DataCell(d.Anomaly.GetDefectTypeDisplay() ?? "—");
                    DataCell(subtype);

                    // Severity cell with color background
                    string bgColor = SeverityColor(d.Anomaly.Severity);
                    table.Cell()
                         .BorderBottom(1).BorderColor("#EEEEEE")
                         .Background(bgColor)
                         .AlignCenter().AlignMiddle()
                         .Padding(6)
                         .Text(d.Anomaly.Severity.ToString())
                         .FontSize(9).Bold().FontColor("#FFFFFF");
                }
            });
        });
    }

    /// <summary>
    /// Resolves the material/component label from the defect classification.
    /// </summary>
    private static string ResolveMaterial(PdfReportGenerationService.DefectEntry d)
    {
        var cls = d.Anomaly.Classification;
        if (cls == null) return "Auxiliary Component";

        if (cls.Category == ComponentCategory.AuxiliaryComponent)
        {
            return cls.AuxiliaryComponentType.HasValue
                ? FormatEnumName(cls.AuxiliaryComponentType.Value.ToString())
                : "Auxiliary Component";
        }

        return cls.BladeMaterial.HasValue
            ? cls.BladeMaterial.Value.ToString()
            : "Blade";
    }

    /// <summary>
    /// Converts a PascalCase enum name to a readable label.
    /// e.g. "LeadingEdgeProtection" → "Leading Edge Protection"
    /// </summary>
    private static string FormatEnumName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var result = new System.Text.StringBuilder();
        foreach (char c in name)
        {
            if (char.IsUpper(c) && result.Length > 0)
                result.Append(' ');
            result.Append(c);
        }
        return result.ToString();
    }
}