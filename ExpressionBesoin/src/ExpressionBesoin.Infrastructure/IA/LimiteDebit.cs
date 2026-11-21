namespace ExpressionBesoin.Infrastructure.IA;

/// <summary>Limite de débit qu'une réponse HTTP 429 signale (DEB-Q87).</summary>
public enum NatureLimite : byte
{
    /// <summary>Non reconnue : pas de nouvel essai.</summary>
    Autre,

    /// <summary>Limite par minute : elle se lève d'elle-même ; un seul nouvel essai.</summary>
    Minute,

    /// <summary>Limite par jour : réessayer ne sert à rien avant sa remise à zéro.</summary>
    Jour,
}

/// <summary>
/// Décision de nouvel essai après une réponse 429 d'Albert API (DEB-Q87), fonctions pures. Corps d'Albert de la forme
/// <c>{"detail":"1280000 input tokens per day exceeded (remaining: 0)."}</c> (relevé dans CSAgent) ; la limite par jour
/// l'emporte quand les deux mots figurent.
/// </summary>
public static class LimiteDebit
{
    /// <summary>Attente au-delà de laquelle on ne réessaie pas : la panne est rendue (503) plutôt qu'un écran figé.</summary>
    public static readonly TimeSpan AttenteMaximale = TimeSpan.FromSeconds(10);

    /// <summary>Attente quand la réponse ne porte pas de <c>Retry-After</c> en secondes.</summary>
    public static readonly TimeSpan AttenteParDefaut = TimeSpan.FromSeconds(2);

    private static readonly string[] MarquesJour = ["per day", "per_day", "daily", "(tpd)", "(rpd)", "par jour"];

    private static readonly string[] MarquesMinute = ["per minute", "per min", "per_minute", "(tpm)", "(rpm)", "par minute"];

    public static NatureLimite Nature(string? corps)
    {
        if (string.IsNullOrEmpty(corps))
        {
            return NatureLimite.Autre;
        }

        var minuscules = corps.ToLowerInvariant();
        if (Contient(minuscules, MarquesJour))
        {
            return NatureLimite.Jour;
        }

        return Contient(minuscules, MarquesMinute) ? NatureLimite.Minute : NatureLimite.Autre;
    }

    /// <summary>
    /// Attente avant l'UNIQUE nouvel essai d'une réponse 429, <c>null</c> s'il ne faut pas réessayer : seule une limite
    /// par minute est réessayée, après <paramref name="reessayerApres"/> (à défaut <see cref="AttenteParDefaut"/>), pourvu
    /// que l'attente ne dépasse pas <see cref="AttenteMaximale"/>.
    /// </summary>
    public static TimeSpan? NouvelEssai(string? corps, TimeSpan? reessayerApres)
    {
        if (Nature(corps) != NatureLimite.Minute)
        {
            return null;
        }

        var attente = reessayerApres ?? AttenteParDefaut;
        return attente <= AttenteMaximale ? attente : null;
    }

    private static bool Contient(string texte, string[] marques)
    {
        foreach (var marque in marques)
        {
            if (texte.Contains(marque, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
