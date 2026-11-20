namespace ExpressionBesoin.Domain;

/// <summary>Motif d'un échec : <paramref name="Code"/> stable (contrat, tests), <paramref name="Message"/> en français.</summary>
public sealed record Refus(string Code, string Message);

/// <summary>
/// Valeur ou refus. La valeur <c>default</c> n'est ni un succès ni un échec : tout usage lève
/// <see cref="InvalidOperationException"/>.
/// </summary>
public readonly struct Resultat<T>
{
    private readonly T _valeur;
    private readonly Refus? _refus;
    private readonly bool _initialise;

    private Resultat(T valeur, Refus? refus)
    {
        _valeur = valeur;
        _refus = refus;
        _initialise = true;
    }

    public static Resultat<T> Succes(T valeur) => new(valeur, null);

    public static Resultat<T> Echec(Refus refus)
    {
        ArgumentNullException.ThrowIfNull(refus);
        return new(default!, refus);
    }

    public static implicit operator Resultat<T>(Refus refus) => Echec(refus);

    public bool EstSucces
    {
        get
        {
            Verifier();
            return _refus is null;
        }
    }

    public TSortie Match<TSortie>(Func<T, TSortie> succes, Func<Refus, TSortie> echec)
    {
        Verifier();
        return _refus is null ? succes(_valeur) : echec(_refus);
    }

    public Resultat<TSortie> Map<TSortie>(Func<T, TSortie> transformer)
    {
        Verifier();
        return _refus is null ? Resultat<TSortie>.Succes(transformer(_valeur)) : Resultat<TSortie>.Echec(_refus);
    }

    public Resultat<TSortie> Bind<TSortie>(Func<T, Resultat<TSortie>> enchainer)
    {
        Verifier();
        return _refus is null ? enchainer(_valeur) : Resultat<TSortie>.Echec(_refus);
    }

    public Resultat<TSortie> Select<TSortie>(Func<T, TSortie> transformer) => Map(transformer);

    /// <summary>Syntaxe de requête : arrêt au premier refus, sans évaluer la suite.</summary>
    public Resultat<TResultat> SelectMany<TIntermediaire, TResultat>(
        Func<T, Resultat<TIntermediaire>> enchainer,
        Func<T, TIntermediaire, TResultat> projeter)
    {
        Verifier();
        if (_refus is not null)
        {
            return Resultat<TResultat>.Echec(_refus);
        }

        // Une lambda dans une struct ne capture pas this : copie locale (règles, annexe « C# »).
        var valeur = _valeur;
        return enchainer(valeur).Map(intermediaire => projeter(valeur, intermediaire));
    }

    private void Verifier()
    {
        if (!_initialise)
        {
            throw new InvalidOperationException("Résultat non initialisé (valeur default) : ni succès ni échec.");
        }
    }
}

/// <summary>Aide non générique, pour l'inférence : <c>Resultat.Succes(42)</c>.</summary>
public static class Resultat
{
    public static Resultat<T> Succes<T>(T valeur) => Resultat<T>.Succes(valeur);
}
