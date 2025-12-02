using Microsoft.EntityFrameworkCore;
using Wkg.EntityFrameworkCore.Extensions;

namespace Wkg.EntityFrameworkCore.Configuration;

/// <summary>
/// Represents a dynamically-loaded configuration for an abstract base model when using Table-Per-Concrete-Type (TPC) inheritance.
/// Requires a corresponding <see cref="IModelLoader"/> or the <see cref="ModelBuilderExtensions.LoadReflectiveModels(ModelBuilder, Action{IModelOptionsBuilder}?)"/> method to be invoked.
/// </summary>
/// <typeparam name="TParentClass">The type of the parent class.</typeparam>
public interface IDiscoverableBaseModelConfiguration<TParentClass> : IBaseModelConfiguration<TParentClass>
    where TParentClass : class, IDiscoverableBaseModelConfiguration<TParentClass>;