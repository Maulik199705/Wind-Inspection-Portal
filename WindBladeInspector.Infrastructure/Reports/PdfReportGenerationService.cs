using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindBladeInspector.Core.Entities;
using WindBladeInspector.Core.Enums;
using WindBladeInspector.Core.Interfaces;
using WindBladeInspector.Core.Models;
using WindBladeInspector.Infrastructure.Reports.Components;

namespace WindBladeInspector.Infrastructure.Reports;

public sealed class PdfReportGenerationService : IReportGenerationService
{
    private readonly string _referenceDocsPath;

    /// <summary>Path to App_Data/blade-images — where uploaded blade photos are stored.</summary>
    private readonly string _bladeImagesDir;

    /// <summary>Path to wwwroot — where Cover.jpeg, Logo.png and other static assets live.</summary>
    private readonly string _wwwRootPath;

    private readonly ILogger<PdfReportGenerationService> _logger;

    private byte[]? _logoBytes;
    private byte[]? _coverImageBytes;
    private byte[]? _schematicBytes;
    private byte[]? _backCoverImageBytes;

    internal const string Black = "#000000";
    internal const string DarkGray = "#333333";
    internal const string LightGray = "#F0F0F0";
    internal const string White = "#FFFFFF";

    public static object DefectSubtype { get; private set; }

