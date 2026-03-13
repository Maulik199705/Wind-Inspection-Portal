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

        var allDefects = project.Blades.ToDictionary(b => b.SerialNumber, b => CollectDefects(b));

        // Calculate page numbers before generating the document
        var pageNumbers = CalculatePageNumbers(project, allDefects);

        byte[] pdfBytes = Document.Create(container =>
        {
            // 1. Cover Page
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.Content().Element(c => new CoverPageComponent(project, _logoBytes).Compose(c));
            });

            // 2. Contents Page (TAGGED) - Pass pageNumbers
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.Header().Element(c => PageHeader(c, "01 CONTENTS"));
                page.Content().Section("ContentsPage")
                              .Element(c => new ContentsPageComponent(project, pageNumbers).Compose(c));
            });

            // 3. Introduction - Details (TAGGED)
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.Header().Element(c => PageHeader(c, "02 INTRODUCTION"));
                page.Content().Section("IntroSection")
                              .Element(c => new InspectionDetailsComponent(project).Compose(c));
            });

            // 4. Introduction - General Overview (TAGGED)
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

                // Blade Overview (TAGGED)
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15);
                    string headerNum = (globalPageNum++).ToString("D2");
                    page.Header().Element(c => PageHeader(c, $"{headerNum} BLADE {blade.SerialNumber}"));

                    // Tag this section for TOC navigation
                    page.Content().Section($"BladeOverview_{blade.SerialNumber}")
                                  .Element(c => new BladeOverviewComponent(blade, bladeDefects).Compose(c));
                });

                // Blade Defects (TAGGED FIRST PAGE ONLY)
                bool isFirstDefectPage = true;
                foreach (var defect in bladeDefects)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(15);
                        string headerNum = (globalPageNum++).ToString("D2");
                        page.Header().Element(c => PageHeader(c, $"{headerNum} BLADE {blade.SerialNumber}"));

                        // FIX: Use Column to wrap both Section and Component
                        page.Content().Column(col =>
                        {
                            // Only tag the first defect page for this blade
                            if (isFirstDefectPage)
                            {
                                col.Item().Section($"BladeDetails_{blade.SerialNumber}");
                                isFirstDefectPage = false;
                            }

                            col.Item().Element(c =>
                            {
                                var imgBytes = LoadImageBytes(defect.ImageUrl);
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

    /// <summary>
    /// Calculates page numbers for all major sections in the report.
    /// Page 1: Cover, Page 2: TOC, Page 3: Intro, Page 4: General Overview, then blades...
    /// </summary>
    private Dictionary<string, int> CalculatePageNumbers(
        InspectionProject project,
        Dictionary<string, List<DefectEntry>> allDefects)
    {
        var pageMap = new Dictionary<string, int>();
        int currentPage = 1;

        // Page 1: Cover
        currentPage++;

        // Page 2: Contents
        pageMap["Contents"] = currentPage;
        currentPage++;

        // Page 3: Introduction
        pageMap["IntroSection"] = currentPage;
        currentPage++;

        // Page 4: General Overview
        pageMap["GeneralOverview"] = currentPage;
        currentPage++;

        // Blade pages
        foreach (var blade in project.Blades)
        {
            var bladeDefects = allDefects[blade.SerialNumber];

            // Blade Overview page
            pageMap[$"BladeOverview_{blade.SerialNumber}"] = currentPage;
            currentPage++;

            // Blade Details (first defect page)
            if (bladeDefects.Count > 0)
            {
                pageMap[$"BladeDetails_{blade.SerialNumber}"] = currentPage;
                currentPage += bladeDefects.Count; // Each defect gets one page
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