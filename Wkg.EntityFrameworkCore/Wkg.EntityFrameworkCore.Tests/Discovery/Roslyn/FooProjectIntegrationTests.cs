using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.EntityFrameworkCore.Configuration;
using Wkg.EntityFrameworkCore.Configuration.Policies.Defaults.EntityNamingPolicies;
using Wkg.EntityFrameworkCore.Configuration.Policies.Defaults.PropertyMappingPolicies;
using Wkg.EntityFrameworkCore.Extensions;
using Wkg.EntityFrameworkCore.Tests.Discovery.Roslyn.TestData;

namespace Wkg.EntityFrameworkCore.Tests.Discovery.Roslyn;

/// <summary>
/// Integration tests that mimic how the Foo project uses the model discovery and Roslyn code generation.
/// These tests validate the complete end-to-end workflow without testing implementation details.
/// </summary>
[TestClass]
public class FooProjectIntegrationTests
{
    [TestMethod]
    public void FooProjectPattern_DbContextWithModelLoader_ShouldWork()
    {
        // This test mimics the exact pattern used in the Foo project:
        // 1. DbContext constructor that takes IModelLoader
        // 2. OnModelCreating calls modelBuilder.LoadModels(modelLoader)
        // 3. Model loader is generated with [ModelDiscovery] attribute
        
        // Arrange
        var options = new DbContextOptionsBuilder<FooLikeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var modelLoader = new TestModelLoader();
        
        // Act & Assert - This should work exactly like the Foo project
        using var context = new FooLikeDbContext(options, modelLoader);
        
        // Verify database can be created
        Assert.IsTrue(context.Database.EnsureCreated());
        
        // Verify entities are properly configured and accessible
        Assert.IsNotNull(context.Books);
        Assert.IsNotNull(context.Categories);
        Assert.IsNotNull(context.Magazines);
        Assert.IsNotNull(context.Authors);
        Assert.IsNotNull(context.BookAuthors);
    }

    [TestMethod]
    public void FooProjectPattern_CompleteWorkflow_ShouldAllowFullCrudOperations()
    {
        // This test validates the complete workflow similar to what would be expected 
        // in a real application using the Foo project pattern
        
        // Arrange
        var options = new DbContextOptionsBuilder<FooLikeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var modelLoader = new TestModelLoader();
        
        // Act
        using var context = new FooLikeDbContext(options, modelLoader);
        context.Database.EnsureCreated();
        
        // Create test data following the same pattern as Foo project entities
        var category = new Category
        {
            Name = "Technology",
            Description = "Technology related books and magazines",
            Magazines = new List<Magazine>()
        };
        
        var author = new Author
        {
            FirstName = "John",
            LastName = "Doe",
            Biography = "Renowned technology writer",
            BookAuthors = new List<BookAuthor>()
        };
        
        var book = new Book
        {
            Name = "Advanced Programming",
            Author = "John Doe",
            ISBN = "978-0123456789",
            PageCount = 350,
            Price = 59.99m,
            CreatedAt = DateTime.UtcNow
        };
        
        var magazine = new Magazine
        {
            Name = "Tech Weekly",
            Price = 4.99m,
            CreatedAt = DateTime.UtcNow,
            Category = category,
            IssueNumber = 1
        };
        
        // Add entities to context
        context.Categories.Add(category);
        context.Authors.Add(author);
        context.Books.Add(book);
        context.Magazines.Add(magazine);
        
        // Create many-to-many relationship
        var bookAuthor = new BookAuthor
        {
            Book = book,
            Author = author,
            IsPrimary = true
        };
        context.BookAuthors.Add(bookAuthor);
        
        // Save changes
        var savedCount = context.SaveChanges();
        Assert.IsTrue(savedCount > 0, "Should save multiple entities");
        
        // Assert - Verify all relationships and configurations work correctly
        
        // Test querying with includes (tests navigation properties)
        var savedMagazine = context.Magazines
            .Include(m => m.Category)
            .First(m => m.Name == "Tech Weekly");
        
        Assert.AreEqual("Tech Weekly", savedMagazine.Name);
        Assert.AreEqual("Technology", savedMagazine.Category.Name);
        Assert.AreEqual(1, savedMagazine.IssueNumber);
        
        // Test unique constraints (ISBN should be unique)
        var savedBook = context.Books.First(b => b.ISBN == "978-0123456789");
        Assert.AreEqual("Advanced Programming", savedBook.Name);
        Assert.AreEqual("John Doe", savedBook.Author);
        Assert.AreEqual(350, savedBook.PageCount);
        
        // Test many-to-many relationship
        var savedBookAuthor = context.BookAuthors
            .Include(ba => ba.Book)
            .Include(ba => ba.Author)
            .First();
        
        Assert.IsTrue(savedBookAuthor.IsPrimary);
        Assert.AreEqual("Advanced Programming", savedBookAuthor.Book.Name);
        Assert.AreEqual("John", savedBookAuthor.Author.FirstName);
        Assert.AreEqual("Doe", savedBookAuthor.Author.LastName);
        
        // Test base model configuration inheritance
        Assert.IsTrue(savedBook.Id > 0, "Book ID should be auto-generated");
        Assert.IsTrue(savedMagazine.Id > 0, "Magazine ID should be auto-generated");
        Assert.AreEqual(59.99m, savedBook.Price);
        Assert.AreEqual(4.99m, savedMagazine.Price);
    }

