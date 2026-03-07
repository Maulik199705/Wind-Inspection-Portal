//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;
//using SkiaSharp;
//using WindBladeInspector.Core.Entities;
//using WindBladeInspector.Core.Interfaces;
//using WindBladeInspector.Core.Models;
//using WindBladeInspector.Infrastructure.Reports.Components;

//namespace WindBladeInspector.Infrastructure.Reports;

///// <summary>
///// Orchestrates the multi-page PDF inspection report using QuestPDF.
///// Produces one section per blade containing annotated defect images + detail tables.
///// </summary>
//public sealed class PdfReportGenerationService : IReportGenerationService
//{
//    private readonly string _referenceDocsPath;
//    private readonly string _webRootPath;
//    private readonly ILogger<PdfReportGenerationService> _logger;

//    // Cached per-instance (Scoped lifetime) assets
//    private byte[]? _logoBytes;
//    private byte[]? _coverImageBytes;

//    // Brand palette
//    internal const string Navy     = "#0D1B2A";
//    internal const string Teal     = "#00A896";
//    internal const string White    = "#FFFFFF";
//    internal const string LightGray = "#E8EDF2";
//    internal const string MidGray   = "#6C7A89";

//    public PdfReportGenerationService(
//        IHostEnvironment environment,
//        ILogger<PdfReportGenerationService> logger,
//        string webRootPath)
//    {
//        _referenceDocsPath = Path.GetFullPath(
//            Path.Combine(environment.ContentRootPath, "..", "refrence_docs"));
//        _webRootPath = webRootPath;
//        _logger = logger;

//        QuestPDF.Settings.License = LicenseType.Community;
//    }

//    /// <inheritdoc/>
//    public Task<byte[]> GenerateProjectReportAsync(InspectionProject project,
//        ReportOptions? options = null)
//    {
//        ArgumentNullException.ThrowIfNull(project);

//        _logger.LogInformation("Generating PDF report for {TurbineId} ({ParkName})",
//            project.TurbineId, project.ParkName);

//        LoadAssets();

//        byte[] pdfBytes = Document.Create(container =>
//        {
//            // ── Cover Page ──────────────────────────────────────────────────
//            container.Page(page =>
//            {
//                page.Size(PageSizes.A4);
//                page.Margin(0);
//                page.Content().Element(c =>
//                    new CoverPageComponent(project, _logoBytes!, _coverImageBytes).Compose(c));
//            });

//            // ── Executive Summary ───────────────────────────────────────────
//            container.Page(page =>
//            {
//                page.Size(PageSizes.A4);
//                page.MarginHorizontal(35); page.MarginVertical(30);
//                page.Header().Element(c => PageHeader(c, project));
//                page.Footer().Element(c => new ReportFooterComponent(project).Compose(c));
//                page.Content().Element(c => new ExecutiveSummaryComponent(project).Compose(c));
//            });

//            // ── Per-Blade + Per-Defect sections ─────────────────────────────
//            foreach (var (blade, bladeIdx) in project.Blades.Select((b, i) => (b, i + 1)))
//            {
//                // Blade divider page
//                container.Page(page =>
//                {
//                    page.Size(PageSizes.A4);
//                    page.MarginHorizontal(35); page.MarginTop(25); page.MarginBottom(30);
//                    page.Header().Element(c => PageHeader(c, project));
//                    page.Footer().Element(c => new ReportFooterComponent(project).Compose(c));
//                    page.Content().Element(c =>
//                        new BladeOverviewComponent(blade, bladeIdx - 1).Compose(c));
//                });

//                // Collect all defects across all views of this blade
//                var allDefects = CollectDefects(blade);

//                if (allDefects.Count == 0)
//                {
//                    // "No defects found" page for this blade
//                    container.Page(page =>
//                    {
//                        page.Size(PageSizes.A4);
//                        page.MarginHorizontal(35); page.MarginTop(25); page.MarginBottom(30);
//                        page.Header().Element(c => PageHeader(c, project));
//                        page.Footer().Element(c => new ReportFooterComponent(project).Compose(c));
//                        page.Content().AlignCenter().AlignMiddle()
//                            .Text("No defects recorded for this blade.")
//                            .FontSize(14).FontColor(MidGray);
//                    });
//                    continue;
//                }

//                // One page per defect
//                foreach (var (defect, imgUrl, view, defectNo) in allDefects)
//                {
//                    container.Page(page =>
//                    {
//                        page.Size(PageSizes.A4);
//                        page.MarginHorizontal(35); page.MarginTop(25); page.MarginBottom(30);
//                        page.Header().Element(c => PageHeader(c, project));
//                        page.Footer().Element(c => new ReportFooterComponent(project).Compose(c));

