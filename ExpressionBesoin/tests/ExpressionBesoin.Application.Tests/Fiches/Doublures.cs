using System.Collections.Immutable;
using System.Text;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Application.Tests.Fiches;

// Doublures en mémoire des ports. differe = true : chaque appel rend la main avant de répondre (Task.Yield), pour que
// chaque attente des cas d'usage soit exercée terminée ET non terminée (branches des machines asynchrones).

internal sealed class DepotEnMemoire(bool differe = false) : IDepotFiches
{
    private readonly Dictionary<Guid, FicheBesoin> _fiches = [];
    private readonly Dictionary<Guid, ListeImmuable<Echange>> _historiques = [];

    /// <summary>Nombre d'appels à <see cref="Enregistrer"/>.</summary>
    public int Enregistrements { get; private set; }

    /// <summary>Dernier numéro imposé ; <c>null</c> → calculé sur les fiches présentes.</summary>
    public string? DernierImpose { get; set; }

    /// <summary>Mise en place d'un essai, hors compte des enregistrements.</summary>
    public DepotEnMemoire Placer(FicheBesoin fiche, ListeImmuable<Echange>? historique = null)
    {
        _fiches[fiche.Id] = fiche;
        _historiques[fiche.Id] = historique ?? ListeImmuable<Echange>.Vide;
        return this;
    }

    public FicheBesoin? FicheDe(Guid id) => _fiches.GetValueOrDefault(id);

    public ListeImmuable<Echange> HistoriqueDe(Guid id) => _historiques.GetValueOrDefault(id) ?? ListeImmuable<Echange>.Vide;

    public async ValueTask<FicheBesoin?> Lire(Guid id, CancellationToken annulation)
    {
        await Attendre();
        return FicheDe(id);
    }

    public async ValueTask<ListeImmuable<Echange>> LireHistorique(Guid id, CancellationToken annulation)
    {
        await Attendre();
        return HistoriqueDe(id);
    }

    public async ValueTask Enregistrer(FicheBesoin fiche, ListeImmuable<Echange> echangesAjoutes, CancellationToken annulation)
    {
        await Attendre();
        Enregistrements++;
        var historique = HistoriqueDe(fiche.Id);
        foreach (var echange in echangesAjoutes)
        {
            historique = historique.Ajouter(echange);
        }

        _fiches[fiche.Id] = fiche;
        _historiques[fiche.Id] = historique;
    }

    public async ValueTask<ImmutableArray<ResumeFiche>> Lister(CancellationToken annulation)
    {
        await Attendre();
        return [.. _fiches.Values.OrderByDescending(fiche => fiche.EnTete.CreeeLe).Select(fiche => ResumeFiche.De(fiche))];
    }

    public async ValueTask<string?> DernierNumero(CancellationToken annulation)
    {
        await Attendre();
        return DernierImpose
            ?? _fiches.Values.Select(fiche => fiche.Numero).OfType<string>().Order(StringComparer.Ordinal).LastOrDefault();
    }

    private async ValueTask Attendre()
    {
        if (differe)
        {
            await Task.Yield();
        }
    }
}

internal sealed class AssistantScenarise(bool differe, params string[] reponses) : IAssistantIA
{
    private readonly Queue<string> _reponses = new(reponses);

    public List<ContexteTour> Recus { get; } = [];

    /// <summary>Panne simulée : levée à l'appel, après réception du contexte.</summary>
    public Exception? Panne { get; set; }

    public async ValueTask<string> Repondre(ContexteTour contexte, CancellationToken annulation)
    {
        if (differe)
        {
            await Task.Yield();
        }

        Recus.Add(contexte);
        if (Panne is not null)
        {
            throw Panne;
        }

        return _reponses.Dequeue();
    }
}

internal sealed class GenerateurFactice(bool differe = false) : IGenerateurDocument
{
    public List<FicheBesoin> Recues { get; } = [];

    public async ValueTask<byte[]> Generer(FicheBesoin fiche, CancellationToken annulation)
    {
        if (differe)
        {
            await Task.Yield();
        }

        Recues.Add(fiche);
        return Encoding.UTF8.GetBytes("docx de " + fiche.Numero);
    }
}

internal sealed class ReferentielFictif(bool differe = false) : IReferentielOrganisation
{
    public async ValueTask<Agent?> TrouverAgent(string identifiant, CancellationToken annulation)
    {
        if (differe)
        {
            await Task.Yield();
        }

        return identifiant == Essais.AgentFictif.Identifiant ? Essais.AgentFictif : null;
    }

    public async ValueTask<ImmutableArray<Agent>> ListerAgents(CancellationToken annulation)
    {
        if (differe)
        {
            await Task.Yield();
        }

        return [Essais.AgentFictif];
    }
}

/// <summary>Horloge réglée : rend <see cref="Maintenant"/>, puis avance de <see cref="Pas"/> à chaque lecture.</summary>
internal sealed class HorlogeReglee(DateTimeOffset maintenant) : TimeProvider
{
    public DateTimeOffset Maintenant { get; set; } = maintenant;

    public TimeSpan Pas { get; set; } = TimeSpan.Zero;

    public override DateTimeOffset GetUtcNow()
    {
        var lu = Maintenant;
        Maintenant += Pas;
        return lu;
    }
}
