using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration.Discovery;

namespace Wkg.EntityFrameworkCore.Configuration.SourceGen;

public interface IModelConfigurationProvider
{
    void LoadModels(ModelBuilder builder, IEntityDiscoveryContext context);
}

public abstract class ModelConfigurationProvider : IModelConfigurationProvider
{
    public abstract void LoadModels(ModelBuilder builder, IEntityDiscoveryContext context);
}

public static class GenericDispatchHelper
{
    public static EntityTypeBuilder<TModel> Configure<TModel>(this ModelBuilder builder) where TModel : class, IModelConfiguration<TModel>
    {
        EntityTypeBuilder<TModel> entityBuilder = builder.Entity<TModel>();
        TModel.Configure(entityBuilder);
        return entityBuilder;
    }

    public static EntityTypeBuilder<TModel> ConfigureBase<TModel, TBaseModel>(this EntityTypeBuilder<TModel> builder)
        where TBaseModel : class, IBaseModelConfiguration<TBaseModel>
        where TModel : class, TBaseModel, IModelConfiguration<TModel>
    {
        TBaseModel.ConfigureBaseModel(builder);
        return builder;
    }

    public static void Register<TModel>(this EntityTypeBuilder<TModel> entitTypeBuilder, IEntityDiscoveryContext context) where TModel : class => 
        context.EntityBuilderCache.Add(typeof(TModel), entitTypeBuilder);
}

public static class EntityDiscoveryContextHelper
{
    public static void RegisterUnsafe<T>(EntityTypeBuilder<T> entityTypeBuilder, IEntityDiscoveryContext context) where T : class
    {
        if (!context.EntityBuilderCache.TryAdd(typeof(T), entityTypeBuilder))
        {
            throw new InvalidOperationException($"Entity type '{typeof(T).FullName}' is already registered.");
        }
    }
}