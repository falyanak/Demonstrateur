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

## 21/11/2026 — F1 commité, F1b : caches de node_modules ignorés

- `Appliquer-Lot F1` puis commit par la ligne enchaînée : `bfaaf97` (2 312 fichiers ; 2 257 sous `node_modules` = 2 233
  installés + 24 raccourcis Windows de `.bin`, 8 commandes × `.cmd`, `.ps1`, sans extension). Sortie d'`Appliquer-Lot` non
  transmise : comptes mesurés attendus de `Compter-Tests` à la session suivante.
- `Ouvrir-Session` : « SESSION NON PRÉPARÉE », 13 fichiers non suivis, tous sous `node_modules/.vitest-cache/` (cache de
  Vitest 5, écrit par `npm test`). Défaut de F1 : le `.gitignore` n'avait été essayé que sur un `node_modules` fraîchement
  installé, sans y lancer les outils (Vitest n'écrit pas ce cache chez l'assistant).
- F1b : `**/ClientApp/node_modules/.*/` ignoré, `.bin` ré-autorisé. Contrôle sur les noms relevés au poste : ancien
  `.gitignore` → 5 fichiers de cache visibles (`.vitest-cache`, `.vite-temp`, `.cache`), nouveau → 0 ; `.bin` et les `bin`
  de paquets toujours suivis. Leçon pour le harnais (gabarit F3) : ignorer les dossiers en point de `node_modules`, pas une
  liste de caches.

## 21/11/2026 — F1 vérifié et clos, F1c : comptes et clôture

- Ouverture : pièces contrôlées (8, tailles et SHA-256 conformes à `PIECES.txt` ; archive 141 entrées, commit
  `fe31e69de65e`, aucun masquage) ; arbre propre, F1b commité.
- Comptes MESURÉS (`Compter-Tests`, Debug) : Domain 207, Application 66, Infrastructure 135, Web 97, Architecture 20,
  total 525, 0 échec ; égaux aux estimations de F1 (26 Infrastructure et 67 Web ajoutés).
- `npm test` sur le poste (Vitest 5.1.1) : 4 fichiers, 58 tests verts (contrats 23, api 11, agentCourant 13, Url 11) ;
  `git status --short` vide ensuite : F1b tient.
- Parcours sur 5080 (`dotnet run --launch-profile Localhost`) : `api/sante` « ok », version
  `0.1.0+fe31e69de65eaadacaaf703e17530ed8800cfff2` ; fiche créée pour `camille.martin` (Bureau 1A, valideur Dominique
  Bernard), `Brouillon`, 0/10, prochaine `application` ; un tour scénarisé : question sur l'application, proposition en
  attente sur `intitule` (la demande initiale, DEB-Q41), `horsPerimetre` faux, aucun écart. L'attendu annoncé par
  l'assistant (« proposition sur la rubrique questionnée ») était inexact pour le premier tour : retiré.
- `api/agents` affiché « agents : 1 » : défaut de la commande de l'assistant, pas de l'API. Sous Windows PowerShell 5.1,
  `Invoke-RestMethod` rend un tableau JSON comme UN objet, que `@(…)` emballe (annexe des règles, piège de
  `ConvertFrom-Json`). Recompté par la commande jointe à F1c. Leçon versée au §2 du prompt pour le harnais.
- Critère de fin de F1 atteint : objectif atteint, on arrête F1. F1c : documents seulement.

## 21/11/2026 — HY1 : versions NuGet, outils dotnet et SDK (DEB-HY1)

- Ouverture : pièces contrôlées (8, tailles et SHA-256 conformes à `PIECES.txt` ; archive 141 entrées, commit
  `3e4555112f69`, aucun masquage) ; F1c commité ; 525 tests mesurés (`Compter-Tests`), 0 échec. Journal : 1 commit à
  publier, 1 commit non sauvegardé (publication et sauvegarde demandées au développeur).
- Versions relevées sur nuget.org le 21/11/2026 : `xunit.v3` 4.0.3 (09/11), `xunit.runner.visualstudio` 4.0.1 (09/10),
  `Microsoft.NET.Test.Sdk` 18.12.0 (11/11), `Microsoft.AspNetCore.Mvc.Testing` et `Microsoft.Data.Sqlite` 11.0.0 et
  10.0.15 (10/11), dotnet-coverage 18.12.0 (06/10), ReportGenerator 5.6.0 (10/11) ; .NET 10.0.15 (correctif de sécurité
  du 10/11, SDK 10.0.404 et 10.0.115). Sqlite 10.0.15 dépend de SQLitePCLRaw 2.1.13 (appel direct de
  `sqlite3_db_config` inchangé). Surface xUnit des tests : `[Fact]`, `[Theory]`, `TestContext.Current` seulement.
- DEB-Q45, DEB-Q46 : « recommandations ok » ; DEB-Q47 tranchée par le développeur contre la recommandation (SDK
  10.0.404 minimum) : `global.json` bloqué sur 10.0.403, `rollForward: disable`, pour la conformité CI/CD ; .NET 11
  installé sur le poste. Plan v1.10.
