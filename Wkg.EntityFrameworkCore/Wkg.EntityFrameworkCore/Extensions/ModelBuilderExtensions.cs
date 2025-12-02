using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration;
using Wkg.EntityFrameworkCore.Configuration.Reflection;
using Wkg.EntityFrameworkCore.Configuration.Reflection.Discovery;
using Wkg.EntityFrameworkCore.Configuration.Discovery;
using Wkg.EntityFrameworkCore.Configuration.Policies;
using Wkg.EntityFrameworkCore.Configuration.Policies.Defaults.PropertyMappingPolicies;
using Wkg.EntityFrameworkCore.Configuration.Policies.Defaults.EntityNamingPolicies;
using Wkg.EntityFrameworkCore.Configuration.Policies.Builder;
using System.Diagnostics.CodeAnalysis;

namespace Wkg.EntityFrameworkCore.Extensions;

/// <summary>
/// Holds extension methods for <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
#pragma warning disable CA1034 // Nested types should not be visible
    // TODO: CA1034 doesn't yet recognize C# 14 semantics for extension everything (remove suppressions when false positives are fixed)
    extension(ModelBuilder self)
#pragma warning restore CA1034 // Nested types should not be visible
    {
        /// <summary>
        /// Initializes a new <see cref="IEntityDiscoveryContext"/> using the specified <paramref name="policies"/>.
        /// </summary>
        /// <param name="policies">The policies to be enforced on the discovered entities.</param>
        /// <returns>The <see cref="IEntityDiscoveryContext"/>.</returns>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "API consistency with other extension methods.")]
        public IEntityDiscoveryContext CreateDiscoveryContext(IEntityPolicy[] policies) => new EntityDiscoveryContext(policies);

        /// <summary>
        /// Loads and configures the specified <typeparamref name="TModel"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="discoveryContext">The <see cref="IEntityDiscoveryContext"/> to be used for discovery. 
        /// The discovery context can later be used to enforce policies on the discovered entities.</param>
        /// <returns>The model builder.</returns>
        public ModelBuilder LoadModel<TModel>(IEntityDiscoveryContext? discoveryContext = null)
            where TModel : class, IModelConfiguration<TModel>
        {
            ArgumentNullException.ThrowIfNull(self);
            EntityTypeBuilder<TModel> entityBuilder = self.Entity<TModel>();
            TModel.Configure(entityBuilder);
            discoveryContext?.Register(typeof(TModel), entityBuilder);
            return self;
        }

        /// <summary>
        /// Loads and configures the specified <typeparamref name="TConnection"/> entity between the specified <typeparamref name="TLeft"/> and <typeparamref name="TRight"/> entities.
        /// </summary>
        /// <typeparam name="TConnection">The type of the connection entity.</typeparam>
        /// <typeparam name="TLeft">The type of the left entity.</typeparam>
        /// <typeparam name="TRight">The type of the right entity.</typeparam>
        /// <param name="discoveryContext">The <see cref="IEntityDiscoveryContext"/> to be used for discovery. 
        /// The discovery context can later be used to enforce policies on the discovered entities.</param>
        /// <returns>The model builder.</returns>
        public ModelBuilder LoadConnection<TConnection, TLeft, TRight>(IEntityDiscoveryContext? discoveryContext = null)
            where TConnection : class, IModelConnection<TConnection, TLeft, TRight>
            where TLeft : class, IModelConfiguration<TLeft>
            where TRight : class, IModelConfiguration<TRight>
        {
            ArgumentNullException.ThrowIfNull(self);
            TConnection.Connect(self.Entity<TLeft>(), self.Entity<TRight>());
            discoveryContext?.Register(typeof(TConnection), self.Entity<TConnection>());
            return self;
        }

        /// <summary>
        /// Loads seed data for the specified <typeparamref name="TDataSeed"/> model.
        /// </summary>
        /// <typeparam name="TDataSeed">The type of the data seed.</typeparam>
        /// <returns>The model builder.</returns>
        public ModelBuilder LoadDataSeed<TDataSeed>()
            where TDataSeed : class, IModelDataSeed<TDataSeed>, IModelConfiguration<TDataSeed>
        {
            ArgumentNullException.ThrowIfNull(self);
            self.Entity<TDataSeed>().HasData(TDataSeed.GetSeedData());
            return self;
        }

        /// <summary>
        /// Loads seed data for the specified <typeparamref name="TModel"/> model using the specified <typeparamref name="TDataSeed"/>.
        /// </summary>
        /// <typeparam name="TDataSeed">The type of the data seed.</typeparam>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <returns>The model builder.</returns>
        public ModelBuilder LoadDataSeed<TDataSeed, TModel>() 
            where TDataSeed : IModelDataSeed<TModel>
            where TModel : class, IModelConfiguration<TModel>
        {
            ArgumentNullException.ThrowIfNull(self);
            self.Entity<TModel>().HasData(TDataSeed.GetSeedData());
            return self;
        }

        /// <summary>
        /// Loads models using the specified <paramref name="loader"/> validates them against the configured policies.
        /// </summary>
        /// <param name="loader">The model loader to load the models from.</param>
        /// <param name="configurePolicies">The policies to configure the discovery process.</param>
        /// <returns>The model builder.</returns>
        public ModelBuilder LoadModels(IModelLoader loader, Action<IPolicyOptionsBuilder>? configurePolicies = null)
        {
            ArgumentNullException.ThrowIfNull(self);
            ArgumentNullException.ThrowIfNull(loader);

            IPolicyOptionsBuilder policyOptionsBuilder = new PolicyOptionsBuilder();
            configurePolicies?.Invoke(policyOptionsBuilder);

            EntityNaming.AddDefaults(policyOptionsBuilder);
            PropertyMapping.AddDefaults(policyOptionsBuilder);

            IEntityPolicy[] policies = policyOptionsBuilder.Build();
            EntityDiscoveryContext discoveryContext = new(policies);
            loader.LoadModels(self, discoveryContext);
            discoveryContext.AuditPolicies();
            return self;
        }

        /// <summary>
        /// Loads and configures all models that implement <see cref="IDiscoverableModelConfiguration{T}"/>.
        /// </summary>
        /// <param name="configureOptions">The options to configure the discovery process.</param>
        /// <returns>The model builder.</returns>
        /// <remarks>
        /// <para>
        /// This method uses reflection to find all types that implement <see cref="IDiscoverableModelConfiguration{T}"/> and then loads and configures them.
        /// Models implementing <see cref="IDiscoverableModelConfiguration{T}"/> should not be loaded explicitly using <see cref="LoadModel{TModel}(ModelBuilder, IEntityDiscoveryContext)"/>.
        /// </para>
        /// </remarks>
        public ModelBuilder LoadReflectiveModels(Action<IModelOptionsBuilder>? configureOptions)
        {
            ArgumentNullException.ThrowIfNull(self);

            ModelOptionsBuilder modelOptions = new();
            configureOptions?.Invoke(modelOptions);
            AddDefaults(modelOptions);
            IEntityPolicy[] policies = modelOptions.PolicyOptionsBuilder.Build();
            DiscoveryOptions discoveryOptions = modelOptions.DiscoveryOptionsBuilder.Build();

            IReflectiveEntityDiscoveryContext discoveryContext = new EntityDiscoveryContext(policies);
            discoveryContext.AddLoader(new ReflectiveModelLoader());
            discoveryContext.AddLoader(new ReflectiveConnectionLoader());
            discoveryContext.AddLoader(new ReflectiveDataSeedLoader());
            discoveryContext.Discover(self, discoveryOptions);
            discoveryContext.AuditPolicies();
            return self;
        }

        private static void AddDefaults(ModelOptionsBuilder modelOptions)
        {
            EntityNaming.AddDefaults(modelOptions.PolicyOptionsBuilder);
            PropertyMapping.AddDefaults(modelOptions.PolicyOptionsBuilder);
        }
    }
}