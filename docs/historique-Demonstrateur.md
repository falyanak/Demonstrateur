# Historique — Demonstrateur

## 20/11/2026 — P0a : amorçage du dépôt (type Documents)

- Contenu : `harnais.config.psd1` (type `Documents`, `HarnaisMinimal` 1.9.0), `.gitignore`, `.gitattributes`,
  `README.md`, `docs/prompt-reprise-Demonstrateur.md`, `docs/historique-Demonstrateur.md`,
  `specifications/plan-demonstrateur.md` (plan v1.0). Aucun code.
- Décisions : arbitrages DEB-Q1 à DEB-Q8 (« recommandations ok », 20/11/2026) ; DEB-D1 (dépôt `Demonstrateur`,
  racine `C:\Dev\Demonstrateur`, solution `ExpressionBesoin/`) ; DEB-D2 (dépôt public : documents internes hors
  dépôt). Questions ouvertes : DEB-Q9, DEB-Q10.
- Préalable côté harnais central : lot OT11 (inscription de `Demonstrateur` au registre de sauvegarde).
- Vérifié par l'assistant : rejeu sur une copie du harnais à HEAD `e80a113` (PowerShell 7.4.6 Linux) :
  `Appliquer-Lot OT11` (1010 tests verts sous Pester 4.10.1 et 3.4.0), `Amorcer-Depot P0a` (« inscrit au registre »),
  `Sauvegarder-Depots`, `Publier-Branche -Confirmer` vers un dépôt nu, `Ouvrir-Session` (8 pièces, état conforme) ;
  octets, BOM et fins de ligne relevés par Python. NON vérifié : Windows PowerShell 5.1, chemins
  Windows réels, publication sur GitHub.

## 20/11/2026 — P0a validé ; HAR-A8 relevée côté harnais

- Poste : OT11 appliqué au harnais (1010 tests verts sous Pester 4.10.1, commit `e834341`) ; `Amorcer-Depot P0a`
  (HEAD `56388a3`, « inscrit au registre ») ; `Sauvegarder-Depots` (Harnais, IAssistMe, Demonstrateur).
- Publication : premier essai, proxy du poste injoignable vers github.com (passager) ; second essai, échec
  d'identification (`/dev/tty` : aucun `credential.helper`, git lancé sans terminal par `Publier-Branche`). Après
  activation de Git Credential Manager, branche `main` publiée.
- `Ouvrir-Session` (`20261120-214128`) : 8 pièces conformes (tailles et SHA-256 de `PIECES.txt`, 8 entrées au total
  de `MASQUAGE.txt`, commit `56388a358996`), « ÉTAT : conforme », commits à publier 0. Critère de fin de P0a atteint.