//                        page.Content().Column(col =>
//                        {
//                            // Defect heading
//                            col.Item().PaddingBottom(6).Row(row =>
//                            {
//                                row.RelativeItem()
//                                    .Text($"Blade {bladeIdx}  ›  Defect {defectNo:D2}  ›  {view} — {GetViewLabel(view)}")
//                                    .FontSize(12).FontColor(Navy).Bold();

//                                row.AutoItem().AlignRight()
//                                    .Element(c => SeverityBadge(c, defect.Severity));
//                            });

//                            col.Item().PaddingBottom(6)
//                                .BorderBottom(1).BorderColor(LightGray);

//                            // ── Annotated blade image ────────────────────────
//                            col.Item().PaddingBottom(10).Element(c =>
//                            {
//                                var imgBytes = LoadImageBytes(imgUrl);
//                                if (imgBytes != null)
//                                {
//                                    // Show ALL defects on this image, but highlight current one
//                                    var allOnThisImg = allDefects
//                                        .Where(d => d.ImageUrl == imgUrl)
//                                        .Select(d => (d.Anomaly, d.DefectNo))
//                                        .ToList();

//                                    new DefectAnnotatedImageComponent(imgBytes, allOnThisImg)
//                                        .Compose(c);
//                                }
//                                else
//                                {
//                                    c.Background(LightGray).Padding(20)
//                                     .AlignCenter().Text("Image not available")
//                                     .FontSize(11).FontColor(MidGray).Italic();
//                                }
//                            });

//                            // ── Defect details table ─────────────────────────
//                            col.Item().Element(c =>
//                                DefectDetailsTable(c, defect, bladeIdx, defectNo, blade));
//                        });
//                    });
//                }
//            }

//            // ── Back Cover ──────────────────────────────────────────────────
//            container.Page(page =>
//            {
//                page.Size(PageSizes.A4);
//                page.Margin(0);
//                page.Content().Background(Navy).AlignCenter().AlignMiddle().Column(col =>
//                {
//                    col.Item().AlignCenter().MaxWidth(180).Image(_logoBytes!).FitWidth();
//                    col.Item().PaddingTop(24).AlignCenter()
//                       .Text("QualiMax Wind Services").FontSize(22).FontColor(White).Bold();
//                    col.Item().PaddingTop(8).AlignCenter()
//                       .Text("Confidential Inspection Report").FontSize(12).FontColor(Teal);
//                    col.Item().PaddingTop(32).AlignCenter()
//                       .Text($"Generated: {DateTime.Now:dd MMMM yyyy  HH:mm}")
//                       .FontSize(10).FontColor($"{White}99");
//                    col.Item().PaddingTop(6).AlignCenter()
//                       .Text($"Turbine: {project.TurbineId}  |  {project.ParkName}")
//                       .FontSize(10).FontColor($"{White}99");
//                });
//            });

//        }).GeneratePdf();

//        _logger.LogInformation("PDF generated — {Bytes:N0} bytes", pdfBytes.Length);
//        return Task.FromResult(pdfBytes);
//    }

//    // ── Private helpers ──────────────────────────────────────────────────────

//    private record DefectEntry(
//        Anomaly Anomaly, string ImageUrl, string View, int DefectNo);

//    private static List<DefectEntry> CollectDefects(Blade blade)
//    {
//        var list = new List<DefectEntry>();
//        int n = 1;
//        foreach (var viewOrder in new[] { "PS", "SS", "LE", "TE" })
//        {
//            var view = blade.Views.FirstOrDefault(v => v.Side == viewOrder);
//            if (view == null) continue;
//            foreach (var img in view.Images.OrderBy(i => i.SequenceOrder))
//                foreach (var a in img.Anomalies.OrderByDescending(a => a.Severity))
//                    list.Add(new DefectEntry(a, img.ImageUrl, view.Side, n++));
//        }
//        return list;
//    }

//    private byte[]? LoadImageBytes(string imageUrl)
//    {
//        if (string.IsNullOrWhiteSpace(imageUrl)) return null;

//        // imageUrl is like "/blade-images/guid.jpg"
//        var relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
//        var fullPath = Path.Combine(_webRootPath, relative);

//        if (!File.Exists(fullPath))
//        {
//            _logger.LogWarning("Blade image not found: {Path}", fullPath);
//            return null;
//        }
//        return File.ReadAllBytes(fullPath);
//    }

//    private static void PageHeader(IContainer container, InspectionProject project)
//    {
//        container.PaddingBottom(10)
//            .BorderBottom(1).BorderColor(Teal)
//            .Row(row =>
//            {
//                row.RelativeItem().Text(txt =>
//                {
//                    txt.Span("QualiMax  ").FontSize(10).FontColor(Navy).Bold();
//                    txt.Span("Wind Turbine Blade Inspection Report")
//                       .FontSize(10).FontColor(MidGray);
//                });
//                row.ConstantItem(200).AlignRight()
//                   .Text($"{project.TurbineId}  |  {project.InspectionDate:dd MMM yyyy}")
//                   .FontSize(9).FontColor(MidGray);
//            });
//    }

