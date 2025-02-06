using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace NTech.Core.Module.Database
{
    public class LegacyEntityFrameworkHelper
    {
        /// <summary>
        /// Call this in OnModelCreating of the DbContext after setting up all the all properties.
        /// The after bit is really important.
        /// 
        /// Use this when migrating a context this way:
        /// 
        /// - Create the new context and an integration test in NTech.Core.Host.IntegrationTests that creates a database from the core context
        /// - Use Tools -> Sql server -> Schema comparison to compare the database created by the core context with one created by the legacy context. (SSDT must be installed. Its in the standard installer)
        /// - Makes changes until they are the same. Be careful when adding code to this method so it doesnt alter what previous databases do in dangerous ways.
        /// 
        /// </summary>
        /// <param name="modelBuilder"></param>
        public void RestoreLegacyNamingConventions(ModelBuilder modelBuilder)
        {
            string Unpluralize(string name) =>
                name.EndsWith("s") ? name.Substring(0, name.Length - 1) : name;

            //Remove name pluralization to mimic how the production db actually looks
            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.FindDiscriminatorProperty() != null)
                {
                    entityType.SetTableName(Unpluralize(entityType.GetTableName()));
                }
                else
                {
                    entityType.SetTableName(entityType.ClrType.Name);
                }

                var pk = entityType.FindPrimaryKey();
                if (pk != null)
                {
                    pk.SetName($"PK_dbo.{entityType.GetTableName()}");
                }

                foreach (var fk in entityType.GetForeignKeys())
                {
                    if (fk.Properties.Count == 1)
                    {
                        string fkTableName = Unpluralize(fk.DeclaringEntityType.GetTableName());
                        string pkTableName = Unpluralize(fk.PrincipalEntityType.GetTableName());
                        string fkColumnName = fk.Properties.Single().Name;
                        var constraintName = $"FK_dbo.{fkTableName}_dbo.{pkTableName}_{fkColumnName}";
                        if (constraintName.Length <= 128)
                        {
                            fk.SetConstraintName($"FK_dbo.{fkTableName}_dbo.{pkTableName}_{fkColumnName}");
                        }
                        //TODO: Deal with > 128
                    }
                }

                var schema = entityType.GetSchema();
                var tableName = entityType.GetTableName();
                var storeObjectIdentifier = StoreObjectIdentifier.Table(tableName, schema);
                foreach (var index in entityType.GetIndexes())
                {
                    if (index.DeclaringEntityType.IsAbstract())
                        continue; //This will be things like tables with discriminators. Like CreditDecision in PreCredit. These are a terrible idea and we should not add more.

                    if (index.Properties.Count == 1 && index.GetDatabaseName() == index.GetDefaultDatabaseName())
                    {
                        var columnName = index.Properties.Single().GetColumnName(storeObjectIdentifier);
                        if (string.IsNullOrWhiteSpace(columnName))
                            throw new Exception("Index edge case detected. Needs investigation. Entity: " + entityType.Name);
                        index.SetDatabaseName($"IX_{columnName}");
                    }
                }
            }
        }
    }
}
