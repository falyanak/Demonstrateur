# Prompt de reprise — Demonstrateur

> Mis à jour le 21/11/2026 par le lot A1. Règles génériques : harnais central, `regles\regles-de-travail.md` (v5) ;
> modèle de projet : `regles\harnais-modele-generique.md` (v2) ; front : `regles\gabarit-frontend-vite-aspnet.md` ;
> outillage : `C:\Dev\Harnais\docs\specification-harnais-central.md` (fait foi).
> Historique détaillé : `docs/historique-Demonstrateur.md` (non envoyé par défaut). Documents hors dépôt dont dépend ce
> prompt (DEB-D2, tenu par le développeur, joint aux sessions qui en ont besoin) : cahier des charges
> `Expression_de_besoin_demonstrateur_IA_SICCRF.docx` (document de cadrage, novembre 2026). Le modèle `FEB.docx` est
> versionné depuis I2, nettoyé de ses métadonnées (DEB-Q9, DEB-Q33).

## ▶ ÉTAT AU 21/11/2026 (A1 livré, à appliquer) — À LIRE D'ABORD

**Où on en est** : chantiers P, B, I, F, L clos ; hygiène HY1 à HY3 close. Chantier A ouvert : accès à Albert API
confirmé (DEB-Q83) et mesuré au poste par une sonde (`openweight-medium` retenu, DEB-Q84) ; **A1 livré** (adaptateur
réel, DEB-Q84 à DEB-Q88), non encore appliqué. Comptes ESTIMÉS : 641 tests .NET (Infrastructure 190, Web 133).
**Prochaine action** : `ALBERT_API_KEY` en variable utilisateur Windows, Visual Studio et site fermés, puis la ligne
enchaînée d'A1 (§0) ; ensuite le critère d'A1 au navigateur (plan §7) : scénario e-commerce sans invention, « pc ne
démarre plus » et « copieur en panne » hors périmètre sans proposition.
**Ensuite** : A1b (comptes mesurés), puis R1, **point de terminaison de la V1** (DEB-D4) ; l'étape « Validation » du
panneau y sera vue au navigateur. **Avant R1** : publier et sauvegarder le dernier commit.
**Ouverture de la session suivante** (site arrêté : il verrouille les DLL de `bin\Debug`) :

    Set-Location C:\Dev\Demonstrateur; . C:\Dev\Harnais\outillage\Charger-Harnais.ps1; Ouvrir-Session -BrancheAttendue main
    # attendu : « SESSION PRÉPARÉE : 8 pièce(s) dans C:\Dev\Sessions\Demonstrateur\<horodatage> » ; joindre toutes les
    # pièces et donner leur compte.

**Décisions récentes** : DEB-Q83 à DEB-Q88 (21/11/2026). **Anomalies ouvertes** : aucune. **Questions ouvertes** :
aucune. **Hygiène** : aucune en cours.

## §0. Protocole (propre au projet)

| Emplacement | Valeur |
| --- | --- |
| Racine | `C:\Dev\Demonstrateur` ; solution `ExpressionBesoin/ExpressionBesoin.slnx` |
| Configuration | `harnais.config.psd1` (dépôt « Demonstrateur », type `Projet` depuis P0, `HarnaisMinimal` 1.9.0) |
| Projets de tests | `ExpressionBesoin/tests/ExpressionBesoin.{Domain,Application,Infrastructure,Web,Architecture}.Tests` (tous dans `Lot.TestsUnitaires` ; Application depuis B2, Infrastructure depuis I1) |
| Couverture | `ExpressionBesoin/coverage-baseline.json` (100 / 100 depuis `a73cb7e`), `ExpressionBesoin/coverage-dette.txt` (vide) ; projets mesurés : Domain.Tests, Application.Tests, Infrastructure.Tests, Web.Tests ; assemblies attendues : Domain, Application, Infrastructure, Web (Infrastructure depuis I1) |
| Outils | SDK 10.0.403, bloqué par `global.json` (`rollForward: disable`, DEB-Q47), à la RACINE du dépôt depuis HY3 (DEB-A2) ; garde `VerifierSdk` (`Directory.Build.props`, `SdkAttendu`) ; .NET 11 installé, non utilisé ; `.config/dotnet-tools.json` : dotnet-coverage 18.12.0, ReportGenerator 5.6.0 (HY1) |
| Paquets | `nuget.config` du projet : nuget.org seul (DEB-Q11) ; versions centralisées dans `Directory.Packages.props` |
| Tests du front | `ExpressionBesoin/src/ExpressionBesoin.Web/ClientApp/tests` (Vitest 5) ; contrats partagés `tests/contrats/{fiche,consultation,tour,liste}-reference.json` |
| Front | Node.js 26.13.1 LTS et son npm (DEB-D7) ; `node_modules` versionné (DEB-D6), Windows x64 ; `npm ci` seulement s'il manque ; versions exactes (`.npmrc`) |
| Données | fictives, jamais réelles ; jeux d'essai hors dépôt ; base SQLite hors dépôt, clé `Fiches:Base` fixée par environnement (DEB-Q54, DEB-Q55) : poste `C:\Dev\Demonstrateur-data\fiches.db` (`appsettings.Localhost.json`) ; serveurs `appsettings.Staging.json`, `appsettings.Production.json` (`D:\Donnees\Demonstrateur-data[-staging]`, à confirmer) ; variable `Fiches__Base` prioritaire ; rien dans `appsettings.json` |
| Dépôt distant | `https://github.com/falyanak/Demonstrateur.git`, **public**, branche `main` ; publication par le développeur seul (`Publier-Branche -Confirmer`, Git Credential Manager) |
| Identifiants | `DEB-D…`, `DEB-A…`, `DEB-Q…`, `DEB-HY…` ; pris : DEB-D1 à DEB-D7, DEB-Q1 à DEB-Q88, DEB-A1 (close par F2), DEB-A2 (close par HY3), DEB-HY1 (close par HY1), DEB-HY2 (close par HY2) ; mutations M1 à M10, MF1 à MF30 (front) |

