using Microsoft.EntityFrameworkCore.Design;

namespace PatchPanda.Web.Db;

internal sealed class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseSqlite("Data Source=:memory:");

        return new DataContext(optionsBuilder.Options);
    }
}