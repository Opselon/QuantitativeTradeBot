using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Port interface defining the boundary for non-blocking data and experience writes.
    /// </summary>
    public interface IExperienceDatabaseWriter
    {
        bool Enqueue(ExperienceRecord record);
    }
}