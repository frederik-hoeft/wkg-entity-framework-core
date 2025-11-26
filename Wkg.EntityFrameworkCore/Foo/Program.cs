

// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration;
using Wkg.EntityFrameworkCore.Configuration.Discovery;
using Wkg.EntityFrameworkCore.Configuration.SourceGen;

Console.WriteLine("Hello, World!");

internal class MyDbContext(IModelConfigurationProvider provider) : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        EntityDiscoveryContext context = new([]);
        provider.LoadModels(modelBuilder, context);
    }
}

internal sealed class MyModelConfigurationProvider : ModelConfigurationProvider
{
    public override void LoadModels(ModelBuilder builder, IEntityDiscoveryContext context)
    {
        GenericDispatchHelper.Configure<MyEntityA>(builder)
            .ConfigureBase<MyEntityA, MyEntityBase>()
            .Register(context);
    }
}

internal abstract class MyEntityBase : IReflectiveBaseModelConfiguration<MyEntityBase>
{
    public int Id { get; set; }

    static void IBaseModelConfiguration<MyEntityBase>.ConfigureBaseModel<TChildClass>(EntityTypeBuilder<TChildClass> self)
    {
        self.Property(my => my.Id)
            .HasColumnType("INTEGER")
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();
    }
}

internal sealed class MyEntityA : MyEntityBase, IReflectiveModelConfiguration<MyEntityA>
{
    public required string Name { get; set; }

    public static void Configure(EntityTypeBuilder<MyEntityA> self)
    {
        self.ToTable("my_entities")
            .HasKey(my => my.Id).HasName("key_id");

        self.Property(my => my.Name)
            .HasColumnType("VARCHAR")
            .HasMaxLength(64)
            .HasColumnName("name")
            .IsRequired();
    }
}