using WindBladeInspector.Core.Entities;
using WindBladeInspector.Core.Interfaces;

namespace WindBladeInspector.Core.Services;

/// <summary>
/// Manages inspection projects, backed by a persistent repository.
/// </summary>
public class DashboardService
{
    private readonly IProjectRepository _repository;

    public DashboardService(IProjectRepository repository)
    {
        _repository = repository;
    }

    public InspectionProject CreateProject(string parkName, string turbineId, string model, string client, string location)
    {
        var project = new InspectionProject
        {
            Id = Guid.NewGuid(),
            ParkName = parkName,
            TurbineId = turbineId,
            Model = model,
            Client = client,
            Location = location,
            DataCaptureStatus = "In Progress",
            AnalysisStatus = "In Progress",
            InspectionDate = DateTime.Now,
            Blades = new List<Blade>
            {
                new Blade { SerialNumber = "A", Length = 0 },
                new Blade { SerialNumber = "B", Length = 0 },
                new Blade { SerialNumber = "C", Length = 0 }
            }
        };

        _repository.Save(project);
        return project;
    }

    public InspectionProject? GetProjectById(Guid id)
        => _repository.GetById(id);

    public List<InspectionProject> GetAllProjects()
        => _repository.GetAll();

    /// <summary>Persist any changes made to a project.</summary>
    public void SaveProject(InspectionProject project)
        => _repository.Save(project);

    /// <summary>Marks a project as Complete and persists it.</summary>
    public void MarkComplete(Guid id)
    {
        var project = _repository.GetById(id);
        if (project == null) return;

        project.DataCaptureStatus = "Complete";
        project.AnalysisStatus = "Complete";
        _repository.Save(project);
    }

    /// <summary>Permanently deletes a project by ID.</summary>
    public void DeleteProject(Guid id)
        => _repository.Delete(id);
}