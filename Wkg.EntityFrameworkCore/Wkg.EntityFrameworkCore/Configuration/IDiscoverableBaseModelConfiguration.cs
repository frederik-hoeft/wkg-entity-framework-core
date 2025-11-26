namespace Wkg.EntityFrameworkCore.Configuration;

/// <summary>
/// Represents a reflectively-loaded configuration for an abstract base model when using Table-Per-Concrete-Type (TPC) inheritance.
/// </summary>
/// <typeparam name="TParentClass">The type of the parent class.</typeparam>
public interface IDiscoverableBaseModelConfiguration<TParentClass> : IBaseModelConfiguration<TParentClass>
    where TParentClass : class, IDiscoverableBaseModelConfiguration<TParentClass>;