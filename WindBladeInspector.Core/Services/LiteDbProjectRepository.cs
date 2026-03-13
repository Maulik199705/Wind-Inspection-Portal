using LiteDB;
using WindBladeInspector.Core.Entities;
using WindBladeInspector.Core.Interfaces;

namespace WindBladeInspector.Infrastructure.Persistence;

/// <summary>
/// Persists InspectionProject data using LiteDB (embedded, file-based, no server required).
/// </summary>
public sealed class LiteDbProjectRepository : IProjectRepository, IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<InspectionProject> _projects;

    public LiteDbProjectRepository(string dbPath)
    {
        _db = new LiteDatabase(dbPath);
        _projects = _db.GetCollection<InspectionProject>("projects");
        _projects.EnsureIndex(x => x.Id, unique: true);
    }

    public InspectionProject? GetById(Guid id)
        => _projects.FindOne(p => p.Id == id);

    public List<InspectionProject> GetAll()
        => _projects.FindAll().ToList();

    public void Save(InspectionProject project)
    {
        _projects.Upsert(project); // Insert or update
    }

    public void Delete(Guid id)
        => _projects.DeleteMany(p => p.Id == id);

    public void Dispose()
        => _db.Dispose();
}