Ligne enchaînée du projet (zip portant `DEPOT.txt` = « Demonstrateur ») :

    Set-Location C:\Dev\Demonstrateur; . C:\Dev\Harnais\outillage\Charger-Harnais.ps1; Appliquer-Lot NOM; if ($?) { git add -A; git commit -m "NOM : …" }

Lancer le site : `npm run dev` dans `ClientApp` (Vite, 5173) ET profil `Localhost` de `launchSettings.json`
(`http://localhost:5080`) ; ouvrir 5080 ; contrôle : `GET api/sante`. IA réelle (A1) : variable utilisateur
`ALBERT_API_KEY` (jamais dans un fichier) ; hors réseau : variable `IA__Fournisseur` = `Scenarise` (DEB-Q86). L'arrêter avant `Ouvrir-Session`, `Appliquer-Lot`
ou `Compter-Tests` : il verrouille les DLL de `bin\Debug` (MSB3027, 21/11/2026). Tests du front :

    Set-Location C:\Dev\Demonstrateur\ExpressionBesoin\src\ExpressionBesoin.Web\ClientApp; npm test

## §1. Objectif et état

Démonstrateur d'un assistant conversationnel qui aide un agent à rédiger la fiche d'expression de besoin d'une
demande d'évolution du SI ; décision du chef de service simulée ; fiche validée produite en .docx au modèle FEB ;
page de liste des fiches et de leur statut. Plan, périmètre et critère de fin : `specifications/plan-demonstrateur.md`.

| Chantier | Lots | État |
| --- | --- | --- |
| P — socle | P0a, P0, P0b | clos le 20/11/2026 : P0a (`56388a3`), P0 (`78ff21b`, base `a73cb7e`), P0b |
| B — métier (Domain, Application) | B1, B1b, B2, B2b | clos le 20/11/2026 : B1 (`3ee722c`), B1b (`17f3a8d`), B2 (`9007adf`), B2b |
| I — infrastructure | I1 (SQLite), I2 (.docx) | clos le 21/11/2026 : I1 (`f42de12`), I1b ; I2 relivré en I2a (`e28a964`), I2b |
| F — front | F1 (socle, API), F2 (Dialogue), F3 (Liste) | F1 clos le 21/11/2026 : F1 (`bfaaf97`), F1b (`fe31e69`), F1c (`3e45551`) ; F2 clos le 21/11/2026 : F2 (`ba3aafd`), F2c (`50ccdb4`) ; F3 clos le 21/11/2026 : F3 (`ce06fe6`), F3b ; `TestsFront` attendu d'OT2 |
| Hygiène | HY1, HY1b (DEB-HY1 : NuGet, outils dotnet, SDK) ; HY2 (DEB-HY2 : dossier de données par environnement) ; HY3 (DEB-A2 : SDK depuis la racine) | HY1 clos le 21/11/2026 : HY1 (`d307a91`), HY1b ; HY2 clos le 21/11/2026 : HY2 (`9b21bdb`) ; HY3 clos le 21/11/2026 : HY3 (`fcdb37a`) |
| L — cycle de vie | L1 (serveur), L2 (front) | ouvert le 21/11/2026 (DEB-Q65) ; clos le 21/11/2026 : L1 (`b275507`), L1b ; L2 (`ec192e6`), L2b |
| A — IA réelle | A1 | ouvert le 21/11/2026 : Albert API (DEB-Q83), `openweight-medium` (DEB-Q84) ; A1 livré, à appliquer ; critère : plan §7 (DEB-Q64) |
| R — recette | R1 | à faire |

