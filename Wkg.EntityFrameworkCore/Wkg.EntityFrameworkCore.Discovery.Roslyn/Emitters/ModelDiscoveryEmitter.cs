using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using Wkg.EntityFrameworkCore.Discovery.Roslyn;
using Wkg.EntityFrameworkCore.Discovery.Roslyn.Helpers;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn.Emitters;

internal static class ModelDiscoveryEmitter
{
    private static readonly FrozenDictionary<string, string> s_types = new Dictionary<string, string>()
    {
        { "EntityTypeBuilder", "global::Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder" },
        { "ModelBuilder", "global::Microsoft.EntityFrameworkCore.ModelBuilder" },
        { "IModelLoader", "global::Wkg.EntityFrameworkCore.Configuration.IModelLoader" },
        { "IEntityDiscoveryContext", "global::Wkg.EntityFrameworkCore.Configuration.Discovery.IEntityDiscoveryContext" },
        { "IModelConfiguration", "global::Wkg.EntityFrameworkCore.Configuration.IModelConfiguration" },
        { "IModelConnection", "global::Wkg.EntityFrameworkCore.Configuration.IModelConnection" },
        { "IBaseModelConfiguration", "global::Wkg.EntityFrameworkCore.Configuration.IBaseModelConfiguration" },
        { "EntityLoader", SymbolNameGenerator.MakeUnique("EntityLoader") },
        { "EntityConnectionLoader", SymbolNameGenerator.MakeUnique("EntityConnectionLoader") },
        { "EntityDiscoveryHelpers", "global::Wkg.EntityFrameworkCore.Configuration.Discovery.EntityDiscoveryHelpers" }
    }.ToFrozenDictionary();

    public static void EmitSource(SourceProductionContext context, ModelDiscoveryGeneratorModel model)
    {
        ModelConfigurationGenerator modelConfigurationGenerator = new(s_types);
        ModelConnectionConfigurationGenerator modelConnectionConfigurationGenerator = new(s_types);
        IEnumerable<IConfigurationCode> modelConfigurations = model.Models.Select(modelConfigurationGenerator.GenerateCode);
        IEnumerable<IConfigurationCode> connectionConfigurations = model.ModelConnections.Select(modelConnectionConfigurationGenerator.GenerateCode);

        IConfigurationCode[] allConfigurations = [..modelConfigurations, ..connectionConfigurations];
        FrozenDictionary<ITypeSymbol, IConfigurationCode> configurationMap = allConfigurations.ToFrozenDictionary<IConfigurationCode, ITypeSymbol, IConfigurationCode>(static c => c.Symbol, static c => c);
        foreach (IConfigurationCode configuration in allConfigurations)
        {
            configuration.ResolveDependencies(configurationMap);
        }
        IEnumerable<string> sourceLines = allConfigurations.SelectMany(c => c.EmitSourceLines());

        StringBuilder sourceBuilder = new(
            $$"""
            #nullable enable

            namespace {{model.Namespace}};

            partial class {{model.Class.Name}} : {{s_types["IModelLoader"]}}
            {
                void {{s_types["IModelLoader"]}}.LoadModels({{s_types["ModelBuilder"]}} builder, {{s_types["IEntityDiscoveryContext"]}} context)
                {
                    {{string.Join($"\r\n{new string(' ', 2 * 4)}", sourceLines)}}
                }
            }

            file readonly ref struct {{s_types["EntityConnectionLoader"]}}<TConnection, TLeft, TRight>
                where TConnection : class, {{s_types["IModelConnection"]}}<TConnection, TLeft, TRight>
                where TLeft : class, {{s_types["IModelConfiguration"]}}<TLeft>
                where TRight : class, {{s_types["IModelConfiguration"]}}<TRight>
            {
                internal readonly {{s_types["EntityTypeBuilder"]}}<TConnection> EntityBuilder { get; }

                private {{s_types["EntityConnectionLoader"]}}({{s_types["EntityTypeBuilder"]}}<TConnection> entityBuilder) => EntityBuilder = entityBuilder;

                public static {{s_types["EntityConnectionLoader"]}}<TConnection, TLeft, TRight> Configure({{s_types["ModelBuilder"]}} builder, {{s_types["EntityTypeBuilder"]}}<TLeft> leftBuilder, {{s_types["EntityTypeBuilder"]}}<TRight> rightBuilder)
                {
                    TConnection.Connect(leftBuilder, rightBuilder);
                    return new {{s_types["EntityConnectionLoader"]}}<TConnection, TLeft, TRight>(builder.Entity<TConnection>());
                }

                public {{s_types["EntityTypeBuilder"]}}<TConnection> Register({{s_types["IEntityDiscoveryContext"]}} context)
                {
                    {{s_types["EntityDiscoveryHelpers"]}}.RegisterInternal(EntityBuilder, context);
                    return EntityBuilder;
                }
            }

            file readonly ref struct {{s_types["EntityLoader"]}}<T> where T : class, {{s_types["IModelConfiguration"]}}<T>
            {
                internal readonly {{s_types["EntityTypeBuilder"]}}<T> EntityBuilder { get; }

                private {{s_types["EntityLoader"]}}({{s_types["EntityTypeBuilder"]}}<T> entityBuilder) =>
                    EntityBuilder = entityBuilder;

                public static {{s_types["EntityLoader"]}}<T> Configure({{s_types["ModelBuilder"]}} builder)
                {
                    {{s_types["EntityTypeBuilder"]}}<T> entityBuilder = builder.Entity<T>();
                    T.Configure(entityBuilder);
                    return new {{s_types["EntityLoader"]}}<T>(entityBuilder);
                }

                public {{s_types["EntityTypeBuilder"]}}<T> Register({{s_types["IEntityDiscoveryContext"]}} context)
                {
                    {{s_types["EntityDiscoveryHelpers"]}}.RegisterInternal(EntityBuilder, context);
                    return EntityBuilder;
                }
            }

            file static class {{SymbolNameGenerator.MakeUnique("EntityLoaderExtensions")}}
            {
                public static {{s_types["EntityLoader"]}}<T> ConfigureBase<T, TBase>(this {{s_types["EntityLoader"]}}<T> loader)
                    where TBase : class, {{s_types["IBaseModelConfiguration"]}}<TBase>
                    where T : class, TBase, {{s_types["IModelConfiguration"]}}<T>
                {
                    TBase.ConfigureBaseModel(loader.EntityBuilder);
                    return loader;
                }
            }
            """);

        SourceText sourceText = SourceText.From(sourceBuilder.ToString(), Encoding.UTF8);

        context.AddSource($"{model.Class.Name}.ModelRegistration.g.cs", sourceText);
    }
}