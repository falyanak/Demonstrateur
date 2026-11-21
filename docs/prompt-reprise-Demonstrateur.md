# Prompt de reprise — Demonstrateur

> Mis à jour le 21/11/2026 par le lot I2b. Règles génériques : harnais central, `regles\regles-de-travail.md` (v5) ;
> modèle de projet : `regles\harnais-modele-generique.md` (v2) ; front : `regles\gabarit-frontend-vite-aspnet.md` ;
> outillage : `C:\Dev\Harnais\docs\specification-harnais-central.md` (fait foi).
> Historique détaillé : `docs/historique-Demonstrateur.md` (non envoyé par défaut). Documents hors dépôt dont dépend ce
> prompt (DEB-D2, tenu par le développeur, joint aux sessions qui en ont besoin) : cahier des charges
> `Expression_de_besoin_demonstrateur_IA_SICCRF.docx` (document de cadrage, novembre 2026). Le modèle `FEB.docx` est
> versionné depuis I2, nettoyé de ses métadonnées (DEB-Q9, DEB-Q33).

## ▶ ÉTAT AU 21/11/2026 (I2b) — À LIRE D'ABORD

**Où on en est** : chantiers P, B et **I clos** ; **I2 validé** sur le poste (relivré en I2a, `e28a964`) : 432 tests
MESURÉS (Domain 207, Application 66, Infrastructure 109, Web 30, Architecture 20), 0 échec ; couverture Domain,
Application, Infrastructure, Web : lignes 1181/1181, branches 356/356 (100 %), base inchangée ; mutations M9 (1 échec)
et M10 (5 échecs) détectées ; exemplaire EB-2026-0007 ouvert dans Word, conforme au modèle FEB. I2b : documents seuls.
**Critère de fin d'I2b** : `Appliquer-Lot I2b` vert et commité ; `Publier-Branche -Confirmer` puis
`Sauvegarder-Depots` ; `Ouvrir-Session` suivante sans point d'ATTENTION (0 commit à publier, sauvegarde à HEAD).
**Point de terminaison de la V1** (DEB-D4) : lot R1, critère du plan §1 ; périmètre gelé jusque-là.
**Prochain pas** : F1 (front, gabarit Vite, page Dialogue, composition dans `Program` ; plan §6, §7), avec son critère
de fin énoncé à l'ouverture ; DEB-Q10 à trancher d'abord.
**Ouverture de la session suivante** :

    Set-Location C:\Dev\Demonstrateur; . C:\Dev\Harnais\outillage\Charger-Harnais.ps1; Ouvrir-Session -BrancheAttendue main
    # attendu : « SESSION PRÉPARÉE : 8 pièce(s) dans C:\Dev\Sessions\Demonstrateur\<horodatage> » ; joindre toutes les
    # pièces et donner leur compte.

