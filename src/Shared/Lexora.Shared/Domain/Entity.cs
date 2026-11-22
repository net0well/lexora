namespace Lexora.Shared.Domain;

public abstract class Entity : IEquatable<Entity>
{
    protected Entity()
    {
    }

    protected Entity(Guid id) => Id = id;

    public Guid Id { get; protected init; }

    public bool Equals(Entity? other) =>
        other is not null
        && (ReferenceEquals(this, other) || (Id != Guid.Empty && Id == other.Id && other.GetType() == GetType()));

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
