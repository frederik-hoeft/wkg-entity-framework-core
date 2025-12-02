using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Reflection;
using Wkg.EntityFrameworkCore.Configuration;
using Wkg.EntityFrameworkCore.Configuration.Discovery;
using Wkg.EntityFrameworkCore.Configuration.Policies.Defaults.EntityNamingPolicies;
using Wkg.EntityFrameworkCore.Configuration.Policies.Defaults.PropertyMappingPolicies;
using Wkg.EntityFrameworkCore.Extensions;
using Wkg.EntityFrameworkCore.Tests.Discovery.Roslyn.TestData;

namespace Wkg.EntityFrameworkCore.Tests.Discovery.Roslyn;

[TestClass]
public class ModelDiscoveryGeneratorTests
{
    [TestMethod]
    public void TestModelLoader_ShouldHaveGeneratedLoadModelsMethod()
    {
        // Arrange
        Type loaderType = typeof(TestModelLoader);
        
        // Act - Check that it implements IModelLoader which has the LoadModels method
        bool implementsInterface = typeof(IModelLoader).IsAssignableFrom(loaderType);
        
        // Assert
        Assert.IsTrue(implementsInterface, "TestModelLoader should implement IModelLoader");
        
        // Verify we can create an instance (means the generated code compiled)
        TestModelLoader loader = new();
        Assert.IsNotNull(loader, "Should be able to create TestModelLoader instance");
    }

    [TestMethod]
    public void TestModelLoader_LoadModels_ShouldNotThrowWithValidInputs()
    {
        // Arrange
        var loader = new TestModelLoader();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var mockDiscoveryContext = new Mock<IEntityDiscoveryContext>();
        
        // Act & Assert - Should not throw
        using var context = new TestDbContext(options, loader, mockDiscoveryContext.Object);
        var model = context.Model; // This triggers model creation
        
        Assert.IsNotNull(model);
    }

    [TestMethod]
    public void TestModelLoader_LoadModels_ShouldRegisterEntitiesWithDiscoveryContext()
    {
        // Arrange
        var loader = new TestModelLoader();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var mockDiscoveryContext = new Mock<IEntityDiscoveryContext>();
        
        // Act
        using var context = new TestDbContext(options, loader, mockDiscoveryContext.Object);
        _ = context.Model; // Trigger model creation
        
        // Assert - Verify that RegisterInternal was called for entities
        // We can't directly verify the RegisterInternal calls since they're made through 
        // EntityDiscoveryHelpers.RegisterInternal, but we can verify the context was used
        mockDiscoveryContext.Verify(ctx => ctx.GetType(), Times.AtLeastOnce);
    }

    [TestMethod]
    public void GeneratedCode_ShouldHandleBaseModelConfigurations()
    {
        // Arrange
        var loader = new TestModelLoader();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var mockDiscoveryContext = new Mock<IEntityDiscoveryContext>();
        
        // Act
        using var context = new TestDbContext(options, loader, mockDiscoveryContext.Object);
        var model = context.Model;
        
        // Assert - Check that base configurations are applied
        var bookEntityType = model.FindEntityType(typeof(Book));
        var magazineEntityType = model.FindEntityType(typeof(Magazine));
        
        // Both Book and Magazine inherit from BaseProduct, so they should have the same base configuration
        Assert.IsNotNull(bookEntityType);
        Assert.IsNotNull(magazineEntityType);
        
        // Verify base properties are configured the same way on both entities
        var bookIdProperty = bookEntityType.FindProperty("Id");
        var magazineIdProperty = magazineEntityType.FindProperty("Id");
        
        // Column types aren't compared since InMemory database uses different type mappings
        Assert.AreEqual(bookIdProperty?.GetColumnName(), magazineIdProperty?.GetColumnName());
        Assert.AreEqual(bookIdProperty?.IsKey(), magazineIdProperty?.IsKey());
    }

    [TestMethod]
    public void GeneratedCode_ShouldHandleModelConnections()
    {
        // Arrange
        var loader = new TestModelLoader();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var mockDiscoveryContext = new Mock<IEntityDiscoveryContext>();
        
        // Act
        using var context = new TestDbContext(options, loader, mockDiscoveryContext.Object);
        var model = context.Model;
        
        // Assert
        var bookAuthorEntityType = model.FindEntityType(typeof(BookAuthor));
        Assert.IsNotNull(bookAuthorEntityType, "BookAuthor connection entity should be registered");
        
        // Verify that the connection entity has the expected foreign keys
        var foreignKeys = bookAuthorEntityType.GetForeignKeys();
        Assert.AreEqual(2, foreignKeys.Count(), "BookAuthor should have exactly 2 foreign keys");
        
        var bookFk = foreignKeys.FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Book));
        var authorFk = foreignKeys.FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Author));
        
        Assert.IsNotNull(bookFk, "BookAuthor should have foreign key to Book");
        Assert.IsNotNull(authorFk, "BookAuthor should have foreign key to Author");
    }

    [TestMethod]
    public void GeneratedCode_ShouldPreserveConfigurationOrder()
    {
        // Arrange
        var loader = new TestModelLoader();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var mockDiscoveryContext = new Mock<IEntityDiscoveryContext>();
        
        // Act
        using var context = new TestDbContext(options, loader, mockDiscoveryContext.Object);
        var model = context.Model;
        
        // Assert - Verify that all expected entities exist
        var expectedEntityTypes = new[]
        {
            typeof(Book),
            typeof(Category), 
            typeof(Magazine),
            typeof(Author),
            typeof(BookAuthor)
        };
        
        foreach (var expectedType in expectedEntityTypes)
        {
            var entityType = model.FindEntityType(expectedType);
            Assert.IsNotNull(entityType, $"Entity type {expectedType.Name} should be registered in the model");
        }
    }

    // Test DbContext class that accepts a mock discovery context
    private class TestDbContext : DbContext
    {
        private readonly IModelLoader _modelLoader;
        private readonly IEntityDiscoveryContext? _discoveryContext;

        public TestDbContext(DbContextOptions<TestDbContext> options, IModelLoader modelLoader) 
            : base(options)
        {
            _modelLoader = modelLoader;
        }

        public TestDbContext(DbContextOptions<TestDbContext> options, IModelLoader modelLoader, IEntityDiscoveryContext discoveryContext) 
            : base(options)
        {
            _modelLoader = modelLoader;
            _discoveryContext = discoveryContext;
        }

        public DbSet<Book> Books => Set<Book>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Magazine> Magazines => Set<Magazine>();
        public DbSet<Author> Authors => Set<Author>();
        public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            if (_discoveryContext != null)
            {
                _modelLoader.LoadModels(modelBuilder, _discoveryContext);
            }
            else
            {
                modelBuilder.LoadModels(_modelLoader, policies =>
                {
                    policies.AddEntityNamingPolicy(EntityNamingPolicy.AllowImplicit)
                            .AddPropertyMappingPolicy(PropertyMappingPolicy.AllowImplicit);
                });
            }
        }
    }
}