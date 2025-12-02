using Wkg.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Wkg.EntityFrameworkCore.Configuration;

/// <summary>
/// Represents a data seed for a model that will be dynamically configured through a corresponding <see cref="IModelLoader"/> or the <see cref="ModelBuilderExtensions.LoadReflectiveModels(ModelBuilder, Action{IModelOptionsBuilder}?)"/> method.
/// </summary>
/// <typeparam name="T">The type of the model that the data seed applies to.</typeparam>
public interface IDiscoverableModelDataSeed<T> : IModelDataSeed<T> where T : class, IDiscoverableModelConfiguration<T>;