- HY1 : `Directory.Packages.props`, `.config/dotnet-tools.json`, `global.json`, plan, prompt, historique. Vérifié par
  l'assistant : JSON et XML bien formés, fins de ligne CRLF conservées, aucun BOM ajouté. Non vérifié : restauration,
  compilation, tests .NET, couverture, format de sortie de xUnit 4 lu par le harnais (SDK .NET et nuget.org refusés au
  shell de l'assistant) : le poste tranche.

## 21/11/2026 — HY1 vérifié et clos, HY1b : clôture et questions de F2

- Ouverture : pièces contrôlées (8, tailles et SHA-256 conformes à `PIECES.txt` ; archive 141 entrées, commit
  `d307a911a46b`, aucun masquage) ; arbre propre, 0 commit à publier, sauvegarde du jour au même HEAD.
- Critère de fin de HY1 atteint : HY1 commité par la ligne enchaînée (donc `Appliquer-Lot HY1` vert : compilation,
  tests, couverture au cliquet 100 / 100) ; `Compter-Tests` (Debug) relit la sortie de xUnit 4 : Domain 207, Application
  66, Infrastructure 135, Web 97, Architecture 20, total 525, 0 échec. Versions à HEAD : `global.json` 10.0.403
  (`rollForward: disable`), `xunit.v3` 4.0.3, runner 4.0.1, Test.Sdk 18.12.0, Mvc.Testing et Sqlite 10.0.15,
  dotnet-coverage 18.12.0, ReportGenerator 5.6.0. Objectif atteint, on arrête HY1.
- Ouverture de F2 : questions DEB-Q48 à DEB-Q53 posées avec recommandations (plan v1.11, questions ouvertes) ; critère de
  fin de F2 précisé (prompt, bloc ÉTAT). HY1b : documents seulement.

## 21/11/2026 — F2 : page Dialogue (DEB-Q48 à DEB-Q53)

- Arbitrages : DEB-Q48 à DEB-Q53 « recommandations ok » ; plan v1.12. F2 bâti après la sortie d'`Appliquer-Lot HY1b`.
- DEB-A1 (anomalie du code de F1, relevée en préparant F2) : `client.fiche(id)` lisait `GET api/fiches/{id}` comme une
  fiche, alors que la route rend `{ fiche, historique }` (DEB-Q21) ; son test donnait une fiche comme réponse du serveur,
  l'écart passait. Inutilisée jusque-là, la méthode est remplacée par `consulter` (lecture `lireConsultation`), avec un
  test de non-régression (une fiche seule est refusée). Close par F2.
- Web : action `Home/Dialogue/{id}` (Guid non vide, sinon 404) et vue `Dialogue.cshtml` ; bouton « Nouvelle fiche » de
  l'accueil. Tests : références `consultation-reference.json` et `tour-reference.json` à l'octet (`FicheVueTests`, 2),
  page Dialogue rendue (1), trois adresses en 404 (théorie, 3) ; total Web estimé 103, solution 531.
- Front : client complet de l'API (routes du §5, corps JSON, `urlDocument`), `erreurException` (réseau, contrat),
  `chemins.ts`, contexte injecté aux pages (`ContextePage` : client, agent courant, navigation) ;
  `features/dialogue/etat.ts` (réducteur pur : une requête à la fois, brouillons gardés et nettoyés, confirmation et
  commentaire remis à zéro au changement de statut) et `vue.ts` (pur : blocs, badges, progression, actions, « Enregistrer
  ») ; `PageDialogue.ts` coquille (une frappe ne reconstruit pas la page) ; exclusion de couverture du front réduite à
  `src/features/**/Page*.ts`. Aucune dépendance ajoutée, `node_modules` inchangé.
- Vérifié chez l'assistant sur une réplique de ClientApp (Node 22, `npm ci` du lock, moteur hors `engines`) : `tsc -b`
  sans erreur ; Vitest 165 tests verts (58 → 165 : contrats 41, api 30, dialogueEtat 21, dialogueVue 49, agentCourant
  13, Url 11) ; couverture du front 153/153 lignes, 123/123 branches ; `vite build` réussi. Mutations sur copie jetable,
  chacune détectée : MF9 (soumission sans confirmation, 1 échec), MF10 (« Proposée » malgré une valeur, 1), MF11
  (brouillon enregistré gardé, 2), MF12 (confirmation gardée au changement de statut, 1), MF13 (commentaire de décision
  omis, 1), MF14 (`horsPerimetre` non vérifié, 1), MF15 (lien du .docx masqué pendant une attente, 1). Références JSON
  produites par réplique Python, compactes, sans BOM.
- Non vérifié chez l'assistant : compilation C#, tests .NET, octets émis par `System.Text.Json` (« », `\n`), rendu
  Razor, parcours dans un navigateur réel : le poste tranche.

## 21/11/2026 — HY2 : dossier de données par environnement (DEB-Q54 à DEB-Q57)

- Ouverture de session : 8 pièces reçues, tailles et SHA-256 conformes à `PIECES.txt`, archive de 150 entrées au commit
  `ba3aafd` ; F2 appliqué et commité (`Compter-Tests` : 531, 0 échec) ; `npm test` et parcours du navigateur de F2 non
  encore transmis.
- Demande du développeur : données par défaut dans `C:\Dev\<nomApplication>-data` sur le poste, dans un dossier du
  serveur à la publication IIS ; puis « il faut simplement pouvoir fixer les chemins du dossier de données pour chaque
  environnement : localhost, staging, production ». Questions DEB-Q54 à DEB-Q57 : « recommandations ok », DEB-Q55
  précisée ainsi (un fichier `appsettings.{environnement}.json` par environnement, au lieu de la seule variable
  d'environnement recommandée) ; plan v1.13.
- Web : `appsettings.Localhost.json` → `C:\Dev\Demonstrateur-data\fiches.db` (révise DEB-Q31) ;
  `appsettings.Staging.json` et `appsettings.Production.json` ajoutés (`D:\Donnees\Demonstrateur-data-staging`,
  `D:\Donnees\Demonstrateur-data`, à confirmer avec l'exploitation) ; message d'arrêt de `CompositionFiches.CheminBase`
  généralisé (fichier de l'environnement ou variable `Fiches__Base`). Aucun changement de comportement : le code lisait
  déjà `Fiches:Base` de la configuration.
- Tests : `ConfigurationEnvironnementsTests` (théorie des trois environnements, chemin exact et absolu ; `appsettings.json`
  sans chemin), 4 tests ; message de `CompositionFichesTests` mis à jour ; total estimé 535. Les tests d'intégration
  restent en `Production` : la source en mémoire de `UsineWeb`, ajoutée en dernier, remplace le chemin du fichier
  (`null` compris pour `UsineSansBase`).
- Vérifié chez l'assistant : valeurs des trois fichiers par une réplique Python, qui échoue sur une copie mutée (chemin
  Staging égal à celui de Production, 1 échec) ; message attendu égal au message émis (réplique) ; octets (CRLF, sans
  BOM, comme les fichiers d'origine). Non vérifié : compilation C#, tests .NET, démarrage du site.

## 21/11/2026 — HY2 et F2 prouvés et clos ; DEB-A2 relevée ; F2c : racine de lot et documents

- Ouverture (20261121-161757) : 8 pièces conformes à `PIECES.txt` ; archive de 153 entrées au commit `9b21bdb` ;
  `Compter-Tests` (Debug) : Domain 207, Application 66, Infrastructure 135, Web 107, Architecture 20, total 535, 0 échec,
  égal à l'estimation de HY2 ; 1 commit à publier, 1 non sauvegardé.
- Preuves du poste : `npm test` 165 tests verts (6 fichiers) ; site au profil `Localhost` : `fiches.db` créé dans
  `C:\Dev\Demonstrateur-data`, `api/sante` « ok », version `0.1.0+9b21bdb…` ; parcours de la page Dialogue joué
  (validation, compléments, refus), page rendue sur 5080 (capture). Publication `ba3aafd..9b21bdb`, sauvegarde des trois
  dépôts « ok ». Objectif atteint, on arrête HY2 et F2.
- Ouverture suivante (20261121-163017) : état conforme, archive de 153 entrées au même commit ; `Compter-Tests` en échec
  de compilation (MSB3027/MSB3021) : DLL de `bin\Debug` verrouillées par le site encore lancé (processus
  `ExpressionBesoin.Web`) et par Visual Studio. Pas une régression ; le site s'arrête avant toute commande qui compile.
- DEB-A2 relevée dans ce journal : les erreurs citent `sdk\11.0.100`, malgré `global.json` (10.0.403,
  `rollForward: disable`, DEB-Q47). Mesure du développeur : `dotnet --version` rend 11.0.100 dans
  `C:\Dev\Demonstrateur` et 10.0.403 dans `ExpressionBesoin` ; SDK installés : 10.0.403 et 11.0.100. Cause : `dotnet`
  cherche `global.json` depuis le dossier courant. Les comptes mesurés depuis HY1 viennent donc du SDK 11 (cible
  `net10.0` inchangée). DEB-Q58 : correction maintenant, avant F3.
- F2c : `global.json` ajouté à `Lot.Racines` (`harnais.config.psd1`), condition de HY3 (`Appliquer-Lot` contrôle les
  chemins avec la configuration chargée avant l'écriture) ; prompt (bloc ÉTAT, comptes mesurés, DEB-A2, DEB-Q58, leçon
  (d) pour le harnais), plan v1.14 (lot HY3, DEB-Q58). Vérifié chez l'assistant : octets (CRLF, BOM du seul `.psd1`
  conservé), taille du prompt sous 30 720 octets. Non vérifié : chargement de la configuration par le harnais.
- Premier envoi sous le nom F2b : 4 fichiers écrits, tests en échec (Web : 1 sur 107,
  `ConfigurationEnvironnementsTests`, cas `Localhost`), commit retenu par `if ($?)`. Cause mesurée (`git diff`) :
  `appsettings.Localhost.json` retouché à la main hors lot (`C:\DEV\demonstrateur-data`, casse changée) ; le garde-fou
  de HY2 a mordu. Arbre remis à HEAD (`git checkout`), contenu relivré sous le nom F2c (règles §3). `global.json` copié
  à la main dans le dossier du profil Windows : sans effet sur `C:\Dev` (recherche depuis le dossier courant), retiré.

## 21/11/2026 — F2c commité ; HY3 : global.json à la racine du dépôt (DEB-A2, DEB-Q58)

- F2c : `git checkout -- .` (arbre remis à HEAD), puis `Appliquer-Lot F2c` : « configuration portée par le lot, valide »,
  4 fichiers écrits, aucune vérification (lot hors solution) ; commit `50ccdb4`, 4 fichiers dont `harnais.config.psd1`.
  Constat : `Appliquer-Lot` valide les chemins d'après la configuration portée par le lot ; le découpage F2c puis HY3
  n'était pas nécessaire (sans dommage). Objectif de F2c atteint.
- HY3 : `global.json` déplacé à la racine du dépôt (octets inchangés : 10.0.403, `rollForward: disable`), ancien retiré
  par `SUPPRESSIONS.txt` ; `.slnx` relié à `../global.json` ; `Directory.Build.props` : `SdkAttendu` et cible
  `VerifierSdk` (`BeforeTargets="BeforeBuild"`, erreur `DEB0001` si `NETCoreSdkVersion` diffère) ; `SolutionTests.Racine`
  rendu `internal`, partagé avec `SdkTests` (3 tests : `global.json` à la racine et bloquant, aucun dans la solution,
  version égale à la garde). Total estimé 538 (Architecture 23).
- Vérifié chez l'assistant : réplique Python des trois tests sur les fichiers du lot (verte), puis sur trois copies
  mutées, chacune en échec : `SdkAttendu` 10.0.404, `global.json` laissé dans la solution, `rollForward` `latestFeature` ;
  XML et JSON bien formés ; octets (CRLF, BOM du seul fichier qui en portait). Non vérifié : compilation C#, tests .NET,
  évaluation de la garde par MSBuild (le poste tranche : `dotnet --version` à la racine, build forcé en échec).

## 21/11/2026 — HY3 vérifié et clos ; F3 : page Liste (DEB-Q59 à DEB-Q63)

- Ouverture (20261121-164746) : 8 pièces conformes à `PIECES.txt` (tailles et SHA-256) ; archive de 154 entrées au
  commit `fcdb37a` (HY3 commité), conforme à `MASQUAGE.txt` ; état conforme, 0 commit à publier, sauvegarde du jour.
  `Compter-Tests` (Debug) : Domain 207, Application 66, Infrastructure 135, Web 107, Architecture 23, total 538,
  0 échec, égal à l'estimation de HY3.
- Preuves du poste pour HY3 : `dotnet --version` rend 10.0.403 à la racine du dépôt ;
  `dotnet build … -p:SdkAttendu=0.0.0` échoue par `DEB0001` (« SDK 10.0.403 utilisé, 0.0.0 attendu »), code 1. DEB-A2
  close. Objectif de HY3 atteint, on arrête.
- DEB-Q59 à DEB-Q63 posées puis tranchées (« recommandations ok ») : liste à l'accueil ; action « Ouvrir » (« Reprendre »
  désignait déjà la transition `ACompleter → Brouillon`) ; découpage pur / coquille ; date à Paris ; toutes les fiches.
- F3 : `contrats.ts` (`ResumeVue`, `lireResumes`), `api.ts` (`lister`), `aiguillage.ts` (`adresse` du contexte, pour
  les liens), `core/statuts.ts` (badge du statut, déplacé de `features/dialogue/vue.ts`, qui le réexporte),
  `features/liste/vue.ts` (pur : `dateCourte`, `lignes`), `PageListe.ts` (coquille : tableau en `textContent`, dernière
  demande seule affichée, rechargement sur `pageshow` persistant, « Nouvelle fiche » reprise de l'accueil), `main.ts`,
  `Views/Home/Index.cshtml` (`data-page="liste"`, zone `#liste`) ; `features/accueil/PageAccueil.ts` retiré
  (`SUPPRESSIONS.txt`). Tests : `listeVue.test.ts`, `contrats.test.ts` et `api.test.ts` complétés ;
  `liste-reference.json` figé par `FicheVueTests` (nouveau test, Web 108) ; `PointsFichesTests` : page d'accueil en
  `data-page="liste"`. Les manifestes d'essai de `ViteTagsTests` citent encore `PageAccueil.ts` : données de test,
  sans lien avec un fichier réel, laissées.
- Vérifié chez l'assistant (réplique du ClientApp, `npm ci` sur le lock du dépôt) : `tsc -b`, `vite build`, Vitest
  200 tests verts (165 + 35), couverture du front 100 % (167/167 lignes, 131/131 branches, coquilles exclues) ; une
  branche inatteignable de `dateCourte` retirée plutôt qu'exclue ; dates attendues recalculées par Python
  (`zoneinfo`) ; mutations MF16 (Télécharger hors validée), MF17 (fuseau ignoré), MF18 (numéro absent), MF19 (route
  de la liste), MF20 (statut non vérifié), MF21 (libellé du badge) : chacune fait échouer 1 à 4 tests. Octets : CRLF
  sans BOM comme les originaux, référence JSON compacte sans fin de ligne. Non vérifié : compilation C# et tests .NET
  (`FicheVueTests` tranche la réplique Python de la sérialisation), coquille `PageListe.ts` dans un navigateur.

## 21/11/2026 — F3 vérifié et clos ; F3b : DEB-Q2 et DEB-Q64

- Ouvertures (20261121-170915, 171214, 172639) : 8 pièces conformes à `PIECES.txt` à chaque fois (tailles et
  SHA-256) ; archives de 158 entrées au commit `ce06fe6` (F3 commité), conformes à `MASQUAGE.txt` ; 1 commit à publier
  et 1 commit non sauvegardé signalés. `Compter-Tests` (Debug) : Domain 207, Application 66, Infrastructure 135,
  Web 108, Architecture 23, total 539, 0 échec, égal à l'estimation de F3.
- Preuves du poste pour F3 : `npm test` 200/200 (7 fichiers, Vitest 5.1.1) ; capture de la liste (EB-2026-0004 à
  0007 : Validée avec « Télécharger », Refusée sans ; plus récente en tête ; date du 21/11/2026) ; statuts
  intermédiaires (Brouillon, Soumise, À compléter, reprise) déclarés conformes par le développeur. Objectif de F3
  atteint, chantier F clos.
- Un « recommandations ok » reçu à l'ouverture, sans question posée dans la session : non appliqué, question renvoyée au
  développeur (DEB-Q2 ou questions d'une conversation antérieure non versionnées ?) ; puis « recommandations ok » sur
  les deux points posés : DEB-Q2 tranchée (Albert API si accès, sinon Mistral ; accès à confirmer à l'ouverture d'A1),
  DEB-Q64 (observation du parcours : demandes d'assistance validées, limite de l'IA scénarisée, détection reportée au
  critère d'A1, aucune correction).
- F3b : documents seuls (plan v1.16, prompt de reprise, historique). Vérifié chez l'assistant : octets (CRLF sans BOM
  comme les originaux), taille du prompt < 30 720 octets. Rien à compiler.

## 21/11/2026 — Chantier L ouvert ; L1 livré (cycle de vie, serveur)

- Ouverture (20261121-173256) : 8 pièces conformes à `PIECES.txt` (tailles et SHA-256), archive de 158 entrées au
  commit `9397dbe`, conforme à `MASQUAGE.txt` ; 2 commits à publier et 2 non sauvegardés signalés ; `Compter-Tests` :
  539, 0 échec.
- Demande du développeur : suivre les états d'une demande. Proposition d'un journal Serilog (DEB-Q65 à DEB-Q72,
  « recommandations ok »), puis alternative du développeur : une table des changements d'états. Recommandation retenue
  (« accord ») : table métier `Transitions` (DEB-Q73 à DEB-Q78), Serilog en évolution V2 ; DEB-Q67 à DEB-Q72 sans objet.
  Chantier L ouvert avant A1 (DEB-Q65), en deux lots : L1 serveur, L2 front.
- L1 : `Transitions` du Domaine (`Entre`, `EtapeDe`, record `Transition`, énumération `Etape`) ; port `IDepotFiches` :
  `LireTransitions`, `Enregistrer(fiche, échanges, transition?, …)` ; `FicheConsultee.Transitions` ; `ServiceFiches` :
  une lecture d'horloge par cas d'usage qui date fiche et transition ; `SchemaFiches` : schéma 2 (`TablesSchema1`
  inchangé, `TableTransitions`, `Creation`, `Migration1Vers2`, `Planifier` pur remplace `DoitCreer`) ;
  `DepotFichesSqlite` : migration, lecture et ajout dans la transaction de la fiche ; `ConsultationVue.Cycle`
  (`TransitionVue`), référence `consultation-reference.json` étendue en fin de document.
- Défaut trouvé AVANT livraison par la réplique sqlite3 : la contrainte écrite `Avant = 0 AND …` admettait une création
  suivie de n'importe quel statut (avec `Avant` NULL, la comparaison vaut NULL et un CHECK à NULL est satisfait : 10
  paires admises au lieu de 6) ; corrigée par `IS` ; le test `La_contrainte_du_schema_admet_exactement_les_paires_du_Domaine`
  compare la contrainte au Domaine sur les 30 paires.
- Vérifié chez l'assistant : SQL extrait du source et rejoué (création, migration 1 → 2 avec structure identique à une
  base neuve, 6 paires sur 30, rangs, plans de requête par clé primaire sans tri, cascade) ; tables du schéma 1
  identiques au texte d'I1 ; octets des BLOB et ticks recalculés en Python ; Vitest 200/200 et `tsc -b` sur une
  réplique (`npm ci`, Node 22 au lieu de 26) avec la référence étendue ; syntaxe C# par tree-sitter (0 erreur sur les
  fichiers du lot, contrôle éprouvé sur un cas fautif) ; équilibre des accolades ; CRLF sans BOM comme les originaux.
  Non vérifié : compilation C#, tests .NET (comptes ESTIMÉS : 568), couverture, mutations C# (aucun SDK).

## 21/11/2026 — L1 vérifié et clos ; L1b : comptes mesurés

- Ouverture (20261121-175422) : 8 pièces conformes à `PIECES.txt` (tailles et SHA-256), archive de 160 entrées au
  commit `b275507` (L1 commité), conforme à `MASQUAGE.txt` ; 3 commits à publier et 3 non sauvegardés signalés.
  `Compter-Tests` (Debug) : Domain 229, Application 66, Infrastructure 142, Web 108, Architecture 23, total 568,
  0 échec, égal à l'estimation de L1.
- Preuve du parcours : script hors dépôt `Jouer-ParcoursL1.ps1` (livré dans la session, par l'API, site au profil
  `Localhost`, `api/sante` version `0.1.0+b275507…`) : parcours A (création, soumission, compléments, reprise,
  nouvelle soumission, validation) : six transitions dans l'ordre, demandeur « Camille Martin » pour création,
  soumissions et reprise, valideur « Dominique Bernard » pour les décisions, chaque `avant` égal à l'`apres` précédent ;
  parcours B : création, soumission, « Refus ». Base du poste (schéma 1 depuis I1) servie après démarrage : migration
  au schéma 2 faite (un échec arrête le démarrage, DEB-Q76).
- Non reçu : la sortie d'`Appliquer-Lot L1` ; la couverture conforme se déduit du commit par la ligne enchaînée
  (`Appliquer-Lot` lève sur un cliquet non conforme). Objectif de L1 atteint.
- L1b : documents seuls (prompt de reprise, historique). Vérifié chez l'assistant : octets (CRLF sans BOM comme les
  originaux), taille du prompt < 30 720 octets. Rien à compiler.

