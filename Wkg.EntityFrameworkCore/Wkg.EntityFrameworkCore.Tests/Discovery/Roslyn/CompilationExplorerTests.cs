using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Wkg.EntityFrameworkCore.Configuration.Policies.Defaults.EntityNamingPolicies;
using Wkg.EntityFrameworkCore.Configuration.Policies.Defaults.PropertyMappingPolicies;
using Wkg.EntityFrameworkCore.Discovery.Roslyn;
using Wkg.EntityFrameworkCore.Extensions;
using Wkg.EntityFrameworkCore.Tests.Discovery.Roslyn.TestData;

namespace Wkg.EntityFrameworkCore.Tests.Discovery.Roslyn;

[TestClass]
public class CompilationExplorerTests
{
    [TestMethod]
    public void TestModelLoader_ShouldHaveModelDiscoveryAttribute()
    {
        // Arrange
        Type loaderType = typeof(TestModelLoader);
        
        // Act
        ModelDiscoveryAttribute? attribute = loaderType.GetCustomAttribute<ModelDiscoveryAttribute>();
        
        // Assert
        Assert.IsNotNull(attribute, "TestModelLoader should have ModelDiscoveryAttribute");
    }

    [TestMethod]
    public void TestModelLoader_ShouldBePartialClass()
    {
        // Arrange
        Type loaderType = typeof(TestModelLoader);
        
        // Act & Assert
        // In .NET, we can't directly check if a class is partial at runtime,
        // but we can verify that the generated portion exists by checking if it implements IModelLoader
        Assert.IsTrue(typeof(Wkg.EntityFrameworkCore.Configuration.IModelLoader).IsAssignableFrom(loaderType),
            "TestModelLoader should implement IModelLoader (indicating successful code generation)");
    }

    [TestMethod] 
    public void DiscoverableEntities_ShouldHaveCorrectInterfaces()
    {
        // Test that our test models implement the expected interfaces
        
        // Test IDiscoverableModelConfiguration implementations
        Assert.IsTrue(typeof(Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelConfiguration<Book>).IsAssignableFrom(typeof(Book)),
            "Book should implement IDiscoverableModelConfiguration<Book>");
            
        Assert.IsTrue(typeof(Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelConfiguration<Category>).IsAssignableFrom(typeof(Category)),
            "Category should implement IDiscoverableModelConfiguration<Category>");
            
        Assert.IsTrue(typeof(Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelConfiguration<Magazine>).IsAssignableFrom(typeof(Magazine)),
            "Magazine should implement IDiscoverableModelConfiguration<Magazine>");
            
        Assert.IsTrue(typeof(Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelConfiguration<Author>).IsAssignableFrom(typeof(Author)),
            "Author should implement IDiscoverableModelConfiguration<Author>");

        // Test IDiscoverableBaseModelConfiguration implementation
        Assert.IsTrue(typeof(Wkg.EntityFrameworkCore.Configuration.IDiscoverableBaseModelConfiguration<BaseProduct>).IsAssignableFrom(typeof(BaseProduct)),
            "BaseProduct should implement IDiscoverableBaseModelConfiguration<BaseProduct>");

        // Test IDiscoverableModelConnection implementation
        Assert.IsTrue(typeof(Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelConnection<BookAuthor, Book, Author>).IsAssignableFrom(typeof(BookAuthor)),
            "BookAuthor should implement IDiscoverableModelConnection<BookAuthor, Book, Author>");
    }

    [TestMethod]
    public void DiscoverableEntities_ShouldHaveRequiredStaticMethods()
    {
        // Test Configure methods exist
        MethodInfo? bookConfigureMethod = typeof(Book).GetMethod("Configure", 
            BindingFlags.Public | BindingFlags.Static,
            new[] { typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Book>) });
        Assert.IsNotNull(bookConfigureMethod, "Book should have static Configure method");

