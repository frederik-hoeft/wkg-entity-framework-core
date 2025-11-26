using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Wkg.EntityFrameworkCore.Configuration;
using Wkg.EntityFrameworkCore.Extensions;

namespace Foo;

public class MyDbContext(DbContextOptions<MyDbContext> options, IModelLoader modelLoader) : IdentityDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.LoadModels(modelLoader);
    }
}