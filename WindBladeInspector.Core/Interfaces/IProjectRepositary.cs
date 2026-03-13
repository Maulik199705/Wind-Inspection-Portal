using WindBladeInspector.Core.Entities;

namespace WindBladeInspector.Core.Interfaces;

public interface IProjectRepository
{
    InspectionProject? GetById(Guid id);
    List<InspectionProject> GetAll();
    void Save(InspectionProject project);
    void Delete(Guid id);
}