## 21/11/2026 — L2 livré (cycle de vie, front)

- Ouverture (20261121-180238) : 8 pièces conformes à `PIECES.txt` (tailles et SHA-256), archive de 160 entrées au
  commit `641bf30` (L1b commité), conforme à `MASQUAGE.txt` ; 4 commits à publier et 4 non sauvegardés signalés ;
  `Compter-Tests` : 568, 0 échec.
- Constat : `lireConsultation` ignorait le champ `cycle` livré par L1 ; soumission, décision et reprise ne renvoient
  qu'une fiche, sans cycle. Questions DEB-Q79 à DEB-Q82 posées avec recommandation, « recommandations ok ».
- L2 : `core/contrats.ts` (`Etape`, `TransitionVue`, `cycle` exigé) ; `core/dates.ts` (`dateCourte` et `FUSEAU`
  déplacés de `features/liste/vue.ts`, qui les réexporte ; `dateHeure`) ; `features/dialogue/etat.ts` (`cycle` dans
  l'état, événements `relue` et `nonRelue`, `apresTransition` pur) ; `features/dialogue/vue.ts` (`lignesCycle`,
  `libelleEtape`, `CYCLE_VIDE`) ; `PageDialogue.ts` (`transitionner` : relecture après les trois transitions ; panneau
  sous le circuit, `textContent`). Tests : `dates.test.ts` (nouveau, tests de `dateCourte` déplacés de
  `listeVue.test.ts`), `contrats`, `dialogueEtat`, `dialogueVue`, `listeVue`. Contrat serveur et fichiers .NET inchangés.
