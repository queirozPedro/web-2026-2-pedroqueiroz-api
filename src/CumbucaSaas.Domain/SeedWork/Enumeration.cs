using System.Reflection;

namespace CumbucaSaas.Domain.SeedWork;

/// <summary>
/// Enum "rico": cada valor é uma instância estática com Id (persistido no banco)
/// e Name (o texto que trafega na API).
/// </summary>
public abstract class Enumeration : IComparable
{
    protected Enumeration(int id, string name) => (Id, Name) = (id, name);

    public int Id { get; }

    public string Name { get; }

    public override string ToString() => Name;

    public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
        typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(campo => campo.GetValue(null))
            .OfType<T>();

    public static T FromValue<T>(int id) where T : Enumeration =>
        GetAll<T>().FirstOrDefault(item => item.Id == id)
        ?? throw new InvalidOperationException($"'{id}' não é um valor válido de {typeof(T).Name}.");

    public static T FromName<T>(string name) where T : Enumeration =>
        TryFromName<T>(name, out var item)
            ? item
            : throw new InvalidOperationException($"'{name}' não é um nome válido de {typeof(T).Name}.");

    public static bool TryFromName<T>(string? name, out T item) where T : Enumeration
    {
        item = GetAll<T>().FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase))!;
        return item is not null;
    }

    public override bool Equals(object? obj) =>
        obj is Enumeration outro && GetType() == outro.GetType() && Id == outro.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public int CompareTo(object? outro) => Id.CompareTo(((Enumeration?)outro)?.Id);
}
