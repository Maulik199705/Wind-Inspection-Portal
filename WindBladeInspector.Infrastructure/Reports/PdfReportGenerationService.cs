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

    /// <summary>Path to App_Data/blade-images — where uploaded blade photos are stored.</summary>
    private readonly string _bladeImagesDir;

    /// <summary>Path to wwwroot — where Cover.jpeg, Logo.png and other static assets live.</summary>
    private readonly string _wwwRootPath;

    private readonly ILogger<PdfReportGenerationService> _logger;

    private byte[]? _logoBytes;
    private byte[]? _coverImageBytes;

    internal const string Black = "#000000";
    internal const string DarkGray = "#333333";
    internal const string LightGray = "#F0F0F0";
    internal const string White = "#FFFFFF";

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
                page.Header().Element(c => PageHeader(c, "01 CONTENTS"));
                page.Content().Section("ContentsPage")
                              .Element(c => new ContentsPageComponent(project, pageNumbers).Compose(c));
            });

            // 3. Introduction - Details
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.Header().Element(c => PageHeader(c, "02 INTRODUCTION"));
                page.Content().Section("IntroSection")
                              .Element(c => new InspectionDetailsComponent(project).Compose(c));
            });

            // 4. General Overview
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.Header().Element(c => PageHeader(c, "03 INTRODUCTION\nGeneral Turbine Overview"));
                page.Content().Section("GeneralOverview")
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
                    page.Header().Element(c => PageHeader(c, $"{headerNum} BLADE {blade.SerialNumber}"));
                    page.Content().Section($"BladeOverview_{blade.SerialNumber}")
                                  .Element(c => new BladeOverviewComponent(blade, bladeDefects).Compose(c));
                });

                bool isFirstDefectPage = true;
                foreach (var defect in bladeDefects)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(15);
                        string headerNum = (globalPageNum++).ToString("D2");
                        page.Header().Element(c => PageHeader(c, $"{headerNum} BLADE {blade.SerialNumber}"));

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
                page.Margin(15);
                page.Content().AlignCenter().AlignMiddle().Column(c =>
                {
                    c.Item().Text("KEEP YOUR BLADES SAFER WITH...").FontSize(16).Bold();
                    c.Item().PaddingTop(20).Text("QUALIMAX\nSERVICES").FontSize(24).Bold().FontColor(DarkGray);
                    c.Item().PaddingTop(20).Text("www.qmservice.in").FontSize(12).FontColor(DarkGray);
                });
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
        container.PaddingBottom(20).Row(row =>
        {
            row.RelativeItem().Text(title).FontSize(14).Bold().FontColor(Black);
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
    /// Loads static brand assets (logo, cover image) from wwwroot/Images.
    /// </summary>
    private void LoadAssets()
    {
        if (_logoBytes != null) return;

        // Static assets live in wwwroot/Images — NOT in blade-images
        var logoPath = Path.Combine(_wwwRootPath, "Images", "Logo.png");
        var coverPath = Path.Combine(_wwwRootPath, "Images", "Cover.jpeg");

        _logoBytes = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : Array.Empty<byte>();
        _coverImageBytes = File.Exists(coverPath) ? File.ReadAllBytes(coverPath) : null;

        _logger.LogInformation("Assets loaded — Logo: {Logo}, Cover: {Cover}",
            _logoBytes.Length > 0 ? "OK" : "MISSING",
            _coverImageBytes != null ? "OK" : $"MISSING at {coverPath}");
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