- Vérifié chez l'assistant (réplique de ClientApp, `npm ci` aux versions exactes, Node 22 au lieu de 26) : `tsc -b`,
  `vite build`, Vitest 249/249 (200 avant), couverture du front 100 % (181/181 lignes, 145/145 branches) ; attendus de
  date et heure recalculés en Python (zoneinfo, passages à l'heure d'été et d'hiver compris) ; mutations MF22 à MF30
  sur une copie jetable : 9 détectées sur 9 (1 à 2 échecs chacune) ; aucun `innerHTML` ; CRLF sans BOM comme les
  originaux. Non vérifié : la coquille au navigateur (parcours de L1 à jouer au poste), Node 26.

## 21/11/2026 — L2 vérifié et clos ; chantier L clos ; L2b : comptes mesurés

- `Appliquer-Lot L2` : 14 fichiers écrits et relus à l'octet près, compilation Release, 568 tests .NET, 0 échec ;
  commit `ec192e6` par la ligne enchaînée (cliquet de couverture conforme). `npm test` : 8 fichiers, 249 tests, 0 échec,
  égal à l'estimation.
- Navigateur (capture du développeur) : panneau « Cycle de vie » d'une fiche : Création (— → Brouillon), Soumission,
  Demande de compléments, Reprise, Soumission, Refus ; demandeur « Louis Petit », valideur « Dominique Bernard » ;
  heures de Paris de 19:39 à 19:45. Étape « Validation » non vue (parcours terminé par un refus) : couverte par Vitest,
  à voir en R1. Objectif de L2 atteint ; chantier L clos.
- L2b : documents seuls (prompt de reprise, historique). Vérifié chez l'assistant : octets (CRLF sans BOM comme les
  originaux), taille du prompt < 30 720 octets. Rien à compiler.

