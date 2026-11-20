# Prompt de reprise — Demonstrateur

> Mis à jour le 20/11/2026 par le lot P0. Règles génériques : harnais central, `regles\regles-de-travail.md` (v5) ;
> modèle de projet : `regles\harnais-modele-generique.md` (v2) ; front : `regles\gabarit-frontend-vite-aspnet.md` ;
> outillage : `C:\Dev\Harnais\docs\specification-harnais-central.md` (fait foi).
> Historique détaillé : `docs/historique-Demonstrateur.md` (non envoyé par défaut). Documents hors dépôt dont dépend ce
> prompt (DEB-D2, tenus par le développeur, joints aux sessions qui en ont besoin) : cahier des charges
> `Expression_de_besoin_demonstrateur_IA_SICCRF.docx` (document de cadrage, novembre 2026) et modèle de fiche
> `FEB.docx`.

## ▶ ÉTAT AU 20/11/2026 (P0) — À LIRE D'ABORD

**Où on en est** : P0a validé (HEAD `56388a3`, publié, session conforme). **P0 livré** : socle .NET 10, passage au
type `Projet` ; comptes ESTIMÉS (non mesurés) : Domain.Tests 25, Web.Tests 30, Architecture.Tests 20, soit 75.
**Critère de fin de P0** : `Appliquer-Lot P0` compile et passe les tests (Release), commit par la ligne enchaînée.
**Prochain pas** : P0b (comptes MESURÉS par `Compter-Tests -Compiler`, base de couverture relevée par
`Lancer-Couverture -UpdateBaseline` et commitée) ; aucun autre lot avant P0b.
**Ouverture de la session suivante** :

    Set-Location C:\Dev\Demonstrateur; . C:\Dev\Harnais\outillage\Charger-Harnais.ps1; Ouvrir-Session -BrancheAttendue main
    # attendu : « SESSION PRÉPARÉE : 8 pièce(s) dans C:\Dev\Sessions\Demonstrateur\<horodatage> » ; joindre toutes les
    # pièces et donner leur compte.

**Décisions récentes** : DEB-D1, DEB-D2 ; arbitrages DEB-Q1 à DEB-Q8, DEB-Q11, DEB-Q12. **Anomalies ouvertes** :
aucune. **Questions ouvertes** : DEB-Q9 (modèle FEB versionné ?), DEB-Q10 (`node_modules` versionné ?).

## §0. Protocole (propre au projet)