//    private static void DefectDetailsTable(
//        IContainer container, Anomaly defect,
//        int bladeIdx, int defectNo, Blade blade)
//    {
//        var coords = defect.Coordinates;

//        container.Table(table =>
//        {
//            table.ColumnsDefinition(cols =>
//            {
//                cols.ConstantColumn(140);
//                cols.RelativeColumn();
//                cols.ConstantColumn(140);
//                cols.RelativeColumn();
//            });

//            void Row2(string l1, string v1, string l2, string v2)
//            {
//                table.Cell().Padding(6).Background(LightGray)
//                    .Text(l1).FontSize(9).FontColor(Navy).Bold();
//                table.Cell().Padding(6)
//                    .Text(v1).FontSize(9).FontColor(Navy);
//                table.Cell().Padding(6).Background(LightGray)
//                    .Text(l2).FontSize(9).FontColor(Navy).Bold();
//                table.Cell().Padding(6)
//                    .Text(v2).FontSize(9).FontColor(Navy);
//            }

//            Row2("Defect Reference",
//                 $"B{bladeIdx}-{defectNo:D2}",
//                 "Blade Serial",
//                 blade.SerialNumber);

//            Row2("Defect Type",
//                 defect.GetDefectTypeDisplay(),
//                 "Severity Level",
//                 $"Level {defect.Severity} — {GetSeverityLabel(defect.Severity)}");

//            Row2("Blade Side",
//                 defect.BladeSide ?? "—",
//                 "Blade View",
//                 "—");

//            if (coords != null)
//            {
//                Row2("Dimensions (px)",
//                     $"{coords.Width:F0} × {coords.Height:F0} px",
//                     "Area (cm²)",
//                     $"{defect.AreaCm2:F2} cm²");

//                if (coords.ReferenceWidth > 0 && coords.ReferenceHeight > 0)
//                {
//                    var areaFraction = coords.Width * coords.Height
//                                      / (coords.ReferenceWidth * coords.ReferenceHeight) * 100;

//                    Row2("% of Image Area",
//                         $"{areaFraction:F3}%",
//                         "Radial Position",
//                         "—");
//                }
//            }

//            // Recommendation spans full width
//            if (!string.IsNullOrWhiteSpace(defect.Recommendation))
//            {
//                table.Cell().ColumnSpan(4).Padding(6).Background(LightGray)
//                    .Text("Recommendation").FontSize(9).FontColor(Navy).Bold();
//                table.Cell().ColumnSpan(4).Padding(6)
//                    .Text(defect.Recommendation).FontSize(9).FontColor(Navy);
//            }
//        });
//    }

//    private static void SeverityBadge(IContainer container, int severity)
//    {
//        var (bg, label) = severity switch
//        {
//            1 => ("#4CAF50", "SEV 1 – Cosmetic"),
//            2 => ("#8BC34A", "SEV 2 – Minor"),
//            3 => ("#FF9800", "SEV 3 – Medium"),
//            4 => ("#F44336", "SEV 4 – Serious"),
//            5 => ("#B71C1C", "SEV 5 – Critical"),
//            _ => ("#607D8B", $"SEV {severity}")
//        };

//        container.Background(bg).PaddingHorizontal(8).PaddingVertical(4)
//            .Text(label).FontSize(9).FontColor(White).Bold();
//    }

//    private static string GetSeverityLabel(int sev) => sev switch
//    {
//        1 => "Cosmetic", 2 => "Minor", 3 => "Medium",
//        4 => "Serious",  5 => "Critical", _ => "Unknown"
//    };

//    private static string GetViewLabel(string side) => side switch
//    {
//        "PS" => "Pressure Side",
//        "SS" => "Suction Side",
//        "LE" => "Leading Edge",
//        "TE" => "Trailing Edge",
//        _    => side
//    };

//    private void LoadAssets()
//    {
//        if (_logoBytes != null) return;

//        var logoPath = Path.Combine(_referenceDocsPath, "Q LOGO.png");
//        _logoBytes = File.Exists(logoPath)
//            ? File.ReadAllBytes(logoPath)
//            : Convert.FromBase64String(
//                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

//        var coverPath = Path.Combine(_referenceDocsPath, "pexels-merictuna-31502929.jpg.jpeg");
//        _coverImageBytes = File.Exists(coverPath) ? File.ReadAllBytes(coverPath) : null;
//    }
//}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using WindBladeInspector.Core.Entities;
using WindBladeInspector.Core.Interfaces;
using WindBladeInspector.Core.Models;
using WindBladeInspector.Infrastructure.Reports.Components;

namespace WindBladeInspector.Infrastructure.Reports;

