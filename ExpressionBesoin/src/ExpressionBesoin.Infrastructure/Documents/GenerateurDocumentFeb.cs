using System.Collections.Frozen;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Xml.Linq;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Infrastructure.Documents;

/// <summary>Partie du paquet du gabarit : nom d'entrée et octets décompressés.</summary>
internal sealed record PartieGabarit(string Nom, byte[] Octets);

/// <summary>
/// Adaptateur <see cref="IGenerateurDocument"/> : .docx au modèle FEB (DEB-Q34), sans SDK OpenXML (paquet = zip, parties
/// XML). Le gabarit est lu, décompressé et VÉRIFIÉ une fois, à la construction : un gabarit inutilisable lève
/// <see cref="InvalidDataException"/> au démarrage, jamais au téléchargement. Chaque génération recopie les parties dans
/// l'ordre du gabarit, ne remplace que <c>word/document.xml</c> et <c>docProps/core.xml</c>, avec un horodatage fixe :
/// mêmes entrées, mêmes octets (DEB-Q37). Sans état mutable : un seul exemplaire pour toute l'application.
/// </summary>
public sealed class GenerateurDocumentFeb : IGenerateurDocument
{
    /// <summary>Nom de la ressource embarquée : le modèle FEB nettoyé de ses métadonnées (DEB-Q9, DEB-Q33).</summary>
    public const string NomRessource = "ExpressionBesoin.Infrastructure.Documents.FEB.docx";

    internal const string PartieDocument = "word/document.xml";
    internal const string PartieCore = "docProps/core.xml";

    /// <summary>Horodatage de toutes les entrées : le plus ancien du format zip (01/01/1980), jamais l'horloge.</summary>
    internal static readonly DateTimeOffset Horodatage = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Parties déjà compressées (images) : stockées telles quelles, sans recompression inutile.</summary>
    private static readonly FrozenSet<string> ExtensionsStockees =
        new[] { ".jpeg", ".jpg", ".png", ".gif" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private readonly ImmutableArray<PartieGabarit> _parties;
    private readonly XDocument _document;

    public GenerateurDocumentFeb(byte[] gabarit)
    {
        _parties = Lire(gabarit);
        var document = _parties.FirstOrDefault(partie => partie.Nom == PartieDocument)
            ?? throw new InvalidDataException($"Gabarit FEB sans partie {PartieDocument}.");
        if (!_parties.Any(partie => partie.Nom == PartieCore))
        {
            throw new InvalidDataException($"Gabarit FEB sans partie {PartieCore}.");
        }

        using var flux = new MemoryStream(document.Octets, writable: false);
        _document = XDocument.Load(flux);
        var anomalies = GabaritFeb.Anomalies(_document);
        if (!anomalies.IsEmpty)
        {
            throw new InvalidDataException("Gabarit FEB inutilisable : " + string.Join(" ", anomalies));
        }
    }

    /// <summary>Générateur sur le modèle FEB embarqué dans l'assembly.</summary>
    public static GenerateurDocumentFeb DepuisRessource()
    {
        using var ressource = typeof(GenerateurDocumentFeb).Assembly.GetManifestResourceStream(NomRessource)!;
        using var tampon = new MemoryStream();
        ressource.CopyTo(tampon);
        return new GenerateurDocumentFeb(tampon.ToArray());
    }

    public ValueTask<byte[]> Generer(ContenuDocument contenu, CancellationToken annulation)
    {
        annulation.ThrowIfCancellationRequested();
        var document = GabaritFeb.Octets(GabaritFeb.Remplir(_document, contenu));
        var core = MetadonneesFeb.Octets(contenu);
        using var paquet = new MemoryStream();
        using (var archive = new ZipArchive(paquet, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var partie in _parties)
            {
                var octets = partie.Nom switch
                {
                    PartieDocument => document,
                    PartieCore => core,
                    _ => partie.Octets,
                };
                var entree = archive.CreateEntry(partie.Nom, Compression(partie.Nom));
                entree.LastWriteTime = Horodatage;
                using var flux = entree.Open();
                flux.Write(octets);
            }
        }

        return ValueTask.FromResult(paquet.ToArray());
    }

    /// <summary>Image déjà compressée → stockée ; toute autre partie → compression optimale.</summary>
    internal static CompressionLevel Compression(string nom) =>
        ExtensionsStockees.Contains(Path.GetExtension(nom)) ? CompressionLevel.NoCompression : CompressionLevel.Optimal;

    private static ImmutableArray<PartieGabarit> Lire(byte[] gabarit)
    {
        using var flux = new MemoryStream(gabarit, writable: false);
        using var archive = new ZipArchive(flux, ZipArchiveMode.Read);
        var parties = ImmutableArray.CreateBuilder<PartieGabarit>(archive.Entries.Count);
        foreach (var entree in archive.Entries)
        {
            using var source = entree.Open();
            using var tampon = new MemoryStream();
            source.CopyTo(tampon);
            parties.Add(new PartieGabarit(entree.FullName, tampon.ToArray()));
        }

        return parties.MoveToImmutable();
    }
}