## 21/11/2026 — Chantier A ouvert ; accès à Albert API sondé ; A1 livré (IA réelle)

- Ouverture (20261121-195412) : 8 pièces conformes à `PIECES.txt` (tailles et SHA-256), archive de 162 entrées au
  commit `f44ece5`, conforme à `MASQUAGE.txt` ; 0 commit à publier. `Compter-Tests` en échec à l'ouverture (MSB3027 :
  DLL de `bin\Debug` verrouillées par le site et Visual Studio, cas prévu au §0 du prompt), puis relancé site arrêté :
  568, 0 échec.
- DEB-Q83 : accès à Albert API confirmé par le développeur ; adresse, clé (`ALBERT_API_KEY`), proxy et quotas relevés
  dans CSAgent (cité par FlowPortal). Sonde hors dépôt `Sonder-Albert.ps1` (lecture seule, livrée hors zip) jouée au
  poste le 21/11/2026 à 20:06 : `GET /models` en 103 ms ; schéma strict respecté 4 fois sur 4 ; `openweight-medium`
  0,8 s et 1,6 s sans invention ; `openweight-large` 35,4 s et sept rubriques tirées d'une phrase. DEB-Q84 à DEB-Q88
  posées avec recommandation, « recommandations ok ».
- A1 : paquets `Microsoft.Agents.AI` 1.28.0 et `Microsoft.Extensions.AI.OpenAI` 10.11.1 (retenue en retrait de la 11.0.1,
  série de MAF) ; Infrastructure `IA/` : `PromptFiche` et `LimiteDebit` (purs), `GestionnaireLimiteDebit` (un seul
  nouvel essai sur 429 par minute), `OptionsIA`, `AssistantIAAlbert` (`ChatClientAgent` sans outils ni session, client
  OpenAI sur `HttpClient`, proxy du système, 60 s) ; Web : `CompositionIA` (choix par `IA:Fournisseur`, arrêt au
  démarrage si incomplet), `CompositionFiches` (IA choisie et vérifiée au démarrage), `appsettings.{Localhost,Staging,
  Production}.json` (section `IA`). Tests : `PromptFicheTests`, `LimiteDebitTests`, `GestionnaireLimiteDebitTests`,
  `AssistantIAAlbertTests` (bout en bout sur `FauxFournisseurIA`), `CompositionIATests`, `ConfigurationEnvironnementsTests`
  étendu ; `UsineWeb` fixe l'IA scénarisée et une clé vide (la clé du poste n'entre jamais dans un test). Comptes
  ESTIMÉS : Infrastructure 142 → 190, Web 108 → 133, total 641.