**Décisions récentes** : DEB-Q9 (20/11/2026), DEB-Q33 à DEB-Q37 (21/11/2026). **Anomalies ouvertes** : aucune.
**Questions ouvertes** : DEB-Q2 (fournisseur d'IA), DEB-Q10 (`node_modules` versionné ?).

## §0. Protocole (propre au projet)

| Emplacement | Valeur |
| --- | --- |
| Racine | `C:\Dev\Demonstrateur` ; solution `ExpressionBesoin/ExpressionBesoin.slnx` |
| Configuration | `harnais.config.psd1` (dépôt « Demonstrateur », type `Projet` depuis P0, `HarnaisMinimal` 1.9.0) |
| Projets de tests | `ExpressionBesoin/tests/ExpressionBesoin.{Domain,Application,Infrastructure,Web,Architecture}.Tests` (tous dans `Lot.TestsUnitaires` ; Application depuis B2, Infrastructure depuis I1) |
| Couverture | `ExpressionBesoin/coverage-baseline.json` (100 / 100 depuis `a73cb7e`), `ExpressionBesoin/coverage-dette.txt` (vide) ; projets mesurés : Domain.Tests, Application.Tests, Infrastructure.Tests, Web.Tests ; assemblies attendues : Domain, Application, Infrastructure, Web (Infrastructure depuis I1) |
| Outils | SDK 10.0.403 sur le poste (`global.json` : 10.0.100, `latestFeature`) ; `.config/dotnet-tools.json` : dotnet-coverage 18.9.0, ReportGenerator 5.5.11 |
| Paquets | `nuget.config` du projet : nuget.org seul (DEB-Q11) ; versions centralisées dans `Directory.Packages.props` |
| Tests du front | à partir de F1 : `ExpressionBesoin/src/ExpressionBesoin.Web/ClientApp/tests` |
| Données | fictives, jamais réelles ; jeux d'essai hors dépôt ; base SQLite hors dépôt, `%LOCALAPPDATA%\Demonstrateur\fiches.db` (DEB-Q31, configurée au lot F1) |
| Dépôt distant | `https://github.com/falyanak/Demonstrateur.git`, **public**, branche `main` ; publication par le développeur seul (`Publier-Branche -Confirmer`, Git Credential Manager) |
| Identifiants | `DEB-D…`, `DEB-A…`, `DEB-Q…` ; pris : DEB-D1 à DEB-D5, DEB-Q1 à DEB-Q37 ; mutations M1 à M10 |

Ligne enchaînée du projet (zip portant `DEPOT.txt` = « Demonstrateur ») :

    Set-Location C:\Dev\Demonstrateur; . C:\Dev\Harnais\outillage\Charger-Harnais.ps1; Appliquer-Lot NOM; if ($?) { git add -A; git commit -m "NOM : …" }

Lancer le site : profil `Localhost` de `launchSettings.json` (`http://localhost:5080`) ; contrôle : `GET api/sante`.

## §1. Objectif et état

Démonstrateur d'un assistant conversationnel qui aide un agent à rédiger la fiche d'expression de besoin d'une
demande d'évolution du SI ; décision du chef de service simulée ; fiche validée produite en .docx au modèle FEB ;
page de liste des fiches et de leur statut. Plan, périmètre et critère de fin : `specifications/plan-demonstrateur.md`.

| Chantier | Lots | État |
| --- | --- | --- |
| P — socle | P0a, P0, P0b | clos le 20/11/2026 : P0a (`56388a3`), P0 (`78ff21b`, base `a73cb7e`), P0b |
| B — métier (Domain, Application) | B1, B1b, B2, B2b | clos le 20/11/2026 : B1 (`3ee722c`), B1b (`17f3a8d`), B2 (`9007adf`), B2b |
| I — infrastructure | I1 (SQLite), I2 (.docx) | clos le 21/11/2026 : I1 (`f42de12`), I1b ; I2 relivré en I2a (`e28a964`), I2b |
| F — front | F1 (Dialogue), F2 (Liste) | à faire ; étape `TestsFront` attendue du lot OT2 du harnais |
| A — IA réelle | A1 | à faire ; fournisseur à trancher (DEB-Q2) |
| R — recette | R1 | à faire |

| Projet de tests | P0, poste (20/11/2026) | B1, poste (20/11/2026) | B2, poste (20/11/2026) | I1, poste (20/11/2026) | I2a, poste (21/11/2026) |
| --- | --- | --- | --- | --- | --- |
| ExpressionBesoin.Domain.Tests | 25 | 189 | 189 | 189 | 207 |
| ExpressionBesoin.Application.Tests | — | — | 65 | 65 | 66 |
| ExpressionBesoin.Infrastructure.Tests | — | — | — | 48 | 109 |
| ExpressionBesoin.Web.Tests | 30 | 30 | 30 | 30 | 30 |
| ExpressionBesoin.Architecture.Tests | 20 | 20 | 20 | 20 | 20 |
| **Total** | **75** (Release et Debug, 0 échec) | **239** (Release et Debug, 0 échec) | **304** (Debug, 0 échec) | **352** (Release et Debug, 0 échec) | **432** (Release et Debug, 0 échec) |
| Couverture | Domain, Web : lignes 64/64, branches 16/16 = 100 % | Domain, Web : lignes 410/410, branches 156/156 = 100 % | Domain, Application, Web : lignes 580/580, branches 202/202 = 100 % | Domain, Application, Infrastructure, Web : lignes 918/918, branches 266/266 = 100 % | Domain, Application, Infrastructure, Web : lignes 1181/1181, branches 356/356 = 100 % |

## §2. Points ouverts

1. Fournisseur d'IA (DEB-Q2) : à trancher avant A1 ; l'IA scénarisée suffit jusque-là. Recommandation : Albert API
   (DINUM) si le ministère y a accès, sinon API Mistral. Technologie : DEB-D5.
2. DEB-Q10 : à trancher à l'ouverture de F1, le prochain lot. Composition
   du générateur dans `Program` (`GenerateurDocumentFeb.DepuisRessource()`, un seul exemplaire) : au lot F1 (DEB-Q32).
3. OT2 du harnais central (`TestsFront`) : sans lui, Vitest est lancé à la main pour F1 et F2.
4. Côté harnais : HAR-A8 relevée le 20/11/2026 (`Publier-Branche` ne peut pas demander d'identifiant ; échec tardif et
   message trompeur sans `credential.helper`) ; correction recommandée plus tard, par un lot court du harnais, qui
   l'inscrira dans son prompt.

## §3. Décisions ACTÉES — ne pas rouvrir

| N° | Décision |
| --- | --- |
| DEB-Q1 à DEB-Q8 | Arbitrages du 20/11/2026, « recommandations ok » : plan §8. |
| DEB-D1 | Dépôt `Demonstrateur` (nom du distant GitHub), racine `C:\Dev\Demonstrateur`, solution `ExpressionBesoin/`, préfixe `DEB` ; révise DEB-Q1 pour la racine (20/11/2026). |
| DEB-D2 | Dépôt public : aucun document interne versionné (cahier des charges, modèle FEB) tant que DEB-Q9 n'est pas tranchée ; le plan en tient lieu (20/11/2026). |
| DEB-Q9 | Modèle FEB vierge versionné, s'il ne porte aucune donnée (« ok », 20/11/2026) : métadonnées nettoyées par I2 (DEB-Q33). |
| DEB-Q33 à DEB-Q37 | Document .docx (I2) : gabarit FEB nettoyé de ses seules métadonnées, ressource embarquée ; sans SDK OpenXML ; placement par libellé, gabarit vérifié au démarrage ; `ContenuDocument` du Domaine (fuseau, dernière décision) reçu par le port ; octets stables : plan §8 (« recommandations ok », 21/11/2026). |
| DEB-D3 | Chaque réponse de l'assistant se termine par la commande `Ouvrir-Session` de la prochaine session, prête à copier, avec le compte de pièces attendu (demande du développeur, 20/11/2026). |
| DEB-D4 | Point de terminaison de la V1 : lot R1, critère du plan §1 prouvé sur le poste ; périmètre gelé ; toute idée nouvelle rejoint les évolutions V2 (§6), ouvertes seulement par décision du développeur (demande du développeur, 20/11/2026 ; règle « Converger »). |
| DEB-D5 | Adaptateur IA (A1) : Microsoft Agent Framework (`ChatClientAgent`, 1.0 GA depuis avril 2026) sur un `IChatClient` compatible OpenAI, derrière `IAssistantIA` (Infrastructure) ; une réponse structurée par tour, relue par `ReponseIA.Lire` ; ni outils, ni workflows, ni multi-agents : le dialogue reste déterministe dans le Domaine ; MAF sert à l'historique, aux middlewares, à l'observabilité (« cadrage ok », 20/11/2026). |
| DEB-Q11 | `nuget.config` propre au projet : nuget.org seul (« recommandation ok », 20/11/2026). |
| DEB-Q12 | Persistance par `Microsoft.Data.Sqlite`, sans Entity Framework : fiche en JSON + colonnes de liste (« recommandation ok », 20/11/2026). |
| DEB-Q28 à DEB-Q32 | Infrastructure (I1) : JSON de la fiche par codes de rubrique et noms d'énumération, version 1 ; colonnes en ticks UTC, BLOB, entiers, tables `STRICT` ; référentiel fictif en code ; base dans `%LOCALAPPDATA%` ; composition dans `Program` au lot F1 : plan §8 (« recommandations ok », 20/11/2026). |
| DEB-Q21 à DEB-Q27 | Cas d'usage (B2) : historique du dialogue persisté à part de la fiche, tour atomique (refus de l'IA → rien d'écrit ; panne = exception), hors périmètre sans propositions, année du numéro en heure de Paris (fuseau en paramètre), auteur de la décision = valideur de l'en-tête, pas de version optimiste en V1, code de rubrique sans casse : plan §8 (« recommandations ok », 20/11/2026). |
| DEB-Q13 à DEB-Q20 | Règles du Domaine de la fiche (origines, ordre de questionnement, fusion, modification au seul Brouillon, contrôles de soumission, liste des décisions, tolérance de lecture de la réponse de l'IA, mutations sur copie jetable) : plan §8 (« recommandations ok », 20/11/2026). |

**Architecture** : clean architecture du modèle générique (Domain, Application sans E/S, Infrastructure, Web), règles
de dépendance vérifiées sur les `.csproj` par `ExpressionBesoin.Architecture.Tests` ; cas d'usage lire → décider
(pur, horloge en paramètre) → écrire ; `Resultat<T>` pour les refus métier ; ce qui relève de l'IA (comprendre,
reformuler, proposer) est séparé de l'automatisation déterministe (rubriques, fusion, complétude, statuts,
numérotation, document) : plan §2. Versions de test : xUnit v3 (3.2.2) et non v4, migration ensemble après essai.

