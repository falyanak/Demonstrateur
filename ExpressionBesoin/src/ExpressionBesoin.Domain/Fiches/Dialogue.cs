using System.Text;

namespace ExpressionBesoin.Domain.Fiches;

/// <summary>
/// Conduite du dialogue (plan §2, DEB-Q14) : la question est DÉTERMINISTE (lot A5, DEB-Q100). L'IA comprend, propose et
/// nomme la prestation ; l'application choisit la rubrique questionnée APRÈS la fusion des propositions du tour et pose
/// sa question type (<see cref="DefinitionRubrique.QuestionType"/>, DEB-Q101), ou une relance fixe (DEB-Q102). Le
/// message de l'IA est privé de ses questions (DEB-Q103) : une question sur une rubrique tout juste proposée ou déjà
/// répondue ne peut plus atteindre l'agent.
/// </summary>
public static class Dialogue
{
    /// <summary>Relance quand il ne reste que le résumé à questionner (DEB-Q102).</summary>
    public const string RelanceResume =
        "Toutes les rubriques obligatoires sont renseignées : envoyez « résumé » pour que je le propose.";

    /// <summary>Relance quand plus aucune rubrique n'est à questionner (DEB-Q102).</summary>
    public const string RelanceFin = "Relisez la fiche, acceptez ou corrigez les propositions, puis confirmez et soumettez.";

    /// <summary>
    /// Message d'une réponse hors périmètre dont il ne reste rien après le retrait des questions : l'échange enregistré
    /// n'est jamais vide.
    /// </summary>
    public const string HorsPerimetre = "Cette demande ne relève pas du service informatique.";

    /// <summary>Séparateur du message de l'IA et de la relance (le front garde les sauts de ligne : <c>pre-wrap</c>).</summary>
    public const string Separateur = "\n\n";

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

    /// <summary>
    /// Relance de l'application pour <paramref name="fiche"/> (DEB-Q102) : question type de la prochaine rubrique ;
    /// <see cref="RelanceResume"/> s'il ne reste que le résumé ; <see cref="RelanceFin"/> s'il ne reste rien. Toute
    /// rubrique de l'ordre de questionnement autre que le résumé a une question type (garanti par les tests du catalogue).
    /// </summary>
    public static string Relance(FicheBesoin fiche) => ProchaineRubrique(fiche) switch
    {
        null => RelanceFin,
        Rubrique.Resume => RelanceResume,
        Rubrique rubrique => Catalogue.Definition(rubrique).QuestionType!,
    };

    /// <summary>
    /// Message enregistré et rendu à l'agent pour un tour (DEB-Q102, DEB-Q103) : message de l'IA privé de ses questions
    /// (<see cref="SansQuestion"/>), puis la relance calculée sur la fiche APRÈS fusion ; hors périmètre, aucune relance
    /// (et <see cref="HorsPerimetre"/> si rien ne reste du message).
    /// </summary>
    public static string Composer(string messageIA, bool horsPerimetre, FicheBesoin fiche)
    {
        var texte = SansQuestion(messageIA);
        if (horsPerimetre)
        {
            return texte.Length == 0 ? HorsPerimetre : texte;
        }

        var relance = Relance(fiche);
        return texte.Length == 0 ? relance : texte + Separateur + relance;
    }

    /// <summary>
    /// Rubrique sur laquelle portait le dernier message de l'application : celle dont la question type le termine, le
    /// résumé s'il se termine par <see cref="RelanceResume"/> ; <c>null</c> sinon (relance de fin, message hors
    /// périmètre, message antérieur au lot A5).
    /// </summary>
    public static Rubrique? RubriqueQuestionnee(string messageIA)
    {
        if (messageIA.EndsWith(RelanceResume, StringComparison.Ordinal))
        {
            return Rubrique.Resume;
        }

        foreach (var rubrique in Catalogue.OrdreQuestionnement)
        {
            var question = Catalogue.Definition(rubrique).QuestionType;
            if (question is not null && messageIA.EndsWith(question, StringComparison.Ordinal))
            {
                return rubrique;
            }
        }

        return null;
    }

    /// <summary>
    /// Garde-fou (DEB-Q103) : retire les phrases terminées par un point d'interrogation. Une phrase finit à une suite de
    /// ponctuations finales (« . », « ! », « ? », « … ») suivie d'un blanc, d'un guillemet ou d'une parenthèse fermants,
    /// ou de la fin du texte (« 2.0 » ne coupe pas) ; un saut de ligne finit aussi une phrase. Elle est une question si
    /// sa ponctuation finale contient « ? » ; ses fermants et les blancs qui la suivent partent avec elle. Le reste est
    /// recopié tel quel ; le résultat est rogné de ses blancs de bord.
    /// </summary>
    public static string SansQuestion(string message)
    {
        var texte = message.AsSpan();
        var resultat = new StringBuilder(texte.Length);
        var debut = 0;
        var i = 0;
        while (i < texte.Length)
        {
            if (texte[i] == '\n')
            {
                resultat.Append(texte[debut..(i + 1)]);
                debut = ++i;
                continue;
            }

            if (!EstFinale(texte[i]))
            {
                i++;
                continue;
            }

            var question = false;
            var j = i;
            while (j < texte.Length && EstFinale(texte[j]))
            {
                question |= texte[j] == '?';
                j++;
            }

            if (j < texte.Length && !char.IsWhiteSpace(texte[j]) && !EstFermant(texte[j]))
            {
                i = j;
                continue;
            }

            var fin = ApresFermants(texte, j);
            while (fin < texte.Length && EstBlanc(texte[fin]))
            {
                fin++;
            }

            if (!question)
            {
                resultat.Append(texte[debut..fin]);
            }

            debut = i = fin;
        }

        resultat.Append(texte[debut..]);
        return resultat.ToString().Trim();
    }

    /// <summary>Position après les fermants qui suivent <paramref name="position"/>, chacun éventuellement précédé de blancs.</summary>
    private static int ApresFermants(ReadOnlySpan<char> texte, int position)
    {
        var fin = position;
        while (true)
        {
            var k = fin;
            while (k < texte.Length && EstBlanc(texte[k]))
            {
                k++;
            }

            if (k == texte.Length || !EstFermant(texte[k]))
            {
                return fin;
            }

            fin = k + 1;
        }
    }

    private static bool EstFinale(char c) => c is '.' or '!' or '?' or '…';

    /// <summary>Blanc dans la ligne (espaces insécables compris) ; le saut de ligne est traité à part.</summary>
    private static bool EstBlanc(char c) => c != '\n' && char.IsWhiteSpace(c);

    private static bool EstFermant(char c) => c is '»' or '"' or '”' or '’' or ')' or ']';
}