        MethodInfo? categoryConfigureMethod = typeof(Category).GetMethod("Configure", 
            BindingFlags.Public | BindingFlags.Static,
            new[] { typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Category>) });
        Assert.IsNotNull(categoryConfigureMethod, "Category should have static Configure method");

        MethodInfo? magazineConfigureMethod = typeof(Magazine).GetMethod("Configure", 
            BindingFlags.Public | BindingFlags.Static,
            new[] { typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Magazine>) });
        Assert.IsNotNull(magazineConfigureMethod, "Magazine should have static Configure method");

        MethodInfo? authorConfigureMethod = typeof(Author).GetMethod("Configure", 
            BindingFlags.Public | BindingFlags.Static,
            new[] { typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Author>) });
        Assert.IsNotNull(authorConfigureMethod, "Author should have static Configure method");

        // Test connection methods exist
        MethodInfo? bookAuthorConfigureConnectionMethod = typeof(BookAuthor).GetMethod("ConfigureConnection", 
            BindingFlags.Public | BindingFlags.Static,
            new[] { typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<BookAuthor>) });
        Assert.IsNotNull(bookAuthorConfigureConnectionMethod, "BookAuthor should have static ConfigureConnection method");

        MethodInfo? bookAuthorConnectMethod = typeof(BookAuthor).GetMethod("Connect", 
            BindingFlags.Public | BindingFlags.Static,
            new[] { 
                typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Book>),
                typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Author>)
            });
        Assert.IsNotNull(bookAuthorConnectMethod, "BookAuthor should have static Connect method");
    }

    [TestMethod]
    public void GeneratedCode_ShouldCompileWithoutErrors()
    {
        // Arrange & Act
        // The fact that we can create an instance of TestModelLoader means the generated code compiled successfully
        TestModelLoader loader = new();
        
        // Assert
        Assert.IsNotNull(loader, "TestModelLoader should be instantiable");
        Assert.IsInstanceOfType<Wkg.EntityFrameworkCore.Configuration.IModelLoader>(loader);
    }

    [TestMethod]
    public void GeneratedCode_ShouldWorkWithEntityFrameworkCore()
    {
        // Arrange
        DbContextOptions<MinimalTestDbContext> options = new DbContextOptionsBuilder<MinimalTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act & Assert - This tests that the generated code integrates properly with EF Core
        using MinimalTestDbContext context = new(options);
        
        // The fact that we can create the context and access the model means everything compiled and works
        Microsoft.EntityFrameworkCore.Metadata.IModel model = context.Model;
        Assert.IsNotNull(model);
        
        // Verify we can create the database schema
        Assert.IsTrue(context.Database.EnsureCreated(), "Database schema should be created successfully");
    }

    [TestMethod]
    public void TestModels_InheritanceHierarchy_ShouldBeStructuredCorrectly()
    {
        // Test that the inheritance relationships are set up correctly for base model configuration
        
        // Book and Magazine should inherit from BaseProduct
        Assert.IsTrue(typeof(BaseProduct).IsAssignableFrom(typeof(Book)),
            "Book should inherit from BaseProduct");
            
        Assert.IsTrue(typeof(BaseProduct).IsAssignableFrom(typeof(Magazine)),
            "Magazine should inherit from BaseProduct");
            
        // BaseProduct should be abstract
        Assert.IsTrue(typeof(BaseProduct).IsAbstract,
            "BaseProduct should be abstract");
            
        // Other entities should not inherit from BaseProduct
        Assert.IsFalse(typeof(BaseProduct).IsAssignableFrom(typeof(Category)),
            "Category should not inherit from BaseProduct");
            
        Assert.IsFalse(typeof(BaseProduct).IsAssignableFrom(typeof(Author)),
            "Author should not inherit from BaseProduct");
            
        Assert.IsFalse(typeof(BaseProduct).IsAssignableFrom(typeof(BookAuthor)),
            "BookAuthor should not inherit from BaseProduct");
    }

    // Minimal test context for compilation validation
    private sealed class MinimalTestDbContext : DbContext
    {
        public MinimalTestDbContext(DbContextOptions<MinimalTestDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            TestModelLoader loader = new();
            modelBuilder.LoadModels(loader, policies =>
            {
                policies.AddEntityNamingPolicy(EntityNamingPolicy.AllowImplicit)
                        .AddPropertyMappingPolicy(PropertyMappingPolicy.AllowImplicit);
            });
        }
    }
}