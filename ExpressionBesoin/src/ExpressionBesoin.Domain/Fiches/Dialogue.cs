using System.Collections.Immutable;
using System.Text;

namespace ExpressionBesoin.Domain.Fiches;

/// <summary>
/// Conduite du dialogue (plan §2, DEB-Q14) : la RUBRIQUE questionnée est choisie de façon déterministe (lot A5,
/// DEB-Q100) ; la QUESTION est contextualisée (lot A7, DEB-Q116 à DEB-Q119). L'IA comprend, propose, nomme la
/// prestation et rédige des questions candidates ; l'application choisit la rubrique APRÈS la fusion des propositions du
/// tour et pose la candidate de l'IA pour cette rubrique si elle passe les garde-fous (<see cref="QuestionValide"/>),
/// sinon la question type (<see cref="DefinitionRubrique.QuestionType"/>, DEB-Q101), précédée du libellé de la rubrique
/// (DEB-Q118) ; ou une relance fixe (DEB-Q102). Le message de l'IA est privé de ses questions (DEB-Q103).
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

    /// <summary>Séparateur du libellé de la rubrique et de sa question (DEB-Q118) : « Critères de réussite : … ? ».</summary>
    public const string SeparateurLibelle = " : ";

    /// <summary>Longueur maximale d'une question candidate, blancs de bord retirés (DEB-Q119).</summary>
    public const int LongueurMaxQuestion = 200;

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
    /// Rubriques à questionner, dans l'ordre de questionnement, sans le résumé (proposé à la demande, DEB-Q102) : celles
    /// qui sont vides et sans proposition en attente. Transmises à l'IA, qui rédige une question pour les trois premières
    /// qu'elle ne propose pas (DEB-Q117). <see cref="ImmutableArray{T}"/>, comme <see cref="Catalogue.OrdreQuestionnement"/> :
    /// <see cref="ListeImmuable{T}"/> exige <c>IEquatable</c>, qu'une énumération n'implémente pas (A7, CS0315).
    /// </summary>
    public static ImmutableArray<Rubrique> AQuestionner(FicheBesoin fiche) =>
    [
        .. Catalogue.OrdreQuestionnement.Where(rubrique =>
        {
            var champ = fiche.ChampDe(rubrique);
            return rubrique != Rubrique.Resume && champ.Origine == Origine.Vide && champ.PropositionEnAttente is null;
        }),
    ];

    /// <summary>
    /// Relance de l'application pour <paramref name="fiche"/> (DEB-Q102) : question de la prochaine rubrique
    /// (<see cref="Question"/>) ; <see cref="RelanceResume"/> s'il ne reste que le résumé ; <see cref="RelanceFin"/> s'il
    /// ne reste rien. Une candidate de l'IA pour une autre rubrique, ou pour le résumé, n'est jamais posée (DEB-Q117).
    /// </summary>
    public static string Relance(FicheBesoin fiche, ListeImmuable<QuestionCandidate> questions) => ProchaineRubrique(fiche) switch
    {
        null => RelanceFin,
        Rubrique.Resume => RelanceResume,
        Rubrique rubrique => Question(rubrique, questions),
    };

    /// <summary>
    /// Question posée pour <paramref name="rubrique"/> : libellé, <see cref="SeparateurLibelle"/>, puis la première
    /// candidate de l'IA pour cette rubrique si elle passe <see cref="QuestionValide"/> (blancs de bord retirés), sinon la
    /// question type (DEB-Q117 à DEB-Q119). Toute rubrique de l'ordre de questionnement autre que le résumé a une question
    /// type (garanti par les tests du catalogue).
    /// </summary>
    public static string Question(Rubrique rubrique, ListeImmuable<QuestionCandidate> questions)
    {
        var definition = Catalogue.Definition(rubrique);
        string? retenue = null;
        foreach (var candidate in questions)
        {
            if (candidate.Rubrique == rubrique)
            {
                retenue = QuestionValide(candidate.Question) ? candidate.Question.Trim() : null;
                break;
            }
        }

        return definition.Libelle + SeparateurLibelle + (retenue ?? definition.QuestionType!);
    }

    /// <summary>
    /// Garde-fous d'une question candidate (DEB-Q119), blancs de bord retirés : non vide, <see cref="LongueurMaxQuestion"/>
    /// caractères au plus, une seule ligne, terminée par « ? » sans autre « ? », une seule phrase (aucune ponctuation
    /// finale « . », « ! », « … » suivie d'un blanc avant la fin : « 2.0 » ne coupe pas).
    /// </summary>
    public static bool QuestionValide(string question)
    {
        var texte = question.AsSpan().Trim();
        if (texte.Length == 0 || texte.Length > LongueurMaxQuestion || texte[^1] != '?')
        {
            return false;
        }

        var corps = texte[..^1];
        if (corps.IndexOfAny('?', '\n', '\r') >= 0)
        {
            return false;
        }

        for (var i = 0; i + 1 < corps.Length; i++)
        {
            if (EstFinale(corps[i]) && char.IsWhiteSpace(corps[i + 1]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Message enregistré et rendu à l'agent pour un tour (DEB-Q102, DEB-Q103) : message de l'IA privé de ses questions
    /// (<see cref="SansQuestion"/>), puis la relance calculée sur la fiche APRÈS fusion, avec les questions candidates de
    /// l'IA (<see cref="Relance"/>) ; hors périmètre, aucune relance ni question (et <see cref="HorsPerimetre"/> si rien ne
    /// reste du message).
    /// </summary>
    public static string Composer(string messageIA, bool horsPerimetre, FicheBesoin fiche, ListeImmuable<QuestionCandidate> questions)
    {
        var texte = SansQuestion(messageIA);
        if (horsPerimetre)
        {
            return texte.Length == 0 ? HorsPerimetre : texte;
        }

        var relance = Relance(fiche, questions);
        return texte.Length == 0 ? relance : texte + Separateur + relance;
    }

    /// <summary>
    /// Rubrique sur laquelle portait le dernier message de l'application : le résumé s'il se termine par
    /// <see cref="RelanceResume"/> ; celle dont le libellé, suivi de <see cref="SeparateurLibelle"/>, ouvre la dernière
    /// ligne terminée par « ? » (lot A7, DEB-Q118) ; à défaut, celle dont la question type le termine (messages des lots
    /// A5 à A7) ; <c>null</c> sinon (relance de fin, message hors périmètre, message antérieur au lot A5). Seules les
    /// rubriques de l'ordre de questionnement sont reconnues.
    /// </summary>
    public static Rubrique? RubriqueQuestionnee(string messageIA)
    {
        if (messageIA.EndsWith(RelanceResume, StringComparison.Ordinal))
        {
            return Rubrique.Resume;
        }

        var ligne = messageIA.AsSpan(messageIA.LastIndexOf('\n') + 1);
        if (ligne.EndsWith("?", StringComparison.Ordinal))
        {
            foreach (var rubrique in Catalogue.OrdreQuestionnement)
            {
                var libelle = Catalogue.Definition(rubrique).Libelle;
                if (ligne.StartsWith(libelle, StringComparison.Ordinal)
                    && ligne[libelle.Length..].StartsWith(SeparateurLibelle, StringComparison.Ordinal))
                {
                    return rubrique;
                }
            }
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
