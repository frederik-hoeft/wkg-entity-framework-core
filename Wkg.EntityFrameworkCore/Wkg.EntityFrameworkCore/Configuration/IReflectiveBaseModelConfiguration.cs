namespace Wkg.EntityFrameworkCore.Configuration;

/// <inheritdoc cref="IDiscoverableBaseModelConfiguration{TParentClass}" />
[Obsolete($"{DeprecationNotice.INTERFACE_REMOVAL} Use IDiscoverableBaseModelConfiguration<TParentClass> instead.")]
public interface IReflectiveBaseModelConfiguration<TParentClass> : IBaseModelConfiguration<TParentClass>
    where TParentClass : class, IReflectiveBaseModelConfiguration<TParentClass>;