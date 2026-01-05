using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AspireDashboardBI.Web
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Order> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("sample");
            
            // Convert all table and column names to lowercase
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                if (entity.IsMappedToJson())
                {
                    continue;
                }

                // Set table name to lowercase
                entity.SetTableName(entity.GetTableName()?.ToLowerInvariant());

                // Set column names to lowercase
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(property.GetColumnName()?.ToLowerInvariant());
                }

                // Set key names to lowercase
                foreach (var key in entity.GetKeys())
                {
                    key.SetName(key.GetName()?.ToLowerInvariant());
                }

                // Set foreign key names to lowercase
                foreach (var foreignKey in entity.GetForeignKeys())
                {
                    foreignKey.SetConstraintName(foreignKey.GetConstraintName()?.ToLowerInvariant());
                }

                // Set index names to lowercase
                foreach (var index in entity.GetIndexes())
                {
                    index.SetDatabaseName(index.GetDatabaseName()?.ToLowerInvariant());
                }
            }
        }
    }
}