- Vérifié chez l'assistant : syntaxe C# (tree-sitter) des 15 fichiers C# ; attendus du prompt et du schéma recalculés par
  une réplique Python (éprouvée sur un attendu faussé) ; API publiques relues dans les sources de Microsoft.Extensions.AI
  10.11.1 (clé `strict`, instructions en message système), OpenAI 2.15.0 et System.ClientModel 1.16.0, et dans la
  documentation de MAF (1.13, dépôt source inaccessible) ; JSON des `appsettings` ; octets (CRLF sans BOM comme les
  originaux) ; taille du prompt < 30 720 octets. Non vérifié : compilation, restauration NuGet, tests, couverture,
  mutations du back-end, IA réelle : `Appliquer-Lot A1` et le critère au poste tranchent.

## 21/11/2026 — A1 appliqué ; périmètre élargi au service informatique (DEB-Q89, DEB-Q90) ; A2 livré

- `Appliquer-Lot A1` : 23 fichiers écrits et relus à l'octet près, compilation Release, 641 tests .NET, 0 échec
  (Infrastructure 190, Web 133 : égal à l'estimation) ; commit `27ec900` par la ligne enchaînée (cliquet de couverture
  conforme). Clé `ALBERT_API_KEY` posée en variable utilisateur par saisie masquée.
- Navigateur (capture du développeur), IA réelle : « demande de prêt d'un ordinateur portable » signalée hors périmètre,
  mais avec une question sur l'application (réponse contradictoire, écart signalé sous le nom provisoire DEB-A3, non
  numéroté) ; « assistance utilisateur » hors périmètre, orientée vers le support. Rien proposé à droite (DEB-Q23).
- Le développeur précise l'objet : tout besoin que le service informatique peut traiter (assistance utilisateur,
  exploitation des serveurs, réseau, applications, développement applicatif) ; l'IA doit repérer dans la demande le
  service, la tâche ou la prestation. DEB-Q89 et DEB-Q90 posées, « recommandations ok » ; DEB-A3 sans objet.
- A2 : `PromptFiche` (objet, services du service informatique, règle 3 : prestation nommée au premier message sans
  devenir une valeur ; règle 5 : élément concerné dans `application` ; règle 9 : hors périmètre = demande non
  informatique, sans question) ; `ScenarioDialogue` (liste de mots, `EstHorsPerimetre`, `SansAccents`,
  `MessageHorsPerimetre` retirés ; question de la rubrique `application` élargie) ; `PageDialogue.ts` (libellé « Demande
  hors du périmètre du service informatique. ») ; tests : `PromptFicheTests`, `ScenarioDialogueTests` (11 cas retirés,
  5 ajoutés), `PointsFichesTests` (une panne traitée comme toute demande). Comptes ESTIMÉS : Infrastructure 190 → 184,
  total 635 ; Vitest inchangé (249).
- Vérifié chez l'assistant : syntaxe C# (tree-sitter) ; attendus des consignes recalculés depuis les fragments du code
  (réplique Python) ; octets (CRLF sans BOM) ; taille du prompt < 30 720 octets. Non vérifié : compilation, tests,
  couverture, `tsc` et `vite build` (un libellé changé dans une coquille), réponses de l'IA réelle.

## 21/11/2026 — A2 appliqué ; anomalie DEB-A3 ; A3 livré (questionnement de l'IA réelle)

- `Appliquer-Lot A2` : 9 fichiers écrits et relus à l'octet près, compilation Release, 635 tests .NET, 0 échec
  (Infrastructure 184 : égal à l'estimation) ; commit `4afcbfe` par la ligne enchaînée.
- Navigateur, IA réelle, fiche EB-2026-0012 (« mon smartphone est en panne », exportée par `GET api/fiches/{id}`) :
  devenue une fiche (DEB-Q89), prestation nommée au premier message (« Je comprends une demande d'assistance
  utilisateur : panne de smartphone. », mais sans question), valeurs tirées des mots de l'agent, soumise et validée.
  Écart : après « pouvoir appeler mes collègues », l'IA a reposé la question des bénéfices en la reformulant.
- DEB-A3 : le dernier message à l'IA annonçait « Dernière question posée sur : X », X déduit comme la première rubrique
  obligatoire vide et sans proposition ; juste pour l'IA scénarisée (elle propose toujours pour la rubrique
  questionnée), faux dès que l'IA réelle n'a rien proposé pour la rubrique questionnée (ici, vraisemblablement, le
  processus métier) : la « suivante » désignait alors une rubrique à laquelle l'agent venait de répondre. L'export ne
  donne que l'état final : cause établie par le code et compatible avec l'historique, non observée tour par tour.
  « Maintenant » (accord du développeur).
- A3 : `PromptFiche.AQuestionner` (pur : obligatoires vides et sans proposition, ordre de questionnement) remplace
  `Questionnement` ; le dernier message porte « Rubriques à questionner, dans l'ordre : … » ; règles 3 (prestation
  nommée, puis question), 4 (réponse brève proposée telle quelle, précision manquante dans `pointsAClarifier`), 6
  (première rubrique de la liste non encore répondue ; jamais de question reposée), 10 (liste vide). Tests :
  `PromptFicheTests` (non-régression DEB-A3 sur le cas observé ; `AQuestionner`). Comptes ESTIMÉS : Infrastructure
  184 → 185, total 636.
- Vérifié chez l'assistant : syntaxe C# (tree-sitter) ; attendus recalculés par réplique Python ; octets ; prompt
  < 30 720 octets. Non vérifié : compilation, tests, réponses de l'IA réelle.

## 21/11/2026 — A3 appliqué, publié, sauvegardé ; anomalie DEB-A4 ; A4 livré (consignes par rubrique)