Comptes de P0 à I1 : historique. Depuis I2a :

| Projet de tests | I2a, poste | F1, poste | HY1, poste | F2, poste | HY2, poste | HY3, poste | F3, poste (21/11/2026) | L1, poste (21/11/2026) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ExpressionBesoin.Domain.Tests | 207 | 207 | 207 | 207 | 207 | 207 | 207 | 229 |
| ExpressionBesoin.Application.Tests | 66 | 66 | 66 | 66 | 66 | 66 | 66 | 66 |
| ExpressionBesoin.Infrastructure.Tests | 109 | 135 | 135 | 135 | 135 | 135 | 135 | 142 |
| ExpressionBesoin.Web.Tests | 30 | 97 | 97 | 103 | 107 | 107 | 108 | 108 |
| ExpressionBesoin.Architecture.Tests | 20 | 20 | 20 | 20 | 20 | 23 | 23 | 23 |
| **Total** | **432** | **525** ; Vitest **58** | **525** (xUnit 4) | **531** ; Vitest **165** | **535** | **538** (Debug, `Compter-Tests`, 0 échec, SDK 10.0.403) | **539** (Debug, `Compter-Tests`, 0 échec) ; Vitest **200** au poste | **568** (Debug, `Compter-Tests`, 0 échec) ; Vitest 200 (inchangé) |
| Couverture | 1181/1181 lignes, 356/356 branches | cliquet 100 / 100 ; front 74/74, 57/57 | cliquet 100 / 100 | cliquet 100 / 100 ; front 153/153, 123/123 | cliquet 100 / 100 | cliquet 100 / 100 (`Appliquer-Lot HY3` vert) | cliquet 100 / 100 (`Appliquer-Lot F3` vert) ; front 167/167 lignes, 131/131 branches (chez l'assistant) | cliquet 100 / 100 (`Appliquer-Lot L1` vert, commit par la ligne enchaînée) |

L1 : comptes mesurés égaux à l'estimation (Domain +22, Infrastructure +7). L2, poste (21/11/2026) : .NET inchangé, 568, 0 échec (Release, `Appliquer-Lot L2`) ; Vitest **249**, 8 fichiers (`npm test`), égal à l'estimation ; front 181/181 lignes, 145/145 branches (chez l'assistant). Comptes .NET mesurés de HY1 à HY2 : compilés par le SDK 11.0.100 (DEB-A2), cible `net10.0` inchangée ; depuis HY3 : SDK 10.0.403.

## §2. Points ouverts

1. A1 : appliquer, puis jouer le critère au navigateur (IA réelle) ; un écart au critère = lot correctif A1x, qui ne
   touche que le prompt (`PromptFiche`) ou le modèle (`IA:Modele`).
2. Publication et sauvegarde du dernier commit : exigées par le critère de fin de la V1 (R1).
6. Chemins des serveurs (DEB-Q55) : `D:\Donnees\Demonstrateur-data\fiches.db` (Production) et
   `D:\Donnees\Demonstrateur-data-staging\fiches.db` (Staging), à confirmer avec l'exploitation avant toute
   publication (évolution V2, DEB-Q56) ; un changement = retouche des deux fichiers et de `ConfigurationEnvironnementsTests`.
3. OT2 du harnais central (`TestsFront`) : sans lui, Vitest est lancé à la main pour F1 à F3.
4. Côté harnais : HAR-A8 relevée le 20/11/2026 (`Publier-Branche` ne peut pas demander d'identifiant ; échec tardif et
   message trompeur sans `credential.helper`) ; correction recommandée plus tard, par un lot court du harnais.
5. Côté harnais, leçons à verser aux règles par un lot du harnais : (a) gabarit front F3, ignorer les dossiers en point
   de `node_modules` (F1b) ; (b) annexe, `Invoke-RestMethod` de Windows PowerShell 5.1 rend un tableau JSON comme UN
   objet : `@(…).Count` vaut 1 (même piège que `ConvertFrom-Json`) ; compter par `(Invoke-RestMethod …).Count` ;
   (c) modèle générique §3 et §10 : dossier de données `C:\Dev\<Depot>-data` sur le poste, chemin par environnement
   (DEB-Q57) ; (d) modèle générique §3 et §4.1 : `global.json` à la RACINE du dépôt, pas dans `<Solution>/` : `dotnet`
   le cherche depuis le dossier courant, et les commandes du harnais partent de la racine (DEB-A2).

## §3. Décisions ACTÉES — ne pas rouvrir

| N° | Décision |
| --- | --- |
| DEB-Q1 à DEB-Q8 | Arbitrages du 20/11/2026, « recommandations ok » : plan §8. |
| DEB-Q83 à DEB-Q88 | IA réelle (A1) : accès à Albert API confirmé et sondé au poste ; `openweight-medium` retenu sur mesure (gpt-oss-120b : 35 s, invention) ; `json_schema` strict ; `IA:Fournisseur`/`Point`/`Modele` par environnement, clé `ALBERT_API_KEY`, arrêt au démarrage si incomplet ; proxy, 60 s, un seul nouvel essai sur limite par minute, sinon 503 ; prompt pur, adaptateur testé sur faux fournisseur : plan §8 (« recommandations ok », 21/11/2026). |
| DEB-Q2, DEB-Q64 | Fournisseur de l'IA réelle : Albert API si le ministère y a accès, sinon API Mistral, accès vérifié à l'ouverture d'A1 ; repli hors réseau : IA scénarisée. Demandes d'assistance validées au parcours de F3 (« pc ne démarre plus ») : limite connue de l'IA scénarisée, non corrigée ; détection jugée dans le critère d'A1 : plan §7, §8 (« recommandations ok », 21/11/2026). |
| DEB-D1 | Dépôt `Demonstrateur` (nom du distant GitHub), racine `C:\Dev\Demonstrateur`, solution `ExpressionBesoin/`, préfixe `DEB` ; révise DEB-Q1 pour la racine (20/11/2026). |
| DEB-D2 | Dépôt public : aucun document interne versionné (cahier des charges, modèle FEB) tant que DEB-Q9 n'est pas tranchée ; le plan en tient lieu (20/11/2026). |
| DEB-Q9 | Modèle FEB vierge versionné, s'il ne porte aucune donnée (« ok », 20/11/2026) : métadonnées nettoyées par I2 (DEB-Q33). |
| DEB-Q33 à DEB-Q37 | Document .docx (I2) : gabarit FEB nettoyé de ses seules métadonnées, ressource embarquée ; sans SDK OpenXML ; placement par libellé, gabarit vérifié au démarrage ; `ContenuDocument` du Domaine (fuseau, dernière décision) reçu par le port ; octets stables : plan §8 (« recommandations ok », 21/11/2026). |
| DEB-D3 | Chaque réponse de l'assistant se termine par la commande `Ouvrir-Session` de la prochaine session, prête à copier, avec le compte de pièces attendu (demande du développeur, 20/11/2026). |
| DEB-D4 | Point de terminaison de la V1 : lot R1, critère du plan §1 prouvé sur le poste ; périmètre gelé ; toute idée nouvelle rejoint les évolutions V2 (§6), ouvertes seulement par décision du développeur (demande du développeur, 20/11/2026 ; règle « Converger »). |
| DEB-D5 | Adaptateur IA (A1) : Microsoft Agent Framework (`ChatClientAgent`, 1.0 GA depuis avril 2026) sur un `IChatClient` compatible OpenAI, derrière `IAssistantIA` (Infrastructure) ; une réponse structurée par tour, relue par `ReponseIA.Lire` ; ni outils, ni workflows, ni multi-agents : le dialogue reste déterministe dans le Domaine ; MAF sert à l'historique, aux middlewares, à l'observabilité (« cadrage ok », 20/11/2026). |
| DEB-Q11 | `nuget.config` propre au projet : nuget.org seul (« recommandation ok », 20/11/2026). |
| DEB-D6 | `ClientApp/node_modules` versionné (révise la recommandation de DEB-Q10) : Windows x64, `bin` ré-autorisés, caches ignorés, `-text` (octets tels que publiés) ; `npm ci` s'il manque (21/11/2026). |
| DEB-D7 | Principe des versions : dernière STABLE de chaque outil et bibliothèque, dernière LTS des environnements d'exécution, épinglées exactement ; prime sur la marge de 14 jours du modèle générique ; Node.js 26.13.1, TypeScript 7.0.2, Vite 8.3.8, Vitest 5.1.1 ; .NET 10 ; NuGet par DEB-HY1 : plan §8 (demande du développeur, 21/11/2026). |
| DEB-Q45 à DEB-Q47 | Versions (HY1) : `net10.0` gardé malgré .NET 11 (non LTS), paquets du framework en 10.0.15 ; xUnit 4.0.3 (MTP v2), runner 4.0.1, Test.Sdk 18.12.0, tous les projets ensemble, révise « xUnit v3 » ; SDK bloqué sur 10.0.403 pour la CI/CD (DEB-Q47, choix du développeur) : plan §8 (21/11/2026). |
| DEB-Q58 | DEB-A2 corrigée tout de suite, avant F3 : `global.json` déplacé à la racine du dépôt (racine de lot ajoutée d'abord, par F2c), garde du SDK au build ; DEB-Q47 inchangée (choix du développeur, 21/11/2026). |
| DEB-Q79 à DEB-Q82 | Panneau « Cycle de vie » (L2) : `cycle` lu strictement par `lireConsultation` ; consultation relue après une soumission, une décision ou une reprise réussie (fiche et cycle relus, conversation gardée ; échec → fiche de l'action, ancien cycle, erreur), aucune transition déduite par la page, contrat serveur inchangé ; panneau sous le circuit : étape en français, statuts en badges, acteur, date et heure à Paris, message pour un cycle vide ; `core/dates.ts` (`dateCourte`, `dateHeure`, `FUSEAU`) : plan §8 (« recommandations ok », 21/11/2026). |
| DEB-Q65 à DEB-Q78 | Cycle de vie (chantier L, ouvert avant A1, DEB-Q65) : aucun texte libre ; table `Transitions` (métier, même transaction que la fiche) plutôt que Serilog (évolution V2), DEB-Q67 à DEB-Q72 sans objet ; ligne `Avant`/`Apres`/`Acteur`/`Le`, étape calculée, contrainte CHECK sur les six transitions ; `Transitions.Entre` pur, `Cycle` inchangé ; migration 1 → 2 au démarrage sans reconstitution ; champ `cycle` de `GET api/fiches/{id}`, panneau en L2 : plan §8 (« recommandations ok » puis « accord », 21/11/2026). |
| DEB-Q59 à DEB-Q63 | Page Liste (F3) : à l'accueil, sans route nouvelle ; action « Ouvrir » (et non « Reprendre », réservé à la transition) et « Télécharger » si validée ; `lireResumes`, `lister`, vue pure, coquille `PageListe.ts`, badge du statut dans `core/statuts.ts`, référence `liste-reference.json` partagée ; date de création jj/mm/aaaa à Paris ; toutes les fiches, ordre du serveur, rechargée à l'affichage : plan §8 (« recommandations ok », 21/11/2026). |
| DEB-Q54 à DEB-Q57 | Données (HY2) : poste `C:\Dev\Demonstrateur-data\fiches.db` (révise DEB-Q31) ; `Fiches:Base` dans `appsettings.{Localhost,Staging,Production}.json`, rien dans `appsettings.json`, variable `Fiches__Base` prioritaire ; serveur : hors dossier publié, volume local, droit Modification du pool ; publication IIS hors V1 ; convention versée au harnais : plan §8 (« recommandations ok », DEB-Q55 précisée par le développeur, 21/11/2026). |
| DEB-Q48 à DEB-Q53 | Page Dialogue (F2) : `Home/Dialogue/{id}` (Guid, sinon 404) et « Nouvelle fiche » à l'accueil ; réducteur et vue purs, coquille DOM sans `innerHTML`, seules les coquilles `Page*.ts` hors couverture du front ; aucune règle métier dans le front (statut, `modifiable`, `complete`, case de confirmation) ; « Enregistrer » explicite par rubrique ; références `tour` et `consultation` partagées xUnit / Vitest ; actions selon le statut : plan §8 (« recommandations ok », 21/11/2026). |
| DEB-Q38 à DEB-Q44 | Front et API (F1) : découpage F1 / F2 Dialogue / F3 Liste ; Bootstrap seul ; lock et `.npmrc` ; IA scénarisée qui reprend les mots de l'agent ; 404 / 409 / 422 / 503, en-tête `X-Agent` ; contrat JSON partagé xUnit / Vitest ; `<vite-scripts />` testé sans exclusion : plan §8 (« recommandations ok », 21/11/2026). |
| DEB-Q12 | Persistance par `Microsoft.Data.Sqlite`, sans Entity Framework : fiche en JSON + colonnes de liste (« recommandation ok », 20/11/2026). |
| DEB-Q28 à DEB-Q32 | Infrastructure (I1) : JSON de la fiche par codes de rubrique et noms d'énumération, version 1 ; colonnes en ticks UTC, BLOB, entiers, tables `STRICT` ; référentiel fictif en code ; base hors dépôt (chemin révisé par DEB-Q54) ; composition dans `Program` au lot F1 : plan §8 (« recommandations ok », 20/11/2026). |
| DEB-Q21 à DEB-Q27 | Cas d'usage (B2) : historique du dialogue persisté à part de la fiche, tour atomique (refus de l'IA → rien d'écrit ; panne = exception), hors périmètre sans propositions, année du numéro en heure de Paris (fuseau en paramètre), auteur de la décision = valideur de l'en-tête, pas de version optimiste en V1, code de rubrique sans casse : plan §8 (« recommandations ok », 20/11/2026). |
| DEB-Q13 à DEB-Q20 | Règles du Domaine de la fiche (origines, ordre de questionnement, fusion, modification au seul Brouillon, contrôles de soumission, liste des décisions, tolérance de lecture de la réponse de l'IA, mutations sur copie jetable) : plan §8 (« recommandations ok », 20/11/2026). |

**Architecture** : clean architecture du modèle générique (Domain, Application sans E/S, Infrastructure, Web), règles
de dépendance vérifiées sur les `.csproj` par `ExpressionBesoin.Architecture.Tests` ; cas d'usage lire → décider
(pur, horloge en paramètre) → écrire ; `Resultat<T>` pour les refus métier ; ce qui relève de l'IA (comprendre,
reformuler, proposer) est séparé de l'automatisation déterministe (rubriques, fusion, complétude, statuts,
numérotation, document) : plan §2. Versions de test : xUnit 4 (4.0.3, MTP v2) depuis HY1 (DEB-Q46), tous les projets ensemble ; tests lancés par
l'exécutable, jamais par `dotnet test`.

## §4. Cartographie (HEAD fait foi)

```
harnais.config.psd1  README.md  .gitignore  .gitattributes  global.json (SDK 10.0.403, à la racine depuis HY3)
docs/            prompt-reprise-Demonstrateur.md  historique-Demonstrateur.md
specifications/  plan-demonstrateur.md
ExpressionBesoin/
  ExpressionBesoin.slnx  nuget.config  Directory.Build.props (garde VerifierSdk, HY3)  Directory.Packages.props
  .config/dotnet-tools.json  coverage-baseline.json  coverage-dette.txt
  src/ExpressionBesoin.Domain/          Resultat.cs (Refus, Resultat<T>, Resultat) ; ListeImmuable.cs (égalité structurelle) ;
                                        Fiches/ Catalogue, FicheBesoin (types), Fiche, Completude, Dialogue, Cycle,
                                        Numerotation, ReponseIA (B1), ContenuDocument (I2), Transitions (L1)
  src/ExpressionBesoin.Application/     ResultatOuvert.cs (aide des coquilles) ; Fiches/ Ports, Modeles, DecisionsFiches (pures),
                                        ServiceFiches (coquille) (B2)
  src/ExpressionBesoin.Infrastructure/  Fiches/ CodecFiche (pur, JSON v1), SchemaFiches (SQL, schéma 2, Planifier pur, L1), DepotFichesSqlite
                                        (coquille) ; Sqlite/ ConnexionsSqlite, OptionsSqlite ; Organisation/
                                        ReferentielOrganisationFictif (I1) ; Documents/ PlanFeb, GabaritFeb, MetadonneesFeb
                                        (purs), GenerateurDocumentFeb (adaptateur), FEB.docx (ressource embarquée) (I2)
  src/ExpressionBesoin.Infrastructure/IA/  ScenarioDialogue (pur), AssistantIAScenarise (adaptateur) (F1) ; PromptFiche,
                                        LimiteDebit (purs), GestionnaireLimiteDebit, OptionsIA, AssistantIAAlbert (A1)
  src/ExpressionBesoin.Web/             Program.cs ; Sante/DiagnosticSante.cs (pur), Sante/PointsSante.cs ;
                                        Hebergement/EnvironnementExtensions.cs, CompositionFiches.cs (F1), CompositionIA.cs (A1) ;
                                        Api/ ContratJson, Vues (projections pures), StatutHttp (pur), PointsFiches (F1) ;
                                        Vite/ ViteTags (pur), LecteurManifeste, ViteScriptsTagHelper (F1) ;
                                        Controllers/HomeController.cs (Index, Dialogue, Error) ; Views/ (Home/Dialogue, F2) ; Properties/launchSettings.json ;
                                        appsettings.json, appsettings.{Localhost,Staging,Production}.json (HY2) ;
                                        ExpressionBesoin.Web.csproj (cibles NpmInstall, ViteBuild,
                                        IncludeViteDist, ViteClean)
  src/ExpressionBesoin.Web/ClientApp/   package.json, package-lock.json, .npmrc, tsconfig*.json, vite.config.ts,
                                        vitest.config.ts ; src/main.ts (coquille) ; src/core/ Url, contrats, api,
                                        agentCourant, aiguillage, chemins, dates (L2), statuts (purs) ; src/features/liste/ vue (pur),
                                        PageListe (coquille) (F3) ; src/features/dialogue/ etat, vue (purs), PageDialogue (coquille) (F2 ; cycle de vie L2) ;
                                        src/styles/main.scss ; tests/ (Vitest : Url, agentCourant, api, contrats, dates,
                                        dialogueEtat, dialogueVue, listeVue) et tests/contrats/{fiche,consultation,tour,liste}-reference.json ;
                                        node_modules/ (DEB-D6)
  tests/Directory.Build.props
  tests/ExpressionBesoin.Domain.Tests/        ResultatTests.cs, ListeImmuableTests.cs ; Fiches/ Essais.cs (aides),
                                              Catalogue, Fiche, CompletudeDialogue, Cycle, Numerotation, ReponseIA, Transitions (Tests)
  tests/ExpressionBesoin.Application.Tests/   Fiches/ Essais.cs, Doublures.cs (ports en mémoire, attente différée en option),
                                              DecisionsFiches, Modeles, ServiceFiches (Tests)
  tests/ExpressionBesoin.Infrastructure.Tests/  Essais.cs (aides, BaseTemporaire : fichier au schéma de production) ;
                                              Fiches/ CodecFiche, SchemaFiches, DepotFichesSqlite ; Sqlite/ ; Organisation/ (Tests) ;
                                              Documents/ EssaisDocuments (attendus de la réplique Python), GabaritFeb, PlanFeb,
                                              MetadonneesFeb, GenerateurDocumentFeb (Tests)
  tests/ExpressionBesoin.Web.Tests/           DiagnosticSanteTests.cs, EnvironnementExtensionsTests.cs, SanteContratTests.cs,
                                              UsineWeb.cs (Production, base temporaire, horloge fixe ; UsineSansBase,
                                              UsinePanne), CompositionFichesTests.cs, ConfigurationEnvironnementsTests.cs (HY2) ; Api/ EssaisApi, FicheVue, StatutHttp,
                                              PointsFiches, ParcoursFiche (Tests) ; Vite/ ViteTags, LecteurManifeste,
                                              ViteScriptsTagHelper (Tests) (F1)
  tests/ExpressionBesoin.Infrastructure.Tests/IA/  ScenarioDialogueTests.cs (F1) ; FauxFournisseurIA (aide), PromptFiche,
                                              LimiteDebit, GestionnaireLimiteDebit, AssistantIAAlbert (Tests) (A1)
  tests/ExpressionBesoin.Architecture.Tests/  AnalyseCsproj.cs (pur), ReglesSolution.cs, SdkTests.cs (HY3),
                                              AnalyseCsprojTests.cs, SolutionTests.cs
```

## §5. Contrats

| Contrat | Forme | Figé par |
| --- | --- | --- |
| `GET api/sante` | `{ "etat": "ok", "version": "<informative>", "demarrage": "<ISO 8601>", "dureeSecondes": <entier ≥ 0> }`, exactement ces champs, dans cet ordre | `SanteContratTests`, `DiagnosticSanteTests` |
| Réponse de l'IA | plan §2 ; tolérances DEB-Q19 ; codes de rubrique de `Catalogue.cs` ; refus `reponseIA.jsonInvalide`, `reponseIA.formeInvalide` ; écarts `MotifEcart` | `ReponseIATests` (B1) |
| Codes de refus du Domaine | `fiche.nonModifiable`, `proposition.absente`, `cycle.transitionInterdite`, `soumission.{confirmationAbsente,incomplete,propositionsEnAttente}`, `decision.commentaireObligatoire`, `numerotation.{annee,illisible,anneeAnterieure,epuisee}` | tests de B1 |
| Stockage des fiches | JSON v1 (DEB-Q28) ; schéma SQLite `user_version = 2` (DEB-Q29, DEB-Q76 : migration depuis 1), tables `Fiches`, `Echanges`, `Transitions` (CHECK : six transitions du cycle) | `CodecFicheTests` (JSON de référence à l'octet), `DepotFichesSqliteTests`, `SchemaFichesTests` (I1) |
| API des fiches | plan §5 (route `reprendre` comprise) ; refus `fiche.introuvable`, `agent.inconnu`, `rubrique.inconnue`, `message.vide`, `document.nonValidee`, `decision.inconnue` ; 404 / 409 / 422, panne 503 `panne.technique` (DEB-Q42) ; en-tête `X-Agent` | tests de B2 ; `PointsFichesTests`, `ParcoursFicheTests`, `StatutHttpTests` (F1) |
| Cycle de vie | `Transitions.Entre` (statut inchangé → aucune), `EtapeDe` (six paires, sinon lève) ; consultation : `cycle` = `[{ etape, avant, apres, acteur, le }]` en dernier champ (DEB-Q77), lu strictement par le front (DEB-Q79) | `TransitionsTests`, `SchemaFichesTests` (contrainte = Domaine sur 30 paires), `ParcoursFicheTests` (L1) ; `contrats.test.ts` (L2) |
| JSON de l'API | projections de `Web/Api/Vues.cs`, camelCase, énumérations par leur nom, encodeur relâché (`ContratJson`) ; fiche : `id, numero, statut, enTete, rubriques, obligatoires, remplies, complete, modifiable, prochaineRubrique, confirmeeLe, decisions` | `FicheVueTests` (octets de `ClientApp/tests/contrats/{fiche,consultation,tour,liste}-reference.json`, réplique Python) et `contrats.test.ts` (Vitest) |
| Pages | `GET /` (accueil : « Nouvelle fiche » et liste, `data-page="liste"`, F3) ; `GET /Home/Dialogue/{id}` → `<div id="app" data-page="dialogue" data-fiche="{id}">`, 404 sans Guid non vide (DEB-Q48) | `PointsFichesTests` (F2) |
| IA scénarisée | réponse au contrat du plan §2 (`ScenarioDialogue`, DEB-Q41) | `ScenarioDialogueTests` (F1) |
| IA réelle | `POST {IA:Point}/chat/completions`, modèle `IA:Modele`, consignes système fixes, `response_format` `json_schema` strict `reponse_fiche` (DEB-Q85) ; 429 : DEB-Q87 | `PromptFicheTests`, `AssistantIAAlbertTests` (A1) |
| Document .docx | modèle FEB embarqué (empreinte figée) ; placement DEB-Q35 ; contenu DEB-Q36 ; octets stables DEB-Q37 | `ContenuDocumentTests`, `GabaritFebTests`, `GenerateurDocumentFebTests` (55 paragraphes attendus, I2) |

## §6. Reliquat, hygiène, leçons

Anomalies : aucune ouverte ; DEB-A1 close par F2, DEB-A2 close par HY3 (`global.json` à la racine, garde DEB0001) : historique. **Évolutions V2** (hors V1, DEB-D4 ; ouvertes seulement par décision du développeur) :
courriel au chef de service, habilitations, instruction XA / SICCRF, rapprochement de demandes, tableau de bord,
synthèse CODIR / COPIL, export PDF, version optimiste des fiches (DEB-Q26), journal technique Serilog structuré
(DEB-Q73) ; toute idée née pendant la V1 s'ajoute ici.

## §7. Prochaine session

Acquis : plan v1.19 ; socle P0, Domaine B1, Application B2, Infrastructure I1 et I2, front F1 à F3, hygiène HY1 à HY3,
cycle de vie L1 et L2 validés sur le poste (568 tests .NET, couverture 100 %, Vitest 249 ; schéma SQLite 2) ; accès à
Albert API sondé au poste (DEB-Q83, DEB-Q84). A1 livré (paquets `Microsoft.Agents.AI` 1.28.0 et
`Microsoft.Extensions.AI.OpenAI` 10.11.1, restaurés au poste depuis nuget.org).
Audit honnête : chez l'assistant, ni SDK .NET ni NuGet (C# analysé par tree-sitter, pas compilé ; API de MAF lues dans
sa documentation 1.13, de Microsoft.Extensions.AI 10.11.1, OpenAI 2.15.0 et System.ClientModel 1.16.0 dans leurs
sources) : `Appliquer-Lot A1` tranche. Étape « Validation » du panneau non vue au navigateur en L2 : à voir en R1.
Plan ordonné : A1 (appliquer, critère au navigateur), A1b, R1 (DEB-D4). Pièces à envoyer : toutes celles
d'`Ouvrir-Session` (8), avec leur compte, site arrêté ; la sortie d'`Appliquer-Lot A1` et le compte rendu du critère.