    public PdfReportGenerationService(
        IHostEnvironment environment,
        ILogger<PdfReportGenerationService> logger,
        string bladeImagesDir,
        string wwwRootPath)
    {
        _referenceDocsPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "refrence_docs"));
        _bladeImagesDir = bladeImagesDir;
        _wwwRootPath = wwwRootPath;
        _logger = logger;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateProjectReportAsync(InspectionProject project, ReportOptions? options = null)
    {
        LoadAssets();

        var allDefects = project.Blades.ToDictionary(b => b.SerialNumber, b => CollectDefects(b));
        var pageNumbers = CalculatePageNumbers(project, allDefects);

        byte[] pdfBytes = Document.Create(container =>
        {
            // 1. Cover Page
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0, Unit.Point);
                page.Content().Element(c =>
                    new CoverPageComponent(project, _logoBytes, _coverImageBytes).Compose(c));
            });

            // 2. Contents Page
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.Header().Element(c => PageHeader(c, "CONTENTS"));
                page.Content().Section("ContentsPage")
                              .Element(c => new ContentsPageComponent(project, pageNumbers).Compose(c));
            });

            // 3. Introduction - Details
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.Header().Element(c => PageHeader(c, "01 INTRODUCTION"));
                page.Content().Section("IntroSection")
                              .Element(c => new InspectionDetailsComponent(project).Compose(c));
            });

            // 4. General Overview
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.Header().Element(c => PageHeader(c, "02 GENERAL TURBINE OVERVIEW"));
                page.Content().Section("General Overview")
                              .Element(c => new GeneralTurbineOverviewComponent(project).Compose(c));
            });

            // 5. Blade Sections
            int globalPageNum = 4;
            foreach (var blade in project.Blades)
            {
                var bladeDefects = allDefects[blade.SerialNumber];

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15);
                    string headerNum = (globalPageNum++).ToString("D2");
                    page.Header().Element(c => PageHeader(c, $"{headerNum}  BLADE {blade.SerialNumber}"));
                    page.Content().Section($"BladeOverview_{blade.SerialNumber}")
                                  .Element(c => new BladeOverviewComponent(blade, bladeDefects, _schematicBytes).Compose(c));
                });

                bool isFirstDefectPage = true;
                foreach (var defect in bladeDefects)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(15);
                        string headerNum = (globalPageNum++).ToString("D2");
                        page.Header().Element(c => PageHeader(c, $"{headerNum}  BLADE {blade.SerialNumber}"));

                        page.Content().Column(col =>
                        {
                            if (isFirstDefectPage)
                            {
                                col.Item().Section($"BladeDetails_{blade.SerialNumber}");
                                isFirstDefectPage = false;
                            }
                            col.Item().Element(c =>
                            {
                                var imgBytes = LoadBladeImageBytes(defect.ImageUrl);
                                new DefectDetailComponent(blade.SerialNumber, defect, imgBytes).Compose(c);
                            });
                        });
                    });
                }
            }

            // 6. Back Cover
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0, Unit.Point);
                page.Content().Element(c =>
                    new BackCoverPageComponent(_logoBytes, _backCoverImageBytes).Compose(c));
            });

        }).GeneratePdf();

        _logger.LogInformation("PDF generated — {Bytes:N0} bytes", pdfBytes.Length);
        return Task.FromResult(pdfBytes);
    }

    private Dictionary<string, int> CalculatePageNumbers(
        InspectionProject project,
        Dictionary<string, List<DefectEntry>> allDefects)
    {
        var pageMap = new Dictionary<string, int>();
        int currentPage = 1;
        currentPage++; // Cover

        pageMap["Contents"] = currentPage++;
        pageMap["IntroSection"] = currentPage++;
        pageMap["GeneralOverview"] = currentPage++;

        foreach (var blade in project.Blades)
        {
            var bladeDefects = allDefects[blade.SerialNumber];
            pageMap[$"BladeOverview_{blade.SerialNumber}"] = currentPage++;

            if (bladeDefects.Count > 0)
            {
                pageMap[$"BladeDetails_{blade.SerialNumber}"] = currentPage;
                currentPage += bladeDefects.Count;
            }
        }

        return pageMap;
    }

    private void PageHeader(IContainer container, string title)
    {
        container.PaddingBottom(20).AlignCenter().Row(row =>
        {
            row.RelativeItem().Text(title).FontSize(14).Bold().FontColor("#000000"); // Green and bold

            row.ConstantItem(150).AlignRight().Text("QUALIMAX SERVICES").FontSize(10).FontColor(DarkGray).Bold();
        });

    }

    /// <summary>
    /// Loads a blade inspection photo from the persistent blade-images directory.
    /// URLs are in the form /blade-images/filename.jpg
    /// </summary>
    private byte[]? LoadBladeImageBytes(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return null;

        // Strip the /blade-images/ prefix — files live directly in _bladeImagesDir
        var fileName = Path.GetFileName(imageUrl);
        var fullPath = Path.Combine(_bladeImagesDir, fileName);

        if (File.Exists(fullPath)) return File.ReadAllBytes(fullPath);

        // Fallback: try resolving relative to wwwroot for legacy paths
        var relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var legacyPath = Path.Combine(_wwwRootPath, relative);
        return File.Exists(legacyPath) ? File.ReadAllBytes(legacyPath) : null;
    }

    /// <summary>
    /// Loads static brand assets (logo, cover image, blade schematic) from wwwroot/Images.
    /// Candidate filenames for schematic: BladeSchematic.png, Schematic.png, blade-schematic.png, 
    /// BladeSchematic.jpg, Schematic.jpg (tried in order, first match wins).
    /// </summary>
    private void LoadAssets()
    {
        if (_logoBytes != null) return;

        var imagesDir = Path.Combine(_wwwRootPath, "Images");

        var logoPath = Path.Combine(imagesDir, "Logo.png");
        var coverPath = Path.Combine(imagesDir, "Cover.jpeg");
        var backcoverPath = Path.Combine(imagesDir, "backcover.jpeg");

        _logoBytes = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : Array.Empty<byte>();
        _coverImageBytes = File.Exists(coverPath) ? File.ReadAllBytes(coverPath) : null;
        _backCoverImageBytes = File.Exists(backcoverPath) ? File.ReadAllBytes(backcoverPath) : null;

        // Try common schematic filenames in order of preference
        var schematicCandidates = new[]
        {
            "bladeSchmeatic.png"

        };

        foreach (var candidate in schematicCandidates)
        {
            var path = Path.Combine(imagesDir, candidate);
            if (File.Exists(path))
            {
                _schematicBytes = File.ReadAllBytes(path);
                _logger.LogInformation("Blade schematic loaded: {File}", candidate);
                break;
            }
        }

        _logger.LogInformation(
            "Assets loaded — Logo: {Logo}, Cover: {Cover}, Schematic: {Schematic}",
            _logoBytes.Length > 0 ? "OK" : "MISSING",
            _coverImageBytes != null ? "OK" : $"MISSING at {coverPath}",
            _schematicBytes != null ? "OK" : $"MISSING (tried {string.Join(", ", schematicCandidates)})");
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
            {
                foreach (var anomaly in img.Anomalies.OrderByDescending(a => a.Severity))
                {

                    // Ensure Classification is populated
                    if (anomaly.Classification != null)
                    {

                        switch (anomaly.Classification.BladeMaterial)
                        {
                            case BladeMaterial.Surface:
                                switch ((SurfaceDefectType)anomaly.Classification.DefectType)
                                {
                                    case SurfaceDefectType.Discoloration:
                                        if (!anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)SurfaceDiscolorationSubtype.Mechanical;
                                        }
                                        if (anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            switch ((SurfaceDiscolorationSubtype)anomaly.Classification.DefectSubtype)
                                            {
                                                case SurfaceDiscolorationSubtype.Mechanical:
                                                    // Handle Mechanical subtype
                                                    break;
                                                case SurfaceDiscolorationSubtype.Scorch:
                                                    // Handle Scorch subtype
                                                    break;
                                                case SurfaceDiscolorationSubtype.IceContamination:
                                                    // Handle Ice Contamination subtype
                                                    break;
                                                default:
                                                    anomaly.Classification.DefectSubtype = null; // No subtype
                                                    break;
                                            }
                                        }
                                        else
                                        {

                                            // Assign a default subtype if necessary
                                            anomaly.Classification.DefectSubtype = (int?)SurfaceDiscolorationSubtype.Mechanical;
                                        }
                                        break;

                                    case SurfaceDefectType.Erosion:
                                        if (!anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)SurfaceErosionSubtype.Chip;
                                        }
                                        if (anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            switch ((SurfaceErosionSubtype)anomaly.Classification.DefectSubtype.Value)
                                            {
                                                case SurfaceErosionSubtype.Chip:
                                                    // Handle Chip subtype
                                                    break;
                                                case SurfaceErosionSubtype.Flaking:
                                                    // Handle Flaking subtype
                                                    break;
                                                default:
                                                    anomaly.Classification.DefectSubtype = null; // No subtype
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            anomaly.Classification.DefectSubtype = null;
                                        }
                                        break;

                                    default:
                                        anomaly.Classification.DefectSubtype = null; // No subtype
                                        break;
                                }
                                break;

                            case BladeMaterial.TopCoat:
                                switch ((TopCoatDefectType)anomaly.Classification.DefectType)
                                {
                                    case TopCoatDefectType.Crack:
                                        if (!anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)TopCoatCrackSubtype.FatigueCracks;
                                        }
                                        if (anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            switch ((TopCoatCrackSubtype)anomaly.Classification.DefectSubtype.Value)
                                            {
                                                case TopCoatCrackSubtype.FatigueCracks:
                                                case TopCoatCrackSubtype.TLCShapedCracks:
                                                case TopCoatCrackSubtype.SpiderWebShaped:
                                                case TopCoatCrackSubtype.BondTransverseOnTE:
                                                case TopCoatCrackSubtype.BondLongitudinalOnTE:
                                                case TopCoatCrackSubtype.BondTransverseOnLE:
                                                case TopCoatCrackSubtype.BondLongitudinalOnLE:
                                                    // Handle specific subtypes
                                                    break;
                                                default:
                                                    anomaly.Classification.DefectSubtype = (int?)TopCoatCrackSubtype.None; // No subtype
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)TopCoatCrackSubtype.None;
                                        }
                                        break;

                                    case TopCoatDefectType.Pinholes:
                                        if (!anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)TopCoatPinholesSubtype.Scorch;
                                        }

                                        if (anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            switch ((TopCoatPinholesSubtype)anomaly.Classification.DefectSubtype.Value)
                                            {
                                                case TopCoatPinholesSubtype.Scorch:
                                                    // Handle Scorch subtype
                                                    break;
                                                default:
                                                    anomaly.Classification.DefectSubtype = (int?)TopCoatPinholesSubtype.None; // No subtype
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)TopCoatPinholesSubtype.None;
                                        }
                                        break;

                                    case TopCoatDefectType.Scratch:
                                    case TopCoatDefectType.Scorch:
                                        anomaly.Classification.DefectSubtype = (int?)TopCoatCrackSubtype.None; // No subtypes for these
                                        break;

                                    default:
                                        anomaly.Classification.DefectSubtype = null; // No subtype
                                        break;
                                }
                                break;

                            case BladeMaterial.Laminate:
                                switch ((LaminateDefectType)anomaly.Classification.DefectType)
                                {
                                    case LaminateDefectType.Erosion:
                                        if (!anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)LaminateErosionSubtype.Lightning;
                                        }
                                        if (anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            switch ((LaminateErosionSubtype)anomaly.Classification.DefectSubtype.Value)
                                            {
                                                case LaminateErosionSubtype.Chip:
                                                case LaminateErosionSubtype.Lightning:
                                                    // Handle specific subtypes
                                                    break;
                                                default:
                                                    anomaly.Classification.DefectSubtype = (int?)LaminateErosionSubtype.None; // No subtype
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)LaminateErosionSubtype.None;
                                        }
                                        break;

                                    case LaminateDefectType.Delamination:
                                        if (!anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)LaminateDelaminationSubtype.Lightning;
                                        }
                                        if (anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            switch ((LaminateDelaminationSubtype)anomaly.Classification.DefectSubtype.Value)
                                            {
                                                case LaminateDelaminationSubtype.Lightning:
                                                    // Handle Lightning subtype
                                                    break;
                                                default:
                                                    anomaly.Classification.DefectSubtype = (int?)LaminateDelaminationSubtype.None; // No subtype
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)LaminateDelaminationSubtype.None;
                                        }
                                        break;

                                    case LaminateDefectType.Scratch:
                                        anomaly.Classification.DefectSubtype = (int?)LaminateErosionSubtype.None; // No subtypes for Scratch
                                        break;

                                    default:
                                        anomaly.Classification.DefectSubtype = null; // No subtype
                                        break;
                                }
                                break;

                            case BladeMaterial.Structure:
                                switch ((StructureDefectType)anomaly.Classification.DefectType)
                                {
                                    case StructureDefectType.Crack:
                                        if (!anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)StructureCrackSubtype.Transverse;
                                        }
                                        if (anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            switch ((StructureCrackSubtype)anomaly.Classification.DefectSubtype.Value)
                                            {
                                                case StructureCrackSubtype.Transverse:
                                                case StructureCrackSubtype.Longitudinal:
                                                case StructureCrackSubtype.TLCShapedCracks:
                                                case StructureCrackSubtype.Other:
                                                case StructureCrackSubtype.TrailingTransverse:
                                                case StructureCrackSubtype.Diagonal:
                                                case StructureCrackSubtype.Surface:
                                                    // Handle specific subtypes
                                                    break;
                                                default:
                                                    anomaly.Classification.DefectSubtype = null; // No subtype
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            anomaly.Classification.DefectSubtype = null;
                                        }
                                        break;

                                    case StructureDefectType.Delamination:
                                        if (!anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)StructureDelaminationSubtype.Edge;
                                        }
                                        if (anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            switch ((StructureDelaminationSubtype)anomaly.Classification.DefectSubtype.Value)
                                            {
                                                case StructureDelaminationSubtype.Edge:
                                                case StructureDelaminationSubtype.Lightning:
                                                case StructureDelaminationSubtype.NonLightning:
                                                    // Handle specific subtypes
                                                    break;
                                                default:
                                                    anomaly.Classification.DefectSubtype = null; // No subtype
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            anomaly.Classification.DefectSubtype = null;
                                        }
                                        break;

                                    case StructureDefectType.Erosion:
                                    case StructureDefectType.Hole:
                                        anomaly.Classification.DefectSubtype = (int?)StructureCrackSubtype.Other; // No specific subtypes
                                        break;

                                    default:
                                        anomaly.Classification.DefectSubtype = null; // No subtype
                                        break;
                                }
                                break;

                            case BladeMaterial.Through:
                                switch ((ThroughDefectType)anomaly.Classification.DefectType)
                                {
                                    case ThroughDefectType.Bondline:
                                        if (!anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)ThroughBondlineSubtype.Crushed;
                                        }
                                        if (anomaly.Classification.DefectSubtype.HasValue)
                                        {
                                            switch ((ThroughBondlineSubtype)anomaly.Classification.DefectSubtype.Value)
                                            {
                                                case ThroughBondlineSubtype.Crushed:
                                                case ThroughBondlineSubtype.OpenTip:
                                                    // Handle specific subtypes
                                                    break;
                                                default:
                                                    anomaly.Classification.DefectSubtype = (int?)ThroughBondlineSubtype.None; // No subtype
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            anomaly.Classification.DefectSubtype = (int?)ThroughBondlineSubtype.None;
                                        }
                                        break;

                                    case ThroughDefectType.Erosion:
                                        anomaly.Classification.DefectSubtype = (int?)ThroughBondlineSubtype.None; // No subtypes for Erosion
                                        break;

                                    default:
                                        anomaly.Classification.DefectSubtype = null; // No subtype
                                        break;
                                }
                                break;

                            default:
                                anomaly.Classification.DefectType = 0; // Default or unknown type
                                anomaly.Classification.DefectSubtype = null; // No subtype
                                break;
                        }
                    }

                    string defectId = $"120D{blade.SerialNumber}{n:D2}";
                    list.Add(new DefectEntry(anomaly, img.ImageUrl, view.Side, n++, defectId));
                }
            }
        }

        return list;
    }
}