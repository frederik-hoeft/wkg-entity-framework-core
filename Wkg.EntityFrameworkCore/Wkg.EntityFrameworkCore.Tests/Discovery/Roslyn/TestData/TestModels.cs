using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration;

namespace Wkg.EntityFrameworkCore.Tests.Discovery.Roslyn.TestData;

// Base model for inheritance testing
internal abstract class BaseProduct : IDiscoverableBaseModelConfiguration<BaseProduct>
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }

    static void IBaseModelConfiguration<BaseProduct>.ConfigureBaseModel<TChildClass>(EntityTypeBuilder<TChildClass> self)
    {
        self.Property(p => p.Id)
            .HasColumnType("INTEGER")
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        self.Property(p => p.Name)
            .HasColumnType("VARCHAR")
            .HasMaxLength(100)
            .HasColumnName("name")
            .IsRequired();

        self.Property(p => p.Price)
            .HasColumnType("DECIMAL")
            .HasPrecision(18, 2)
            .HasColumnName("price")
            .IsRequired();

        self.Property(p => p.CreatedAt)
            .HasColumnType("DATETIME")
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}

// Simple entity configuration test
internal sealed class Book : BaseProduct, IDiscoverableModelConfiguration<Book>
{
    public required string Author { get; set; }
    public required string ISBN { get; set; }
    public int PageCount { get; set; }

    public static void Configure(EntityTypeBuilder<Book> self)
    {
        self.ToTable("books")
            .HasKey(b => b.Id).HasName("pk_books");

        self.Property(b => b.Author)
            .HasColumnType("VARCHAR")
            .HasMaxLength(200)
            .HasColumnName("author")
            .IsRequired();

        self.Property(b => b.ISBN)
            .HasColumnType("VARCHAR")
            .HasMaxLength(20)
            .HasColumnName("isbn")
            .IsRequired();

        self.HasIndex(b => b.ISBN)
            .IsUnique()
            .HasDatabaseName("ix_books_isbn");

        self.Property(b => b.PageCount)
            .HasColumnType("INTEGER")
            .HasColumnName("page_count")
            .IsRequired();
    }
}

// Another simple entity for relationship testing
internal sealed class Category : IDiscoverableModelConfiguration<Category>
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required ICollection<Magazine> Magazines { get; set; }

    public static void Configure(EntityTypeBuilder<Category> self)
    {
        self.ToTable("categories")
            .HasKey(c => c.Id).HasName("pk_categories");

        self.Property(c => c.Id)
            .HasColumnType("INTEGER")
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        self.Property(c => c.Name)
            .HasColumnType("VARCHAR")
            .HasMaxLength(50)
            .HasColumnName("name")
            .IsRequired();

        self.Property(c => c.Description)
            .HasColumnType("TEXT")
            .HasColumnName("description");

        self.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("ix_categories_name");
    }
}

// Entity with foreign key relationship
internal sealed class Magazine : BaseProduct, IDiscoverableModelConfiguration<Magazine>
{
    public int CategoryId { get; set; }
    public required Category Category { get; set; }
    public int IssueNumber { get; set; }

    public static void Configure(EntityTypeBuilder<Magazine> self)
    {
        self.ToTable("magazines")
            .HasKey(m => m.Id).HasName("pk_magazines");

        self.Property(m => m.CategoryId)
            .HasColumnType("INTEGER")
            .HasColumnName("category_id")
            .IsRequired();

        self.Property(m => m.IssueNumber)
            .HasColumnType("INTEGER")
            .HasColumnName("issue_number")
            .IsRequired();

        self.HasOne(m => m.Category)
            .WithMany(c => c.Magazines)
            .HasForeignKey(m => m.CategoryId)
            .HasConstraintName("fk_magazines_category")
            .OnDelete(DeleteBehavior.Cascade);

        self.HasIndex(m => new { m.CategoryId, m.IssueNumber })
            .IsUnique()
            .HasDatabaseName("ix_magazines_category_issue");
    }
}

// Many-to-many relationship entities
internal sealed class Author : IDiscoverableModelConfiguration<Author>
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Biography { get; set; }
    public required ICollection<BookAuthor> BookAuthors { get; set; }

    public static void Configure(EntityTypeBuilder<Author> self)
    {
        self.ToTable("authors")
            .HasKey(a => a.Id).HasName("pk_authors");

        self.Property(a => a.Id)
            .HasColumnType("INTEGER")
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        self.Property(a => a.FirstName)
            .HasColumnType("VARCHAR")
            .HasMaxLength(100)
            .HasColumnName("first_name")
            .IsRequired();

        self.Property(a => a.LastName)
            .HasColumnType("VARCHAR")
            .HasMaxLength(100)
            .HasColumnName("last_name")
            .IsRequired();

        self.Property(a => a.Biography)
            .HasColumnType("TEXT")
            .HasColumnName("biography");
    }
}

// Junction table for many-to-many relationship using IDiscoverableModelConnection
internal sealed class BookAuthor : IDiscoverableModelConnection<BookAuthor, Book, Author>
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int AuthorId { get; set; }
    public required Book Book { get; set; }
    public required Author Author { get; set; }
    public bool IsPrimary { get; set; }

    public static void ConfigureConnection(EntityTypeBuilder<BookAuthor> self)
    {
        self.ToTable("book_authors")
            .HasKey(ba => ba.Id).HasName("pk_book_authors");

        self.Property(ba => ba.Id)
            .HasColumnType("INTEGER")
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        self.Property(ba => ba.BookId)
            .HasColumnType("INTEGER")
            .HasColumnName("book_id")
            .IsRequired();

        self.Property(ba => ba.AuthorId)
            .HasColumnType("INTEGER")
            .HasColumnName("author_id")
            .IsRequired();

        self.Property(ba => ba.IsPrimary)
            .HasColumnType("BIT")
            .HasColumnName("is_primary")
            .IsRequired()
            .HasDefaultValue(false);

        self.HasIndex(ba => new { ba.BookId, ba.AuthorId })
            .IsUnique()
            .HasDatabaseName("ix_book_authors_book_author");
    }

    public static void Connect(EntityTypeBuilder<Book> left, EntityTypeBuilder<Author> right)
    {
        // Configure the many-to-many relationship through the junction table
        left.HasMany<BookAuthor>()
            .WithOne(ba => ba.Book)
            .HasForeignKey(ba => ba.BookId)
            .HasConstraintName("fk_book_authors_book")
            .OnDelete(DeleteBehavior.Cascade);

        right.HasMany(a => a.BookAuthors)
            .WithOne(ba => ba.Author)
            .HasForeignKey(ba => ba.AuthorId)
            .HasConstraintName("fk_book_authors_author")
            .OnDelete(DeleteBehavior.Cascade);
    }
}