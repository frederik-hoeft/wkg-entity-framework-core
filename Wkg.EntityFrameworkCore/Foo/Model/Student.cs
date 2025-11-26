using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration;

namespace Foo.Model;

internal sealed class Class : IDiscoverableModelConfiguration<Class>
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required ICollection<Student> Students { get; set; }

    public static void Configure(EntityTypeBuilder<Class> self)
    {
        self.ToTable("classes")
            .HasKey(c => c.Id).HasName("key_id");

        self.Property(c => c.Id)
            .HasColumnType("int")
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        self.Property(c => c.Name)
            .HasColumnType("nvarchar(100)")
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();
    }
}

internal sealed class Enrollment : IDiscoverableModelConnection<Enrollment, Student, Class>
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public required Student Student { get; set; }

    public required Class Class { get; set; }

    public static void ConfigureConnection(EntityTypeBuilder<Enrollment> self)
    {
        self.ToTable("enrollments")
            .HasKey(e => e.Id).HasName("key_id");

        self.Property(e => e.Id)
            .HasColumnType("int")
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        self.Property(e => e.StudentId)
            .HasColumnType("int")
            .HasColumnName("student_id")
            .IsRequired();

        self.Property(e => e.ClassId)
            .HasColumnType("int")
            .HasColumnName("class_id")
            .IsRequired();
    }

    public static void Connect(EntityTypeBuilder<Student> left, EntityTypeBuilder<Class> right) => left
        .HasMany(s => s.Classes)
        .WithMany(c => c.Students)
        .UsingEntity<Enrollment>(
            l => l.HasOne(e => e.Class)
                .WithMany()
                .HasForeignKey(e => e.ClassId),
            r => r.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId),
            ConfigureConnection);
}

internal sealed class Student : Person, IDiscoverableModelConfiguration<Student>
{
    public required ICollection<Class> Classes { get; set; }

    public static void Configure(EntityTypeBuilder<Student> self)
    {
        self.ToTable("my_entities")
            .HasKey(my => my.Id).HasName("key_id");
    }
}