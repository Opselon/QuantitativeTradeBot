using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Application.Ports
{
    /// <summary>
    /// Port interface representing a generic repository structure for persistence management.
    /// Used only where generic capabilities are suitable without unnecessary abstraction complexity.
    /// </summary>
    /// <typeparam name="T">The type of the persistence entity.</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Retrieves an entity by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier (Guid or string representation).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The found entity, or null if it does not exist.</returns>
        Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new entity to the persistence store.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task AddAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing entity in the persistence store.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity from the persistence store.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all entities of type T.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of all entities.</returns>
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