## §4. Cartographie (HEAD fait foi)

```
harnais.config.psd1  README.md  .gitignore  .gitattributes
docs/            prompt-reprise-Demonstrateur.md  historique-Demonstrateur.md
specifications/  plan-demonstrateur.md
ExpressionBesoin/
  ExpressionBesoin.slnx  global.json  nuget.config  Directory.Build.props  Directory.Packages.props
  .config/dotnet-tools.json  coverage-baseline.json  coverage-dette.txt
  src/ExpressionBesoin.Domain/          Resultat.cs (Refus, Resultat<T>, Resultat) ; ListeImmuable.cs (égalité structurelle) ;
                                        Fiches/ Catalogue, FicheBesoin (types), Fiche, Completude, Dialogue, Cycle,
                                        Numerotation, ReponseIA (B1), ContenuDocument (I2)
  src/ExpressionBesoin.Application/     ResultatOuvert.cs (aide des coquilles) ; Fiches/ Ports, Modeles, DecisionsFiches (pures),
                                        ServiceFiches (coquille) (B2)
  src/ExpressionBesoin.Infrastructure/  Fiches/ CodecFiche (pur, JSON v1), SchemaFiches (SQL, DoitCreer pur), DepotFichesSqlite
                                        (coquille) ; Sqlite/ ConnexionsSqlite, OptionsSqlite ; Organisation/
                                        ReferentielOrganisationFictif (I1) ; Documents/ PlanFeb, GabaritFeb, MetadonneesFeb
                                        (purs), GenerateurDocumentFeb (adaptateur), FEB.docx (ressource embarquée) (I2)
  src/ExpressionBesoin.Web/             Program.cs ; Sante/DiagnosticSante.cs (pur), Sante/PointsSante.cs ;
                                        Hebergement/EnvironnementExtensions.cs ; Controllers/HomeController.cs ;
                                        Views/ ; Properties/launchSettings.json ; appsettings*.json
  tests/Directory.Build.props
  tests/ExpressionBesoin.Domain.Tests/        ResultatTests.cs, ListeImmuableTests.cs ; Fiches/ Essais.cs (aides),
                                              Catalogue, Fiche, CompletudeDialogue, Cycle, Numerotation, ReponseIA (Tests)
  tests/ExpressionBesoin.Application.Tests/   Fiches/ Essais.cs, Doublures.cs (ports en mémoire, attente différée en option),
                                              DecisionsFiches, Modeles, ServiceFiches (Tests)
  tests/ExpressionBesoin.Infrastructure.Tests/  Essais.cs (aides, BaseTemporaire : fichier au schéma de production) ;
                                              Fiches/ CodecFiche, SchemaFiches, DepotFichesSqlite ; Sqlite/ ; Organisation/ (Tests) ;
                                              Documents/ EssaisDocuments (attendus de la réplique Python), GabaritFeb, PlanFeb,
                                              MetadonneesFeb, GenerateurDocumentFeb (Tests)
  tests/ExpressionBesoin.Web.Tests/           DiagnosticSanteTests.cs, EnvironnementExtensionsTests.cs,
                                              SanteContratTests.cs (UsineWeb, environnement Production)
  tests/ExpressionBesoin.Architecture.Tests/  AnalyseCsproj.cs (pur), ReglesSolution.cs,
                                              AnalyseCsprojTests.cs, SolutionTests.cs
```

