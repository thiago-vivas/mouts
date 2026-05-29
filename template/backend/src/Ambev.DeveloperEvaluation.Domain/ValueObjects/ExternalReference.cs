namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

/// <summary>
/// Represents a reference to an entity that lives in another domain/aggregate,
/// following the External Identities pattern with denormalization of the entity
/// description. We keep the external <see cref="Id"/> plus a denormalized
/// <see cref="Name"/> so we never hold a foreign key to another aggregate.
/// </summary>
public class ExternalReference
{
    /// <summary>The identifier of the referenced entity in its own domain.</summary>
    public Guid Id { get; private set; }

    /// <summary>The denormalized human-readable description of the referenced entity.</summary>
    public string Name { get; private set; } = string.Empty;

    // Parameterless constructor required by EF Core for owned types.
    private ExternalReference() { }

    public ExternalReference(Guid id, string name)
    {
        Id = id;
        Name = name ?? string.Empty;
    }
}