- `Appliquer-Lot A3` appliqué et commité (HEAD `e84710c`) ; `Compter-Tests` à l'ouverture : 636 tests, 0 échec
  (Infrastructure 185 : égal à l'estimation). Session contrôlée : 8 pièces conformes à `PIECES.txt`, archive de 174
  entrées, commit `e84710c`. Publié (`f44ece5..e84710c`) et sauvegardé (`Sauvegarder-Depots`, 3 dépôts) par le
  développeur.
- Navigateur, IA réelle, fiche `01a1ffc7-…` (« mon pc ne s'allume plus », exportée par `GET api/fiches/{id}`) :
  tour 1, prestation nommée, mais la panne n'est proposée pour aucune rubrique (question sur le modèle du PC) ; tour 2,
  après « dell x25 », question incohérente « Quel est votre poste de travail ? » ; tour 3, après « je ne comprends pas
  la question », exemple portant des faits inventés (« depuis ce matin, après une mise à jour logicielle ») ; tour 4,
  après la description de la panne, « Quelles difficultés rencontrez-vous… ? », vécue comme « quel est le problème ? »
  une seconde fois.
- DEB-A4 : les consignes ne donnaient que « code : libellé » ; le modèle forgeait ses questions sur le libellé seul ; la
  règle 5 ne faisait proposer que l'intitulé et l'application au premier message ; pour une panne, situation actuelle
  et difficultés se recouvrent si « difficultés » n'est pas défini comme les conséquences sur le travail. L'export ne
  donne pas les propositions de chaque tour : lecture du tour 2 probable, non certaine.
- DEB-Q91 (maintenant), DEB-Q92 (rôles et questions types dans `PromptFiche`, pas dans le Domaine), DEB-Q93 (V1 : dix
  rubriques obligatoires pour une panne aussi), DEB-Q94 (prompt d'abord, modèle inchangé) : « recommandations ok ».
- A4 : `PromptFiche.Guide` (pur : rôle et question type de chaque rubrique, rubrique inconnue → exception) ; liste des
  rubriques des consignes « code : libellé (obligatoire) — rôle. Question type : « … » » ; règles 3 (puis règles 5 et
  6), 5 (toute rubrique renseignée par la demande ; panne → `situationActuelle`), 6 (question type adaptée ; jamais une
  information déjà donnée), 11 (reformulation sans fait inventé). Tests : `PromptFicheTests` (attendus des consignes ;
  non-régression DEB-A4 ; guide complet ; rubrique inconnue). Comptes ESTIMÉS : Infrastructure 185 → 188, total 639.
  Prompt de reprise condensé (≤ 30 720 octets).
- Vérifié chez l'assistant : syntaxe C# (tree-sitter), équilibre des accolades ; consignes reconstruites depuis les
  fragments du code par une réplique Python, 20 assertions des tests rejouées, contrôle éprouvé sur une mutation
  (2 échecs) ; octets (CRLF sans BOM). Non vérifié : compilation, tests, couverture, réponses de l'IA réelle.

## 21/11/2026 — Choix du modèle par mesure (sondes) ; A5 et A6 planifiés ; A4b (clôture)

- A4 appliqué au poste (question type visible au navigateur) ; commit et comptes à confirmer par `Ouvrir-Session`
  (estimés : 639 tests, Infrastructure 188).
- Développeur : « le modèle n'est pas assez évolué ». Sondes hors dépôt (PowerShell, clé jamais affichée), même
  deuxième tour de « mon pc ne s'allume plus », consignes d'A4 :
  - `Sonder-Tour2` : gpt-oss (`openweight-large`) rend `content: null` 3 fois sur 3 (~103 jetons générés, `stop`) ;
  - `Sonder-Variantes` : vide dès qu'une réponse de l'assistant est dans l'historique avec un format imposé (schéma,
    `json_object`, raisonnement `low`) ; contrat respecté sans historique (23 à 31 s) ou sans format (20 s) ;
    quotas : 10 requêtes et 128 000 jetons par minute ;
  - `Sonder-Variantes2` : sans historique, raisonnement `low` : 5,6 à 7,9 s, mais sept rubriques remplies d'une seule
    phrase (invention) ; `openweight-medium` sans historique invente aussi, avec historique il est juste ;
  - `Sonder-Consignes` : consignes resserrées sans effet (12 essais) ; tour 2 juste 6/6 ; tour 1 : propositions justes,
    question sur la rubrique tout juste proposée 6/6 ;
  - `Sonder-Tour1` : une précision au premier message sans effet (6/6) ;
  - `Sonder-Modeles` (42 réponses) : question juste au premier tour mistral-small 1/10, gemma-4-31b 4/10,
    deepseek-v4-flash 10/10 (27,3 s en moyenne, raisonnement explicite qui retire les rubriques proposées) ;
    catalogue d'Albert : mistral-small retiré le 01/12/2026.
- DEB-Q95 à DEB-Q99 (mesures), DEB-Q100 (« option a » : question déterministe), DEB-Q101 à DEB-Q104 (« recommandations
  ok »), DEB-D8 (bascule de fournisseur par configuration, demande du développeur), DEB-Q105 à DEB-Q107 posées.
  Leçon : la mécanique de questionnement relève du déterministe ; mesurer un modèle sur la requête exacte de
  l'adaptateur, cas du critère compris, avant tout lot.
- A4b : documents seuls (plan 1.23, historique, prompt de reprise), appliqué seulement si A4 est commité.

## 21/11/2026 — A5 : question déterministe (DEB-Q100 à DEB-Q104)

- Ouverture : 8 pièces reçues, empreintes conformes à `PIECES.txt` ; archive de 174 entrées (= `MASQUAGE.txt`, rien
  masqué), commit `345a707` = HEAD. A4 commité : `Compter-Tests` 639 (Domain 229, Application 66, Infrastructure 188,
  Web 133, Architecture 23), conforme à l'estimation ; 2 commits à publier, sauvegarde provisoire.
- Domaine : `DefinitionRubrique.QuestionType` (textes d'A4, repris à l'identique ; `null` pour le résumé et les points
  à clarifier) ; `Dialogue.Relance` (question type de la prochaine rubrique, `RelanceResume`, `RelanceFin`, DEB-Q102),
  `Dialogue.SansQuestion` (garde-fou DEB-Q103 : phrases dont la ponctuation finale contient « ? », fermants compris ;
  « 2.0 » ne coupe pas ; un saut de ligne finit une phrase), `Dialogue.Composer` (message sans questions + relance
  calculée APRÈS la fusion ; hors périmètre sans relance ; repli DEB-Q108 posée), `Dialogue.RubriqueQuestionnee`
  (rubrique dont la question type termine le dernier message de l'application).
- Application : `ConclureTour` compose le message ; c'est lui qui est enregistré et rendu (l'IA voit dans l'historique
  la question réellement posée). Contrat de l'API, front, base, .docx inchangés.
- Infrastructure : consignes sans question (règle 6 : « Ne pose aucune question… » ; règle 10 : résumé quand la
  rubrique questionnée est resume ; règle 11 : expliquer sans question) ; `Guide` réduit au rôle ; dernier message :
  « Rubrique questionnée : » lue dans l'historique (exacte même après une saisie manuelle entre deux tours, leçon de
  DEB-A3), à défaut la prochaine rubrique ; `AQuestionner` retiré. IA scénarisée sans question
  (`MessageIntitule`, `MessageReprise`, `MessageRien`) ; `Question`, `Suivante`, `MessageFin` retirés.
- Web : `IA:Modele = gemma-4-31b-it` dans Localhost, Staging, Production (DEB-Q104).
- Tests : `DialogueTests` (nouveau : relances, composition, rubrique questionnée, 20 cas du garde-fou), `CatalogueTests`
  (questions types), `DecisionsFichesTests` (question de l'IA sur la rubrique tout juste proposée remplacée),
  `ServiceFichesTests`, `PromptFicheTests`, `ScenarioDialogueTests`, `PointsFichesTests`,
  `ConfigurationEnvironnementsTests`. Comptes ESTIMÉS : Domain 281, Application 67, Infrastructure 189, Web 133,
  Architecture 23, total 693.
- Vérifié chez l'assistant : syntaxe C# (tree-sitter, contrôle éprouvé sur un fichier cassé) ; garde-fou rejoué par une
  réplique Python (attendus des 20 cas) ; consignes reconstruites depuis les fragments du code, 25 assertions rejouées,
  contre-épreuve sur une assertion faussée (1 échec) ; dernier message du tour et rubrique questionnée rejoués ;
  octets (CRLF sans BOM) ; prompt de reprise ≤ 30 720 octets. Non vérifié : compilation, tests, couverture (branches du
  garde-fou visées par les cas), mutations, réponses de gemma.

## 21-22/11/2026 — A5 appliqué ; anomalie DEB-A5 ; finalité helpdesk ; A5b

- A5 appliqué et commité (`bf75669`, ligne enchaînée) ; au navigateur, 1er tour conforme (message sans question, question
  type de l'application) ; 2e tour : « La réponse de l'IA n'est pas un JSON valide : The input does not contain any JSON
  tokens » (422), deux fois, sur deux fiches : DEB-A5 (contenu vide rendu par Albert, lu comme un refus).
- Sondes hors dépôt, requête de l'adaptateur reconstituée depuis `GET api/fiches/{id}` : `Sonder-A5` (texte, historique
  à plat, sans format : 0 vide sur 15 ; sans format, JSON entouré d'une clôture, que `ReponseIA.Lire` tolère) ;
  `Sonder-A5b` (contenus en tableau de parties, tous ou assistant seul : 0 vide sur 20). Réponses de gemma justes :
  « difficultés » proposée avec les mots de l'agent, aucune question, aucune invention (« J'ai complété… » au lieu de
  « je propose » : noté pour R1). `Relais-A5` (relais local) : aucune requête capturée, site non branché sur lui ; puis
  503 au 2e tour. Conclusion : la requête réelle diffère des requêtes reconstituées.
- Développeur : prompt « agent de qualification IT (Helpdesk N1) » (finalité du POC) ; DEB-Q109 à DEB-Q111 posées.
- FlowPortal (`artydev/FlowPortal`) délègue à CSAgent ; `CSAgent/Core/Llm/LlmClient.cs` : `HttpClient` brut, corps
  `model`, `temperature`, `messages`, sans `response_format`, messages NETTOYÉS avant envoi (clés permises, aucune
  valeur nulle, `tool_calls` vide retiré, `content` en chaîne) parce que vLLM derrière Albert plante sur des champs
  renvoyés par le serveur ou nuls. DEB-Q112 à DEB-Q115 : « recommandations ok ».
- A5b : `CorpsRequete` (pur : `Nettoyer`, `ContenuVide`, `Diagnostic`), `GestionnaireCorpsRequete` (maillon en tête,
  avant celui du 429), `AssistantIAAlbert` (journal, au plus 2 appels si la réponse est vide, puis exception),
  `CompositionIA.Creer(configuration, journal)`. Tests : `CorpsRequeteTests` (29), `GestionnaireCorpsRequeteTests` (1),
  `AssistantIAAlbertTests` (+3 : messages envoyés `role`/`content` en chaîne, réponse vide redemandée et journalisée,
  deux vides → panne). Comptes ESTIMÉS : Infrastructure 222, total 726.
- Vérifié chez l'assistant : syntaxe C# (tree-sitter) ; attendus du nettoyage recalculés par une réplique Python ;
  octets (CRLF sans BOM) ; prompt ≤ 30 720 octets. Non vérifié : compilation (dont la référence transitive à
  `Microsoft.Extensions.Logging.Abstractions` dans l'Infrastructure), tests, couverture, effet sur Albert.

## 22/11/2026 — A5b appliqué ; DEB-A5 corrigée ; DEB-Q116 ; clôture A5c

- A5b appliqué et commité (`c30a13b`). Site lancé par `dotnet run`, sortie enregistrée (`site-A5b.txt`) : 7 tours de
  dialogue en 200 (2,2 à 2,8 s ; deux à 19,7 et 20,4 s, non expliqués : attente du 429 ou lenteur d'Albert, noté pour
  R1), aucune ligne « Réponse vide de l'IA (DEB-A5) » : DEB-A5 corrigée par le nettoyage du corps (DEB-Q113), sans
  nouvel essai. Cause précise non capturée (champ du message produit par le client OpenAI .NET, probable).
- Constat du développeur : « pas de blocage, c'est mieux, mais les questions posées indiquent qu'il n'y a pas de réelle
  compréhension » : questions types fixes, sans égard au contexte (panne de PC : résultat recherché, critères de
  réussite, bénéfices attendus évidents ou déjà dits). DEB-Q116 posée (recommandation : question contextualisée).
- A5c : clôture de session (documents seuls) ; plan v1.26.