- HAR-A8 (harnais) : `Publier-Branche` ne peut pas répondre à une demande d'identifiant ; correction recommandée plus
  tard (`GIT_TERMINAL_PROMPT=0`, absence de `credential.helper` signalée dans l'aperçu) ; à inscrire par un lot du
  harnais.

## 20/11/2026 — P0 : socle .NET 10, passage au type Projet

- Arbitrages DEB-Q11 (nuget.org seul, `nuget.config` du projet) et DEB-Q12 (`Microsoft.Data.Sqlite` sans Entity
  Framework), « recommandation ok ». Contrôles du poste : SDK 10.0.403, nuget.org joignable (200).
- Contenu : `harnais.config.psd1` type `Projet` (sections `Lot`, `Couverture`, vérification `Compter-Tests`) ;
  `ExpressionBesoin/` : `global.json`, `Directory.Build.props`, `Directory.Packages.props` (xunit.v3 3.2.2,
  xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.10.1, Microsoft.AspNetCore.Mvc.Testing 10.0.12),
  `nuget.config`, `.slnx`, outils de couverture épinglés (versions de la démonstration du harnais), base 0 / 0, dette
  vide ; Domain (`Resultat<T>`), Application et Infrastructure vides, Web (`api/sante`, `Localhost`, `HomeController`,
  layout provisoire) ; tests Domain (25), Web (30 dont 4 d'intégration), Architecture (20) : 75 ESTIMÉS.
- Choix : xUnit v3 et non v4 (migration de tous les projets ensemble, après essai) ; tous les projets de tests dans
  `Lot.TestsUnitaires` (les règles d'architecture contrôlées à chaque lot) ; `UsineWeb` fixe l'environnement
  `Production` ; `api/sante` lit la version informative (`0.1.0+<commit>` sur le poste).
- Vérifié chez l'assistant (SDK .NET 10.0.115 d'Ubuntu, NuGet inaccessible) : compilation Release des quatre projets
  de src, 0 avertissement ; site lancé, `api/sante` conforme au contrat (quatre champs, ordre, durée croissante),
  `/` et `/Home/Error` à 200, route inconnue à 404 ; tests compilés et exécutés sur une réplique locale de l'API
  xUnit : 71 verts (Domain 25, Architecture 20, Web 26 hors `SanteContratTests`) ; sept mutations sur copie jetable,
  chacune détectée (1 à 6 échecs) ; octets, BOM et fins de ligne relevés par Python.
- NON vérifié : restauration NuGet et compilation des projets de tests contre le vrai xUnit, `SanteContratTests`
  (`WebApplicationFactory`), Windows, SDK 10.0.403, couverture.

## 20/11/2026 — P0 validé ; P0b : mesures inscrites, chantier P clos

- Poste : `Appliquer-Lot P0` (« type Documents → Projet », 42 fichiers, compilation Release, Domain 25, Web 30,
  Architecture 20, 0 échec), commit `78ff21b` ; `Compter-Tests -Compiler` : 75 tests dans 3 projets, 0 échec ;
  `Lancer-Couverture -UpdateBaseline` : outils restaurés, lignes 64/64 et branches 16/16 (100 %), « COUVERTURE
  CONFORME », base relevée et commitée (`a73cb7e`). Les 75 ESTIMÉS au lot P0 sont confirmés par la mesure.
- Points non vérifiables chez l'assistant et tranchés par le poste : restauration NuGet, compilation des projets de
  tests avec xunit.v3 3.2.2, xunit.runner.visualstudio 3.1.5 et Microsoft.NET.Test.Sdk 18.10.1 ensemble,
  `SanteContratTests` (version informative `0.1.0+<commit>`).
- P0b : prompt de reprise (ÉTAT, §0, §1, §7) et historique. Documents seuls. Chantier P clos ; prochain : B1.

## 20/11/2026 — P0c : commande d'ouverture à chaque réponse, point de terminaison de la V1

- Demande du développeur : donner à chaque réponse la commande d'ouverture de la prochaine session (DEB-D3) ; fixer
  un point de terminaison même pour une première version simplifiée (DEB-D4 : lot R1, critère du plan §1, périmètre
  gelé, évolutions V2 listées au prompt §6).
- P0c : plan v1.2 (§1, §7, §8), prompt de reprise (ÉTAT, §0, §3, §6, §7), historique. Documents seuls.

## 20/11/2026 — B1 : Domaine de la fiche

- Session ouverte sur `8b64c420dc0c` : 8 pièces reçues, tailles et SHA-256 conformes à `PIECES.txt`, archive à 46
  entrées comme l'annonce `MASQUAGE.txt`.
- Plan de B1 présenté avant rédaction ; arbitrages DEB-Q13 à DEB-Q20 : « recommandations ok ». Écarts au plan v1.2
  tracés en v1.3 : origine `ProposeeIA` retirée (DEB-Q13) ; `Cycle.Appliquer(fiche, evenement, maintenant)` remplacé par
  une fonction par transition (`Soumettre`, `Decider`, `Reprendre`), sans hiérarchie d'événements (un record abstrait
  laisse des membres non couverts, règles annexe « Couverture ») ; maquette du §6 corrigée (10 obligatoires).
- Question du développeur sur le chatbot et la technologie du dialogue : recommandation inscrite pour DEB-Q2 (plan §8),
  décision attendue avant A1.
- B1 : `ListeImmuable<T>` (égalité structurelle, pour que les records se comparent champ par champ) ; `Fiches/` :
  `Catalogue`, types de la fiche, `Fiche`, `Completude`, `Dialogue`, `Cycle`, `Numerotation`, `ReponseIA` ; 164 tests
  ESTIMÉS (Domain 25 → 189). Vérifié chez l'assistant : syntaxe C# (tree-sitter, contrôle d'abord mis en échec sur un
  fichier fautif), attendus recalculés par une réplique Python. Non vérifié : compilation, tests, couverture,
  mutations (poste).

