using System.Collections.Immutable;

namespace ExpressionBesoin.Domain.Fiches;

/// <summary>Complétude (plan §3) : toutes les rubriques obligatoires ont une valeur acceptée ou manuelle.</summary>
public static class Completude
{
    /// <summary>Rubriques obligatoires sans valeur retenue, dans l'ordre du catalogue.</summary>
    public static ImmutableArray<Rubrique> Manquantes(FicheBesoin fiche)
    {
        var manquantes = ImmutableArray.CreateBuilder<Rubrique>();
        foreach (var definition in Catalogue.Toutes)
        {
            if (definition.Obligatoire && fiche.ChampDe(definition.Rubrique).Origine == Origine.Vide)
            {
                manquantes.Add(definition.Rubrique);
            }
        }

        return manquantes.ToImmutable();
    }

    public static bool EstComplete(FicheBesoin fiche) => Manquantes(fiche).IsEmpty;

    /// <summary>Rubriques, obligatoires ou non, qui portent une proposition en attente (DEB-Q17).</summary>
    public static ImmutableArray<Rubrique> PropositionsEnAttente(FicheBesoin fiche)
    {
        var enAttente = ImmutableArray.CreateBuilder<Rubrique>();
        foreach (var definition in Catalogue.Toutes)
        {
            if (fiche.ChampDe(definition.Rubrique).PropositionEnAttente is not null)
            {
                enAttente.Add(definition.Rubrique);
            }
        }

        return enAttente.ToImmutable();
    }
}
