using Microsoft.EntityFrameworkCore;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Generic implementation of the <see cref="IRepository{T}"/> pattern utilizing Entity Framework Core.
    /// Provides generic CRUD behaviors where custom specialized repositories are not required.
    /// </summary>
    /// <typeparam name="T">The persistence db model entity type.</typeparam>
    public class EfRepository<T> : IRepository<T> where T : class
    {
        /// <summary>
        /// Gets the active Database Context instance.
        /// </summary>
        protected readonly NexusDbContext Context;

        /// <summary>
        /// Gets the DbSet for type T.
        /// </summary>
        protected readonly DbSet<T> DbSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="EfRepository{T}"/> class.
        /// </summary>
        /// <param name="context">The initialized database context.</param>
        public EfRepository(NexusDbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            DbSet = context.Set<T>();
        }

        /// <inheritdoc />
        public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (Guid.TryParse(id, out var guidId))
            {
                return await DbSet.FindAsync(new object[] { guidId }, cancellationToken);
            }
            return await DbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await DbSet.AddAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            DbSet.Update(entity);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            DbSet.Remove(entity);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet.ToListAsync(cancellationToken);
        }
    }
}