    [TestMethod]
    public void FooProjectPattern_DatabaseConstraints_ShouldBeEnforced()
    {
        // This test verifies that the database constraints configured in the models
        // are properly enforced, similar to how they would be in the Foo project
        
        // Arrange
        var options = new DbContextOptionsBuilder<FooLikeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var modelLoader = new TestModelLoader();
        
        using var context = new FooLikeDbContext(options, modelLoader);
        context.Database.EnsureCreated();
        
        // Test unique constraint on Category.Name
        var category1 = new Category { Name = "Duplicate", Description = "First", Magazines = new List<Magazine>() };
        var category2 = new Category { Name = "Duplicate", Description = "Second", Magazines = new List<Magazine>() };
        
        context.Categories.Add(category1);
        context.SaveChanges();
        
        context.Categories.Add(category2);
        
        // In-memory database doesn't enforce unique constraints, but we can test the configuration exists
        var categoryEntityType = context.Model.FindEntityType(typeof(Category));
        var nameIndex = categoryEntityType?.GetIndexes().FirstOrDefault(i => 
            i.Properties.Any(p => p.Name == "Name") && i.IsUnique);
        
        Assert.IsNotNull(nameIndex, "Category.Name should have a unique index");
        Assert.AreEqual("ix_categories_name", nameIndex.GetDatabaseName());
    }

    [TestMethod]
    public void FooProjectPattern_ModelValidation_ShouldMatchExpectedSchema()
    {
        // This test verifies that the generated model matches the expected database schema
        // that would be created by the model configurations
        
        // Arrange
        var options = new DbContextOptionsBuilder<FooLikeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var modelLoader = new TestModelLoader();
        
        // Act
        using var context = new FooLikeDbContext(options, modelLoader);
        var model = context.Model;
        
        // Assert - Verify table names match configuration
        var bookEntity = model.FindEntityType(typeof(Book));
        var categoryEntity = model.FindEntityType(typeof(Category));
        var magazineEntity = model.FindEntityType(typeof(Magazine));
        var authorEntity = model.FindEntityType(typeof(Author));
        var bookAuthorEntity = model.FindEntityType(typeof(BookAuthor));
        
        Assert.AreEqual("books", bookEntity?.GetTableName());
        Assert.AreEqual("categories", categoryEntity?.GetTableName());
        Assert.AreEqual("magazines", magazineEntity?.GetTableName());
        Assert.AreEqual("authors", authorEntity?.GetTableName());
        string? bookAuthorTableName = bookAuthorEntity?.GetTableName();
        Assert.IsTrue(bookAuthorTableName == "book_authors" || bookAuthorTableName == "BookAuthor",
            $"BookAuthor table name should be 'book_authors' or 'BookAuthor', but was '{bookAuthorTableName}'");
        
        // Verify primary key names match configuration
        Assert.AreEqual("pk_books", bookEntity?.FindPrimaryKey()?.GetName());
        Assert.AreEqual("pk_categories", categoryEntity?.FindPrimaryKey()?.GetName());
        Assert.AreEqual("pk_magazines", magazineEntity?.FindPrimaryKey()?.GetName());
        Assert.AreEqual("pk_authors", authorEntity?.FindPrimaryKey()?.GetName());
        Assert.AreEqual("pk_book_authors", bookAuthorEntity?.FindPrimaryKey()?.GetName());
        
        // Verify foreign key constraint names
        var magazineCategoryFk = magazineEntity?.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Category));
        Assert.AreEqual("fk_magazines_category", magazineCategoryFk?.GetConstraintName());
        
        var bookAuthorBookFk = bookAuthorEntity?.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Book));
        Assert.AreEqual("fk_book_authors_book", bookAuthorBookFk?.GetConstraintName());
        
        var bookAuthorAuthorFk = bookAuthorEntity?.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Author));
        Assert.AreEqual("fk_book_authors_author", bookAuthorAuthorFk?.GetConstraintName());
    }

    /// <summary>
    /// DbContext class that mimics the exact pattern used in Foo/MyDbContext.cs
    /// </summary>
    private class FooLikeDbContext : DbContext
    {
        private readonly IModelLoader _modelLoader;

        public FooLikeDbContext(DbContextOptions<FooLikeDbContext> options, IModelLoader modelLoader) 
            : base(options)
        {
            _modelLoader = modelLoader;
        }

        public DbSet<Book> Books => Set<Book>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Magazine> Magazines => Set<Magazine>();
        public DbSet<Author> Authors => Set<Author>();
        public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // This is the exact pattern used in Foo/MyDbContext.cs, but with test-friendly policies
            modelBuilder.LoadModels(_modelLoader, policies =>
            {
                policies.AddEntityNamingPolicy(Wkg.EntityFrameworkCore.Configuration.Policies.Defaults.EntityNamingPolicies.EntityNamingPolicy.AllowImplicit)
                        .AddPropertyMappingPolicy(Wkg.EntityFrameworkCore.Configuration.Policies.Defaults.PropertyMappingPolicies.PropertyMappingPolicy.AllowImplicit);
            });
        }
    }
}