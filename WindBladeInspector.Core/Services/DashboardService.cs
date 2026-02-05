using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Core.Services;

/// <summary>
/// Manages inspection projects in memory. 
/// In a real production app, this would connect to a database.
/// </summary>
public class DashboardService
{
    // In-memory storage for the session
    private readonly List<InspectionProject> _projects = new();

    /// <summary>
    /// Creates a new empty project with 3 default blades.
    /// </summary>
    public InspectionProject CreateProject(string parkName, string turbineId, string model)
    {
        var project = new InspectionProject
        {
            Id = Guid.NewGuid(),
            ParkName = parkName,
            TurbineId = turbineId,
            Model = model,
            DataCaptureStatus = "Not Started",
            AnalysisStatus = "New",
            InspectionDate = DateTime.Now,
            Blades = new List<Blade>
            {
                new Blade { SerialNumber = "A", Length = 0 }, // Length to be filled during inspection
                new Blade { SerialNumber = "B", Length = 0 },
                new Blade { SerialNumber = "C", Length = 0 }
            }
        };

        _projects.Add(project);
        return project;
    }

    public InspectionProject? GetProjectById(Guid id)
    {
        return _projects.FirstOrDefault(p => p.Id == id);
    }

    public List<InspectionProject> GetAllProjects()
    {
        return _projects;
    }
}