using Wkg.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Wkg.EntityFrameworkCore.Configuration;

/// <summary>
/// Represents a model configuration that will be dynamically configured through a corresponding <see cref="IModelLoader"/> or the <see cref="ModelBuilderExtensions.LoadReflectiveModels(ModelBuilder, Action{IModelOptionsBuilder}?)"/> method.
/// </summary>
/// <typeparam name="T">The type of the model.</typeparam>
/// <remarks>
/// <para>
/// This interface must be implemented by the entity to be configured.
/// </para>
/// </remarks>
public interface IDiscoverableModelConfiguration<T> : IModelConfiguration<T> where T : class, IDiscoverableModelConfiguration<T>;