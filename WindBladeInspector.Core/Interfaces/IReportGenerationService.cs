using WindBladeInspector.Core.Entities;
using WindBladeInspector.Core.Models;

namespace WindBladeInspector.Core.Interfaces;

/// <summary>
/// Defines the contract for generating PDF inspection reports from project data.
/// </summary>
public interface IReportGenerationService
{
    /// <summary>
    /// Generates a full PDF inspection report for the given project.
    /// </summary>
    Task<byte[]> GenerateProjectReportAsync(
        InspectionProject project,
        ReportOptions? options = null);
}

