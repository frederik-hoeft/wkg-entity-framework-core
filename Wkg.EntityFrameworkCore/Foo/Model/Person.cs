using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration;

namespace Foo.Model;

internal abstract class Person : IDiscoverableBaseModelConfiguration<Person>
{
    public int Id { get; set; }

    public required string Name { get; set; }

    static void IBaseModelConfiguration<Person>.ConfigureBaseModel<TChildClass>(EntityTypeBuilder<TChildClass> self)
    {
        self.Property(my => my.Id)
            .HasColumnType("INTEGER")
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        self.Property(my => my.Name)
            .HasColumnType("VARCHAR")
            .HasMaxLength(64)
            .HasColumnName("name")
            .IsRequired();
    }
}
