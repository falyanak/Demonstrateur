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