public sealed class PdfReportGenerationService : IReportGenerationService
{
    private readonly string _referenceDocsPath;
    private readonly string _webRootPath;
    private readonly ILogger<PdfReportGenerationService> _logger;

    private byte[]? _logoBytes;

    // Brand Palette matching the document
    internal const string Black = "#000000";
    internal const string DarkGray = "#333333";
    internal const string LightGray = "#F0F0F0";
    internal const string White = "#FFFFFF";

    public PdfReportGenerationService(
        IHostEnvironment environment,
        ILogger<PdfReportGenerationService> logger,
        string webRootPath)
    {
        _referenceDocsPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "refrence_docs"));
        _webRootPath = webRootPath;
        _logger = logger;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateProjectReportAsync(InspectionProject project, ReportOptions? options = null)
    {
        LoadAssets();

        byte[] pdfBytes = Document.Create(container =>
        {
            var allDefects = project.Blades.ToDictionary(b => b.SerialNumber, b => CollectDefects(b));

            // 1. Cover Page
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Content().Element(c => new CoverPageComponent(project,_logoBytes).Compose(c));
            });

            // 2. Contents Page
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Header().Element(c => PageHeader(c, "01 CONTENTS"));
                page.Content().Element(c => new ContentsPageComponent(project).Compose(c));
            });

            // 3. Introduction - Details
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Header().Element(c => PageHeader(c, "02 INTRODUCTION"));
                page.Content().Element(c => new InspectionDetailsComponent(project).Compose(c));
            });

            // 4. Introduction - General Overview
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Header().Element(c => PageHeader(c, "03 INTRODUCTION\nGeneral Turbine Overview"));
                page.Content().Element(c => new GeneralTurbineOverviewComponent(project).Compose(c));
            });

            // 5. Blade Sections
            int globalPageNum = 4; // Tracking page numbers based on the sample layout
            foreach (var blade in project.Blades)
            {
                var bladeDefects = allDefects[blade.SerialNumber];

                // Blade Overview
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    string headerNum = (globalPageNum++).ToString("D2");
                    page.Header().Element(c => PageHeader(c, $"{headerNum} BLADE {blade.SerialNumber}"));
                    page.Content().Element(c => new BladeOverviewComponent(blade, bladeDefects).Compose(c));
                });

                // Blade Defects
                foreach (var defect in bladeDefects)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        string headerNum = (globalPageNum++).ToString("D2");
                        page.Header().Element(c => PageHeader(c, $"{headerNum} BLADE {blade.SerialNumber}"));

                        page.Content().Element(c =>
                        {
                            var imgBytes = LoadImageBytes(defect.ImageUrl);
                            new DefectDetailComponent(blade.SerialNumber, defect, imgBytes).Compose(c);
                        });
                    });
                }
            }

            // 6. Back Cover
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Content().AlignCenter().AlignMiddle().Column(c =>
                {
                    c.Item().Text("KEEP YOUR BLADES SAFER WITH...").FontSize(16).Bold();
                    c.Item().PaddingTop(20).Text("QUALIMAX\nSERVICES").FontSize(24).Bold().FontColor(DarkGray);
                    c.Item().PaddingTop(20).Text("www.qmservice.in").FontSize(12).FontColor(DarkGray);
                });
            });

        }).GeneratePdf();

        return Task.FromResult(pdfBytes);
    }

    private void PageHeader(IContainer container, string title)
    {
        container.PaddingBottom(20).Row(row =>
        {
            row.RelativeItem().Text(title).FontSize(14).Bold().FontColor(Black);
            row.ConstantItem(150).AlignRight().Text("QUALIMAX SERVICES").FontSize(10).FontColor(DarkGray).Bold();
        });
    }

    private byte[]? LoadImageBytes(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return null;
        var relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_webRootPath, relative);
        return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
    }

    private void LoadAssets()
    {
        if (_logoBytes != null) return;
        var logoPath = Path.Combine(_referenceDocsPath, "Q LOGO.png");
        _logoBytes = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : Array.Empty<byte>();
    }

    public record DefectEntry(Anomaly Anomaly, string ImageUrl, string View, int DefectNo, string DefectId);

    private static List<DefectEntry> CollectDefects(Blade blade)
    {
        var list = new List<DefectEntry>();
        int n = 1;
        foreach (var viewOrder in new[] { "LE", "PS", "SS", "TE" })
        {
            var view = blade.Views.FirstOrDefault(v => v.Side == viewOrder);
            if (view == null) continue;
            foreach (var img in view.Images.OrderBy(i => i.SequenceOrder))
                foreach (var a in img.Anomalies.OrderByDescending(a => a.Severity))
                {
                    string dId = $"120D{blade.SerialNumber}{n:D2}";
                    list.Add(new DefectEntry(a, img.ImageUrl, view.Side, n++, dId));
                }
        }
        return list;
    }
}