## §5. Contrats

| Contrat | Forme | Figé par |
| --- | --- | --- |
| `GET api/sante` | `{ "etat": "ok", "version": "<informative>", "demarrage": "<ISO 8601>", "dureeSecondes": <entier ≥ 0> }`, exactement ces champs, dans cet ordre | `SanteContratTests`, `DiagnosticSanteTests` |
| Réponse de l'IA | plan §2 ; tolérances DEB-Q19 ; codes de rubrique de `Catalogue.cs` ; refus `reponseIA.jsonInvalide`, `reponseIA.formeInvalide` ; écarts `MotifEcart` | `ReponseIATests` (B1) |
| Codes de refus du Domaine | `fiche.nonModifiable`, `proposition.absente`, `cycle.transitionInterdite`, `soumission.{confirmationAbsente,incomplete,propositionsEnAttente}`, `decision.commentaireObligatoire`, `numerotation.{annee,illisible,anneeAnterieure,epuisee}` | tests de B1 |
| Stockage des fiches | JSON v1 (DEB-Q28) ; schéma SQLite `user_version = 1` (DEB-Q29), tables `Fiches`, `Echanges` | `CodecFicheTests` (JSON de référence à l'octet), `DepotFichesSqliteTests`, `SchemaFichesTests` (I1) |
| API des fiches | plan §5 ; cas d'usage `ServiceFiches` (B2) ; refus `fiche.introuvable`, `agent.inconnu`, `rubrique.inconnue`, `message.vide`, `document.nonValidee` | tests de B2 ; routes à venir (F1) |
| Document .docx | modèle FEB embarqué (empreinte figée) ; placement DEB-Q35 ; contenu DEB-Q36 ; octets stables DEB-Q37 | `ContenuDocumentTests`, `GabaritFebTests`, `GenerateurDocumentFebTests` (55 paragraphes attendus, I2) |

## §6. Reliquat, hygiène, leçons

Aucune anomalie du projet. **Évolutions V2** (hors V1, DEB-D4 ; ouvertes seulement par décision du développeur) :
courriel au chef de service, habilitations, instruction XA / SICCRF, rapprochement de demandes, tableau de bord,
synthèse CODIR / COPIL, export PDF, version optimiste des fiches (DEB-Q26) ; toute idée née pendant la V1 s'ajoute ici.

## §7. Prochaine session

Acquis : plan v1.8 ; socle P0, Domaine B1, Application B2, Infrastructure I1 et I2 validés sur le poste (432 tests,
couverture 100 %, mutations détectées, .docx ouvert dans Word).
Audit honnête : chez l'assistant, ni SDK .NET ni NuGet ; un lot C# y est vérifié par analyse syntaxique (tree-sitter)
et réplique Python des attendus, le poste tranchant compilation, tests, couverture et mutations (B1 : tout conforme
au premier essai ; B2 et I1 de même, I1 SQL compris, rejoué dans sqlite3). I2 : placement validé par une réplique Python et
LibreOffice, puis dans Word sur le poste ; seul écart, un test qui comparait selon la culture (I2a). Plan ordonné : F1,
F2, A1, R1 (point de terminaison de la V1, DEB-D4). Pièces à envoyer : toutes celles d'`Ouvrir-Session` (8), avec leur
compte.
