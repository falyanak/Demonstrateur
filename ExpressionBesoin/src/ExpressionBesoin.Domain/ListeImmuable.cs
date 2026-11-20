using System.Collections;
using System.Collections.Immutable;

namespace ExpressionBesoin.Domain;

/// <summary>
/// Liste immuable à égalité STRUCTURELLE : un record qui la porte se compare élément par élément (un
/// <see cref="ImmutableArray{T}"/> ne se compare que par référence). Énumération sans allocation par <c>foreach</c>.
/// </summary>
public sealed class ListeImmuable<T> : IReadOnlyList<T>, IEquatable<ListeImmuable<T>>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _elements;

    private ListeImmuable(ImmutableArray<T> elements) => _elements = elements;

    public static ListeImmuable<T> Vide { get; } = new(ImmutableArray<T>.Empty);

    public static ListeImmuable<T> De(IEnumerable<T> elements) => new([.. elements]);

    public int Count => _elements.Length;

    public T this[int index] => _elements[index];

    public ListeImmuable<T> Ajouter(T element) => new(_elements.Add(element));

    public ListeImmuable<T> Remplacer(int index, T element) => new(_elements.SetItem(index, element));

    public bool Equals(ListeImmuable<T>? autre) =>
        autre is not null && _elements.AsSpan().SequenceEqual(autre._elements.AsSpan());

    public override bool Equals(object? obj) => Equals(obj as ListeImmuable<T>);

    public override int GetHashCode()
    {
        var empreinte = new HashCode();
        foreach (var element in _elements)
        {
            empreinte.Add(element);
        }

        return empreinte.ToHashCode();
    }

    public ImmutableArray<T>.Enumerator GetEnumerator() => _elements.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_elements).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_elements).GetEnumerator();
}
