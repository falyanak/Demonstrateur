namespace ExpressionBesoin.Domain.Fiches;

/// <summary>Conduite du dialogue (plan §2, DEB-Q14).</summary>
public static class Dialogue
{
    /// <summary>
    /// Rubrique à questionner ensuite : la première, dans l'ordre de questionnement, qui est vide et sans proposition
    /// en attente ; <c>null</c> quand il n'y a plus de question à poser.
    /// </summary>
    public static Rubrique? ProchaineRubrique(FicheBesoin fiche)
    {
        foreach (var rubrique in Catalogue.OrdreQuestionnement)
        {
            var champ = fiche.ChampDe(rubrique);
            if (champ.Origine == Origine.Vide && champ.PropositionEnAttente is null)
            {
                return rubrique;
            }
        }

        return null;
    }
}
