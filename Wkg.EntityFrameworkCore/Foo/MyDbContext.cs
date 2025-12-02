using Microsoft.EntityFrameworkCore;
using Wkg.EntityFrameworkCore.Configuration;
using Wkg.EntityFrameworkCore.Extensions;

namespace Foo;

public class MyDbContext(DbContextOptions<MyDbContext> options, IModelLoader modelLoader) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.LoadModels(modelLoader);
    }
}