| Emplacement | Valeur |
| --- | --- |
| Racine | `C:\Dev\Demonstrateur` ; solution `ExpressionBesoin/ExpressionBesoin.slnx` |
| Configuration | `harnais.config.psd1` (dépôt « Demonstrateur », type `Projet` depuis P0, `HarnaisMinimal` 1.9.0) |
| Projets de tests | `ExpressionBesoin/tests/ExpressionBesoin.{Domain,Web,Architecture}.Tests` (tous dans `Lot.TestsUnitaires`) |
| Couverture | `ExpressionBesoin/coverage-baseline.json` (0 / 0 jusqu'à P0b), `ExpressionBesoin/coverage-dette.txt` (vide) ; projets mesurés : Domain.Tests, Web.Tests ; assemblies attendues : Domain, Web |
| Outils | SDK 10.0.403 sur le poste (`global.json` : 10.0.100, `latestFeature`) ; `.config/dotnet-tools.json` : dotnet-coverage 18.9.0, ReportGenerator 5.5.11 |
| Paquets | `nuget.config` du projet : nuget.org seul (DEB-Q11) ; versions centralisées dans `Directory.Packages.props` |
| Tests du front | à partir de F1 : `ExpressionBesoin/src/ExpressionBesoin.Web/ClientApp/tests` |
| Données | fictives, jamais réelles ; jeux d'essai hors dépôt |
| Dépôt distant | `https://github.com/falyanak/Demonstrateur.git`, **public**, branche `main` ; publication par le développeur seul (`Publier-Branche -Confirmer`, Git Credential Manager) |
| Identifiants | `DEB-D…`, `DEB-A…`, `DEB-Q…` ; pris : DEB-D1, DEB-D2, DEB-Q1 à DEB-Q12 |

Ligne enchaînée du projet (zip portant `DEPOT.txt` = « Demonstrateur ») :

    Set-Location C:\Dev\Demonstrateur; . C:\Dev\Harnais\outillage\Charger-Harnais.ps1; Appliquer-Lot NOM; if ($?) { git add -A; git commit -m "NOM : …" }

Lancer le site : profil `Localhost` de `launchSettings.json` (`http://localhost:5080`) ; contrôle : `GET api/sante`.

## §1. Objectif et état

Démonstrateur d'un assistant conversationnel qui aide un agent à rédiger la fiche d'expression de besoin d'une
demande d'évolution du SI ; décision du chef de service simulée ; fiche validée produite en .docx au modèle FEB ;
page de liste des fiches et de leur statut. Plan, périmètre et critère de fin : `specifications/plan-demonstrateur.md`.

| Chantier | Lots | État |
| --- | --- | --- |
| P — socle | P0a, P0, P0b | P0a validé le 20/11/2026 (HEAD `56388a3`) ; P0 livré le 20/11/2026 |
| B — métier (Domain, Application) | B1, B2 | à faire |
| I — infrastructure | I1 (SQLite), I2 (.docx) | à faire |
| F — front | F1 (Dialogue), F2 (Liste) | à faire ; étape `TestsFront` attendue du lot OT2 du harnais |
| A — IA réelle | A1 | à faire ; fournisseur à trancher (DEB-Q2) |
| R — recette | R1 | à faire |

| Projet de tests | P0 (estimé) |
| --- | --- |
| ExpressionBesoin.Domain.Tests | 25 |
| ExpressionBesoin.Web.Tests | 30 |
| ExpressionBesoin.Architecture.Tests | 20 |
| **Total** | **75** |

## §2. Points ouverts

1. Fournisseur d'IA (DEB-Q2) : à trancher avant A1 ; l'IA scénarisée suffit jusque-là.
2. DEB-Q9 et DEB-Q10 (plan §8), à trancher au plus tard à I2 et F1.
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
| DEB-Q11 | `nuget.config` propre au projet : nuget.org seul (« recommandation ok », 20/11/2026). |
| DEB-Q12 | Persistance par `Microsoft.Data.Sqlite`, sans Entity Framework : fiche en JSON + colonnes de liste (« recommandation ok », 20/11/2026). |

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
  src/ExpressionBesoin.Domain/          Resultat.cs (Refus, Resultat<T>, Resultat)
  src/ExpressionBesoin.Application/     (vide)
  src/ExpressionBesoin.Infrastructure/  (vide)
  src/ExpressionBesoin.Web/             Program.cs ; Sante/DiagnosticSante.cs (pur), Sante/PointsSante.cs ;
                                        Hebergement/EnvironnementExtensions.cs ; Controllers/HomeController.cs ;
                                        Views/ ; Properties/launchSettings.json ; appsettings*.json
  tests/Directory.Build.props
  tests/ExpressionBesoin.Domain.Tests/        ResultatTests.cs
  tests/ExpressionBesoin.Web.Tests/           DiagnosticSanteTests.cs, EnvironnementExtensionsTests.cs,
                                              SanteContratTests.cs (UsineWeb, environnement Production)
  tests/ExpressionBesoin.Architecture.Tests/  AnalyseCsproj.cs (pur), ReglesSolution.cs,
                                              AnalyseCsprojTests.cs, SolutionTests.cs
```

## §5. Contrats

| Contrat | Forme | Figé par |
| --- | --- | --- |
| `GET api/sante` | `{ "etat": "ok", "version": "<informative>", "demarrage": "<ISO 8601>", "dureeSecondes": <entier ≥ 0> }`, exactement ces champs, dans cet ordre | `SanteContratTests`, `DiagnosticSanteTests` |
| Réponse de l'IA, API des fiches | plan §2 et §5 | à venir (B1, F1) |

## §6. Reliquat, hygiène, leçons

Aucune anomalie du projet. Évolutions hors périmètre (plan §1) : courriel, habilitations, instruction XA / SICCRF,
rapprochement, tableau de bord, export PDF.

## §7. Prochaine session

Acquis : plan v1.1, dépôt publié, socle P0. Audit honnête : la compilation des projets de tests et le test
d'intégration `SanteContratTests` n'ont pas été vérifiés chez l'assistant (NuGet inaccessible) ; la sortie
d'`Appliquer-Lot P0` les tranche. Plan ordonné : P0b, B1, B2, I1, I2, F1, F2, A1, R1. Pièces à envoyer : toutes celles
d'`Ouvrir-Session` (8), avec leur compte ; pour I2, le modèle `FEB.docx` (hors dépôt, DEB-D2).