## 20/11/2026 — B1 validé, B1b : comptes mesurés, technologie de l'IA

- `Appliquer-Lot B1` : 20 fichiers écrits et relus, compilation Release, 239 tests (Domain 189, Web 30,
  Architecture 20), 0 échec ; commit `3ee722c`. Les 239 ESTIMÉS au lot B1 sont confirmés par la mesure.
- Mutations sur copies jetables (DEB-Q20) : M1 (saisie manuelle écrasée par la proposition) → 16 échecs ; M2
  (numérotation décalée d'un rang) → 4 échecs ; 2 sur 2 détectées.
- `Lancer-Couverture -UpdateBaseline` : lignes 410/410, branches 156/156 (100 %), « COUVERTURE CONFORME », base
  inchangée (déjà à 100 / 100), rien à commiter. Critère de fin de B1 atteint.
- Le développeur retient Microsoft Agent Framework (1.0 GA, avril 2026) ; cadrage proposé (MAF sur `IChatClient`, usage
  limité à l'adaptateur) : « cadrage ok » → DEB-D5. DEB-Q2 ne porte plus que sur le fournisseur.
- B1b : plan v1.4 (§8), prompt de reprise (ÉTAT, §0, §1, §2, §3, §7), historique. Documents seuls.

## 20/11/2026 — B2 : Application (cas d'usage, ports, doublures)

- Session ouverte sur `17f3a8d1a6d2` : 8 pièces reçues, tailles et SHA-256 conformes à `PIECES.txt`, archive à 63
  entrées comme l'annonce `MASQUAGE.txt` (0 fichier masqué). Critère de fin de B1b constaté au journal (0 commit à
  publier, sauvegarde à HEAD).
- Plan de B2 présenté avant rédaction, critère de fin énoncé ; arbitrages DEB-Q21 à DEB-Q27 : « recommandations ok »
  (plan v1.5, §8).
- B2 : `ExpressionBesoin.Application` (ports `IDepotFiches`, `IAssistantIA`, `IGenerateurDocument`,
  `IReferentielOrganisation` ; modèles `Agent`, `Echange`, `ResumeFiche`, `ContexteTour`, `TourDialogue`… ; décisions
  pures `DecisionsFiches` ; coquille `ServiceFiches` ; aide `ResultatOuvert`) ; projet
  `ExpressionBesoin.Application.Tests` (doublures en mémoire, attente différée en option pour exercer les deux issues
  de chaque `await` ; parcours du critère de la V1 joué deux fois) ; configuration du harnais (projet de tests,
  assembly attendue) et solution. 65 tests ESTIMÉS (total 239 → 304). Vérifié chez l'assistant : syntaxe C#
  (tree-sitter, contrôle d'abord mis en échec sur un fichier fautif), attendus recalculés par une réplique Python
  (numérotation selon le fuseau, rubriques manquantes, prochaine rubrique, 19 écritures du parcours). Non vérifié :
  compilation, tests, couverture (branches des machines asynchrones notamment), mutations M3 et M4 (poste).

## 20/11/2026 — B2 validé, B2b : comptes et mesures

- Session ouverte sur `9007adf87501` (B2 commité) : 8 pièces reçues, tailles et SHA-256 conformes à `PIECES.txt`,
  archive à 74 entrées comme l'annonce `MASQUAGE.txt` (0 fichier masqué). Journal : arbre propre, 1 commit à publier,
  sauvegarde à `17f3a8d`.
- `Compter-Tests -Compiler` (journal de session) : 304 tests (Domain 189, Application 65, Web 30, Architecture 20),
  0 échec. Les 304 ESTIMÉS au lot B2 sont confirmés par la mesure.
- `Lancer-Couverture -UpdateBaseline` : Domain, Application, Web ; lignes 580/580, branches 202/202 (100 %),
  « COUVERTURE CONFORME », base inchangée (déjà à 100 / 100), rien à commiter.
- Mutations sur clone jetable dans `%TEMP%` (DEB-Q20), `DecisionsFiches.cs` : la commande M3/M4 du compte rendu de B2
  n'étant pas disponible dans la session, deux mutations équivalentes, nommées M5 et M6 pour éviter l'homonymie :
  M5 (année du numéro lue en UTC, fuseau ignoré, DEB-Q24) → 2 échecs ; M6 (propositions gardées hors périmètre,
  DEB-Q23) → 1 échec ; 2 sur 2 détectées. Critère de fin de B2 atteint ; chantier B clos.
- B2b : prompt de reprise (ÉTAT, §1, §7), historique. Documents seuls.

## 20/11/2026 — I1 : Infrastructure SQLite et référentiel fictif

- Session ouverte sur `e66d24892f2e` (B2b commité) : 8 pièces reçues, tailles et SHA-256 conformes à `PIECES.txt`,
  archive à 74 entrées comme l'annonce `MASQUAGE.txt` (0 fichier masqué). Critère de fin de B2b constaté au journal
  (0 commit à publier, sauvegarde à HEAD).
- Plan d'I1 présenté avant rédaction, critère de fin énoncé ; arbitrages DEB-Q28 à DEB-Q32 : « recommandations ok »
  (plan v1.6, §8).
- I1 : `ExpressionBesoin.Infrastructure` (`CodecFiche` pur : JSON écrit et lu à la main, sans réflexion ;
  `SchemaFiches` : schéma et requêtes, `DoitCreer` pur ; `ConnexionsSqlite` : clés étrangères, guillemets doubles
  refusés par `sqlite3_db_config` ; `DepotFichesSqlite` : coquille, tour atomique en une transaction `BEGIN IMMEDIATE`,
  appels synchrones rendus en `ValueTask` terminée ; `ReferentielOrganisationFictif` : 4 agents, 2 bureaux) ; paquet
  `Microsoft.Data.Sqlite` 10.0.11 ; projet `ExpressionBesoin.Infrastructure.Tests` (fichier temporaire au schéma de
  production ; JSON de référence figé à l'octet ; plans de requête vérifiés sans tri) ; configuration du harnais (projet
  de tests, assembly attendue) et solution. 48 tests ESTIMÉS (total 304 → 352).
- Vérifié chez l'assistant : syntaxe C# (tree-sitter, contrôle d'abord mis en échec sur un fichier fautif) ; schéma et
  requêtes rejoués dans sqlite3 3.53 (contraintes, ordre de la liste, MAX ordinal, rang suivant, plans d'exécution) ;
  JSON de référence, ticks et octets du BLOB recalculés par une réplique Python. Non vérifié : compilation, tests,
  couverture, appel natif `sqlite3_db_config` de SQLitePCLRaw, échappement exact de `Utf8JsonWriter` (poste).
- Mutations prévues (DEB-Q20), sur clone jetable, après le commit d'I1 : M7 (liste triée du plus ancien au plus récent)
  et M8 (guillemets doubles de nouveau lus comme des chaînes), un échec attendu chacune. Commande, depuis une console
  Windows PowerShell :

      Set-Location C:\Dev\Demonstrateur; & { $ErrorActionPreference = 'Stop'; $copie = Join-Path $env:TEMP ('deb-mut-I1-' + [guid]::NewGuid().ToString('N')); git clone -q C:\Dev\Demonstrateur $copie; if ($LASTEXITCODE) { throw 'clonage' }; $projet = Join-Path $copie 'ExpressionBesoin\tests\ExpressionBesoin.Infrastructure.Tests\ExpressionBesoin.Infrastructure.Tests.csproj'; $mutations = @(@{ Nom = 'M7'; Fichier = 'ExpressionBesoin\src\ExpressionBesoin.Infrastructure\Fiches\SchemaFiches.cs'; Avant = 'ORDER BY CreeeLe DESC, rowid DESC'; Apres = 'ORDER BY CreeeLe ASC, rowid ASC' }, @{ Nom = 'M8'; Fichier = 'ExpressionBesoin\src\ExpressionBesoin.Infrastructure\Sqlite\ConnexionsSqlite.cs'; Avant = 'DqsDml, 0, out _'; Apres = 'DqsDml, 1, out _' }); foreach ($m in $mutations) { $chemin = Join-Path $copie $m.Fichier; $texte = [IO.File]::ReadAllText($chemin); if (-not $texte.Contains($m.Avant)) { throw ($m.Nom + ' : motif absent') }; [IO.File]::WriteAllText($chemin, $texte.Replace($m.Avant, $m.Apres)); dotnet build $projet -c Debug -v q -nologo | Out-Null; if ($LASTEXITCODE) { throw ($m.Nom + ' : compilation') }; $dll = (dotnet msbuild $projet -getProperty:TargetPath -p:Configuration=Debug | Out-String).Trim(); $sortie = & ([IO.Path]::ChangeExtension($dll, '.exe')) | Out-String -Width 200; $code = $LASTEXITCODE; $total = ($sortie -split "`r?`n") | Where-Object { $_ -match 'Total:' } | Select-Object -Last 1; Write-Host ($m.Nom + ' : code ' + $code + ' ;' + $total); git -C $copie checkout -q -- $m.Fichier }; Remove-Item -Recurse -Force $copie }

  Attendu : `M7 : code 1 ; … Failed: 1 …` puis `M8 : code 1 ; … Failed: 1 …`.

## 20/11/2026 — I1 validé, I1b : comptes et mesures

- `Appliquer-Lot I1` : 18 fichiers écrits et relus, compilation Release, 352 tests (Domain 189, Application 65,
  Infrastructure 48, Web 30, Architecture 20), 0 échec ; « LOT I1 APPLIQUÉ » ; commit `f42de12`. Les 48 tests ESTIMÉS
  au lot I1 sont confirmés par la mesure ; les points non vérifiés chez l'assistant (compilation, appel natif
  `sqlite3_db_config`, JSON de `Utf8JsonWriter` à l'octet) sont levés par ces tests verts.
- Mutations sur clone jetable dans `%TEMP%` (DEB-Q20), par la commande de l'entrée I1 : M7 → 1 échec ; M8 → 1 échec ;
  2 sur 2 détectées.
- `Lancer-Couverture -UpdateBaseline` : Domain, Application, Infrastructure, Web ; lignes 918/918, branches 266/266
  (100 %), « COUVERTURE CONFORME », base inchangée, rien à commiter. Critère de fin d'I1 atteint.
- `Ouvrir-Session` (20261120-234006) : 352 tests (Debug), 2 points d'ATTENTION attendus avant clôture (1 commit à
  publier, sauvegarde à `e66d248`), levés par I1b.
- I1b : prompt de reprise (ÉTAT, §0, §1, §2, §3, §4, §5, §7), historique. Documents seuls.

## 20/11/2026 — I2 : document .docx au modèle FEB

- Session ouverte sur `d70cc7ff17d9` (I1b commité) : 8 pièces reçues, tailles et SHA-256 conformes à `PIECES.txt`,
  archive à 86 entrées comme l'annonce `MASQUAGE.txt` (0 fichier masqué). Critère de fin d'I1b constaté au journal
  (0 commit à publier, sauvegarde à HEAD).
- DEB-Q9 : « ok ». Le modèle `FEB.docx` reçu (272 056 octets) n'a ni signet ni contrôle de contenu ; son corps ne porte
  aucune donnée, mais ses MÉTADONNÉES en portaient : nom réel du dernier modificateur (`core.xml`), chemin d'un `.dotx`
  dans le profil Windows d'un autre agent (`settings.xml.rels`), titre et modèle d'origine. Gabarit versionné nettoyé de
  ces seules parties (270 277 octets, SHA-256 `9A754375…D87B77B4`), corps identique à l'octet. Arbitrages DEB-Q33 à
  DEB-Q37 proposés et appliqués (plan v1.7, §8), à confirmer.
- I2 : Domaine `ContenuDocument` (contenu pur : dates dans le fuseau, culture invariante, dernière décision) ; port
  `IGenerateurDocument` qui reçoit ce contenu ; `ServiceFiches.Telecharger` le construit avec son fuseau ; Infrastructure
  `Documents/` : `PlanFeb` (marqueurs et remplacements, purs), `GabaritFeb` (vérification, remplissage, sérialisation,
  purs), `MetadonneesFeb` (core.xml, pur), `GenerateurDocumentFeb` (adaptateur : gabarit décompressé et vérifié une
  fois ; parties recopiées dans l'ordre, horodatage fixe, image stockée sans recompression) ; `FEB.docx` en ressource
  embarquée ; `*.docx binary` dans `.gitattributes`. 80 tests ESTIMÉS (total 352 → 432 : Domain +18, Application +1,
  Infrastructure +61). Test faible, nommé comme tel : `Outil_exemplaire_pour_Word_…` (n'écrit que si `DEB_I2_SORTIE` est
  posée).
- Vérifié chez l'assistant : syntaxe C# (tree-sitter, contrôle d'abord mis en échec sur un fichier fautif) ; nettoyage du
  gabarit déterministe, recherche des métadonnées personnelles vide sur le gabarit nettoyé et positive (7) sur
  l'original ; placement rejoué par une réplique Python (55 paragraphes, 26 modifiés), document d'essai ouvert et rendu
  par LibreOffice (libellés en gras, retours à la ligne, emoji, case ☒, ligne du chef) ; attendus des tests recalculés
  par cette réplique. Non vérifié : compilation, tests, couverture, sérialisation exacte de `XmlWriter` et de
  `ZipArchive` (méthode « stockée » pour `NoCompression`), ouverture dans Word (poste).
- Mutations prévues (DEB-Q20), sur clone jetable, après le commit d'I2 : M9 (`ContenuDocument` lit la PREMIÈRE décision)
  → 1 échec attendu (Domain) ; M10 (`GabaritFeb` écrit la valeur À LA PLACE du libellé) → 5 échecs ESTIMÉS
  (Infrastructure). Commande, depuis une console Windows PowerShell :

      Set-Location C:\Dev\Demonstrateur; & { $ErrorActionPreference = 'Stop'; $copie = Join-Path $env:TEMP ('deb-mut-I2-' + [guid]::NewGuid().ToString('N')); git clone -q C:\Dev\Demonstrateur $copie; if ($LASTEXITCODE) { throw 'clonage' }; $mutations = @(@{ Nom = 'M9'; Projet = 'ExpressionBesoin\tests\ExpressionBesoin.Domain.Tests\ExpressionBesoin.Domain.Tests.csproj'; Fichier = 'ExpressionBesoin\src\ExpressionBesoin.Domain\Fiches\ContenuDocument.cs'; Avant = 'fiche.Decisions[^1]'; Apres = 'fiche.Decisions[0]' }, @{ Nom = 'M10'; Projet = 'ExpressionBesoin\tests\ExpressionBesoin.Infrastructure.Tests\ExpressionBesoin.Infrastructure.Tests.csproj'; Fichier = 'ExpressionBesoin\src\ExpressionBesoin.Infrastructure\Documents\GabaritFeb.cs'; Avant = 'remplacement.Marqueur.Forme == Forme.Libelle ? 1 : 0'; Apres = '0' }); foreach ($m in $mutations) { $projet = Join-Path $copie $m.Projet; $chemin = Join-Path $copie $m.Fichier; $texte = [IO.File]::ReadAllText($chemin); if (-not $texte.Contains($m.Avant)) { throw ($m.Nom + ' : motif absent') }; [IO.File]::WriteAllText($chemin, $texte.Replace($m.Avant, $m.Apres)); dotnet build $projet -c Debug -v q -nologo | Out-Null; if ($LASTEXITCODE) { throw ($m.Nom + ' : compilation') }; $dll = (dotnet msbuild $projet -getProperty:TargetPath -p:Configuration=Debug | Out-String).Trim(); $sortie = & ([IO.Path]::ChangeExtension($dll, '.exe')) | Out-String -Width 200; $code = $LASTEXITCODE; $total = ($sortie -split "`r?`n") | Where-Object { $_ -match 'Total:' } | Select-Object -Last 1; Write-Host ($m.Nom + ' : code ' + $code + ' ;' + $total); git -C $copie checkout -q -- $m.Fichier }; Remove-Item -Recurse -Force $copie }

  Attendu : `M9 : code 1 ; … Failed: 1 …` puis `M10 : code 1 ; … Failed: 5 …` (5 estimé ; au moins 1).
- Exemplaire pour Word (critère de fin d'I2), après le commit : la commande lance les tests de l'Infrastructure avec
  `DEB_I2_SORTIE` posée, puis ouvre le fichier écrit (fiche fictive EB-2026-0007 validée) :

      Set-Location C:\Dev\Demonstrateur; & { $ErrorActionPreference = 'Stop'; $projet = 'C:\Dev\Demonstrateur\ExpressionBesoin\tests\ExpressionBesoin.Infrastructure.Tests\ExpressionBesoin.Infrastructure.Tests.csproj'; dotnet build $projet -c Debug -v q -nologo | Out-Null; if ($LASTEXITCODE) { throw 'compilation' }; $dll = (dotnet msbuild $projet -getProperty:TargetPath -p:Configuration=Debug | Out-String).Trim(); $fichier = Join-Path $env:TEMP 'EB-2026-0007.docx'; $env:DEB_I2_SORTIE = $fichier; try { & ([IO.Path]::ChangeExtension($dll, '.exe')) | Out-Null; if ($LASTEXITCODE) { throw 'tests en échec' } } finally { Remove-Item Env:DEB_I2_SORTIE }; Invoke-Item $fichier }

  Attendu : Word s'ouvre sans message de réparation, en-tête DGCCRF et logo du modèle, rubriques renseignées, case ☒,
  « Validée » dans la ligne « Chef de service ».
- 21/11/2026 : `Appliquer-Lot I2` : 21 fichiers écrits, compilation Release, 432 tests (Domain 207, Application 66,
  Infrastructure 109, Web 30, Architecture 20) — les comptes ESTIMÉS sont confirmés — dont 1 échec :
  `GabaritFebTests.Octets_echappe_les_caracteres_speciaux_du_texte` ; pas de commit (ligne enchaînée). Cause retenue
  (hypothèse, message d'échec non relevé) : `Assert.DoesNotContain(string, string)` de xUnit compare selon la culture ;
  sous ICU, « \u0001 » est ignorable et se trouve dans tout texte. Défaut du test, pas du code. Lot I2a : I2 complet,
  ce test en comparaison ordinale et sans exigence sur « > » (XML n'impose que « & » et « < »). Leçon générique à verser
  aux règles par un lot du harnais : assertion sur une sous-chaîne → `StringComparison.Ordinal`.

## 21/11/2026 — I2a validé, I2b : clôture du chantier I

- `Appliquer-Lot I2a` (après remise de l'arbre à HEAD, fichiers d'I2 seuls) : 21 fichiers, compilation Release,
  432 tests MESURÉS (Domain 207, Application 66, Infrastructure 109, Web 30, Architecture 20), 0 échec ; commit
  `e28a964`. L'hypothèse d'I2 (comparaison selon la culture) n'est pas démentie : le seul test retouché passe.
- Mutations sur clone jetable : M9 → `Failed: 1` (Domain, attendu 1) ; M10 → `Failed: 5` (Infrastructure, estimé 5).
- `Lancer-Couverture -UpdateBaseline` : lignes 1181/1181, branches 356/356 = 100 %, base inchangée (déjà à 100 %),
  « COUVERTURE CONFORME ».
- Exemplaire EB-2026-0007 (commande de l'entrée I2) : ouvert dans Word sans réparation, conforme au modèle FEB (en-tête,
  logo, case ☒, « Validée » dans la ligne du chef), déclaration du développeur du 21/11/2026.
- DEB-Q33 à DEB-Q37 : « recommandations ok » (21/11/2026) ; plan v1.8. Chantier I clos. I2b : documents seuls.

## 21/11/2026 — F1 : socle du front, composition, API, IA scénarisée

- Ouverture : pièces contrôlées (8, tailles et SHA-256 conformes à `PIECES.txt` ; archive 98 entrées, commit
  `4c6c984e`, aucun masquage). DEB-Q10 tranchée par le développeur à l'inverse de la recommandation : `node_modules`
  versionné (DEB-D6). Principe des versions demandé par le développeur (DEB-D7) : Node.js 26.13 LTS et dernières
  versions stables. DEB-Q38 à DEB-Q44 : « recommandations ok » (21/11/2026). Plan v1.9 (F1 socle, F2 Dialogue, F3 Liste ;
  route `reprendre` ajoutée au §5 : le cas d'usage existait depuis B2, la route manquait au plan).
- Mesures de l'assistant (Windows x64 simulé par `npm ci --os=win32 --cpu=x64`) : `node_modules` de 103 Mo, 2 233
  fichiers, 63 paquets installés ; plus gros fichier 23 Mo (binaire Rolldown) ; 5 dossiers `bin` (dont `vite`, `typescript`)
  ignorés par `[Bb]in/` sans ré-autorisation. npm 10.9 (Node 22) plante sur Vitest 4.1 (`edgesOut`) : sans objet avec
  Node 26 (npm 11.20). Clone Windows (`core.autocrlf=true`) sans `-text` : 1 836 fichiers sur 2 233 réécrits en CRLF ;
  avec : 0. Hypothèse retirée : un « diff fantôme » après `npm ci` (mesuré : aucun, avec ou sans l'attribut).
- Contenu : `ClientApp/` (package.json et lock aux versions de DEB-D7, `.npmrc`, configurations TypeScript 7, Vite 8.3,
  Vitest 5 ; `core/` pur : Url, contrats, client d'API, agent courant, aiguillage ; page accueil ; tests Vitest et JSON de
  référence partagé) ; Web : `<vite-scripts />` (`ViteTags` pur, `LecteurManifeste` en cache sur la date,
  `ViteScriptsTagHelper`), routes des fiches (`PointsFiches`, `Vues`, `StatutHttp`, `ContratJson`), composition
  (`CompositionFiches` : base `Fiches:Base`, schéma et gabarit vérifiés au démarrage, `Guid.CreateVersion7`, fuseau
  Europe/Paris), cibles MSBuild (`NpmInstall` = `npm ci`, `ViteBuild` incrémental, `IncludeViteDist`, `ViteClean`) ;
  Infrastructure : `ScenarioDialogue` (pur) et `AssistantIAScenarise` ; `.gitignore`, `.gitattributes`.
- Vérifié par l'assistant : sous Node 26.13.1, `tsc -b` et `vite build` sans avertissement, 58 tests Vitest, couverture du
  front 100 % (lignes 74/74, branches 57/57) ; mutations MF1 à MF8 (Url ×2, aiguillage, agent courant, contrat ×2, client
  ×2) : chacune fait échouer au moins un test (1 à 3). C# : 24 fichiers sans erreur de syntaxe (tree-sitter, contrôle qui
  échoue sur un cas fautif) ; attendus de `ScenarioDialogueTests` recalculés par une réplique Python ; JSON de référence
  produit par une réplique Python (3 267 octets). Non vérifié : compilation C#, tests .NET (93 ajoutés, ESTIMÉS : 26
  Infrastructure, 67 Web), couverture .NET, cibles MSBuild sous Windows (SDK .NET et nuget.org refusés par la politique
  réseau de l'assistant).
