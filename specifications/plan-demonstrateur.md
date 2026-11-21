# Plan du démonstrateur « Expression de besoin » (version simplifiée)

> Version 1.17 du 21/11/2026 (lot L1 ; 1.16 au lot F3b, 1.15 au lot F3, 1.14 au lot F2c, 1.13 au lot HY2, 1.12 au lot F2, 1.11 au lot HY1b, 1.10 au lot HY1, 1.9 au lot F1, 1.8 au lot I2b, 1.7 au lot I2, 1.6 au lot I1, 1.5 au lot B2, 1.4 au lot B1b, 1.3 au lot B1, 1.2 au lot P0c, 1.1 au
> lot P0, 1.0 au lot P0a). Arbitrages DEB-Q1 à DEB-Q8, DEB-Q11 à DEB-Q32 : « recommandations ok » (20/11/2026) ; DEB-Q9 :
> « ok » (20/11/2026) ; DEB-Q33 à DEB-Q37 : « recommandations ok » (21/11/2026) ;
> DEB-Q38 à DEB-Q44 : « recommandations ok » (21/11/2026) ; DEB-Q45, DEB-Q46 : « recommandations ok »
> (21/11/2026) ; DEB-Q47 : tranchée par le développeur (21/11/2026) ; DEB-Q48 à DEB-Q53 : « recommandations ok » (21/11/2026) ;
> DEB-Q54 à DEB-Q57 : « recommandations ok », DEB-Q55 précisée par le développeur (21/11/2026) ; DEB-Q58 : « maintenant,
> avant F3 » (21/11/2026) ; DEB-Q59 à DEB-Q63 : « recommandations ok » (21/11/2026) ; DEB-Q2, DEB-Q64 : « recommandations ok » (21/11/2026) ;
> DEB-Q65, DEB-Q66 : « recommandations ok », DEB-Q67 à DEB-Q72 rendues sans objet par DEB-Q73 ; DEB-Q73 à DEB-Q78 :
> « accord » du développeur (21/11/2026). Décisions : §8. Document d'origine **hors
> dépôt** (DEB-D2) : cahier des charges « Document de cadrage » (novembre 2026), tenu par le développeur ; le modèle de
> fiche (FEB) est versionné, nettoyé de ses métadonnées (DEB-Q9, DEB-Q33).

## 1. Objet et périmètre

Une application web de démonstration aide un agent à rédiger, par dialogue avec une IA, la fiche d'expression de
besoin d'une demande d'évolution du système d'information ; la décision du chef de service est simulée ; la fiche
validée est produite au format Word, au modèle FEB. Données fictives uniquement ; aucune décision réelle.

| Dans le périmètre | Hors périmètre (évolutions ultérieures) |
| --- | --- |
| Page **Dialogue** : conversation à gauche, fiche modifiable à droite | Courriel au chef de service, lien sécurisé |
| Propositions de l'IA soumises à acceptation ; une saisie manuelle n'est jamais écrasée | Authentification, habilitations par profil |
| Confirmation explicite de l'agent, puis soumission | Préqualification, instruction XA / SICCRF, avis RSSI et Data/IA |
| Bouton qui **simule la décision du chef de service** (valider, demander des compléments, refuser) | Rapprochement de demandes, tableau de bord, synthèse CODIR / COPIL |
| **Génération et téléchargement du .docx** au modèle FEB après validation | Export PDF |
| Page **Liste** des fiches avec leur statut | |

**Point de terminaison de la version 1 (V1 simplifiée, DEB-D4)** : la V1 s'arrête au lot R1, quand le critère
ci-dessous est prouvé sur le poste ; l'assistant le dit alors en clair (« objectif atteint, on arrête »). Son
périmètre est **gelé** : la colonne de droite du tableau ci-dessus, et toute idée née en cours de route, rejoignent
la liste des évolutions (prompt de reprise §6) et ne s'ouvrent qu'en V2, par décision du développeur, en chantier
propre avec son propre critère de fin. Un lot correctif de la V1 ne corrige que l'écart au critère.

**Critère de fin du démonstrateur (V1)** : sur le poste, le scénario du cahier des charges (§7, partie agent et chef de
service) est joué de bout en bout : la demande vague « contrôle des plates-formes e-commerce et prélèvements à
distance » devient une fiche complète sans information inventée ; l'agent corrige une rubrique, confirme et soumet ; la
validation est simulée ; le .docx s'ouvre dans Word au modèle FEB ; la liste affiche « Validée ». Variante jouée
aussi : retour pour complément, puis nouvelle soumission. Tests verts, couverture conforme ; dernier commit publié et
sauvegardé.

## 2. Partage IA / automatisation déterministe

| IA (adaptateur derrière le port `IAssistantIA`) | Déterministe (fonctions pures du Domaine) |
| --- | --- |
| Comprendre le texte libre, reformuler | Liste des rubriques, obligatoires ou facultatives |
| Formuler la question suivante | **Rubrique à questionner ensuite** (ordre fixe : première rubrique obligatoire vide) |
| Proposer une valeur de rubrique, avec sa justification | Fusion des propositions : rubrique manuelle jamais écrasée, proposition mise en attente |
| Signaler une demande hors périmètre (incident, assistance, équipement, habilitation) | Complétude, cycle des statuts, numéro `EB-AAAA-NNNN` attribué à la soumission |
| Alimenter « Points restant à clarifier » et le résumé | Contenu du .docx, à partir de la fiche |

Réponse de l'IA : **JSON structuré** `{ message, propositions: [{ rubrique, valeur, justification }], horsPerimetre }`,
lu par une fonction pure (`ReponseIA.Lire`) : rubrique inconnue → proposition écartée et signalée ; JSON invalide →
refus explicite, la fiche reste inchangée. Ce contrat est figé par des tests sur des réponses d'exemple ; ses règles de
tolérance sont fixées par DEB-Q19. Codes de rubrique (camelCase, `situationActuelle`…) : `Catalogue.cs` du Domaine.

## 3. Modèle de la fiche (d'après le modèle FEB)

| Bloc | Rubriques (* = obligatoire, DEB-Q4) | Renseignée par |
| --- | --- | --- |
| En-tête | N° (à la soumission), demandeur, date, bureau, valideur | Système : table fictive agent → service → chef de service |
| Identification | Application ou service concerné*, intitulé court* | Dialogue |
| 1. Synthèse | Résumé* | IA en fin de dialogue, à accepter |
| 2. Contexte | Situation actuelle*, difficultés rencontrées*, exemple représentatif | Dialogue |
| 3. Objectifs | Résultat recherché*, critères de réussite*, solution envisagée | Dialogue (la solution n'est pas le besoin) |
| 4. Périmètre | Utilisateurs ou services concernés*, processus métier*, liens avec d'autres outils ou demandes | Dialogue |
| 5. Bénéfices | Bénéfices attendus*, échéance et justification, contraintes identifiées | Dialogue |
| 6. Compléments | Documents ou références, points restant à clarifier | Agent ; IA |
| Confirmation | Case « je confirme… », date de confirmation | Agent (DEB-Q5) |
| Circuit | Décision du chef de service, auteur, date, commentaire | Simulation (DEB-Q3) |
| Cycle de vie | Une ligne par changement de statut : étape, statuts avant et après, acteur, date (DEB-Q74) | Système, à chaque écriture (DEB-Q75) |

Soit 17 rubriques, dont 10 obligatoires. Chaque champ porte `Valeur`, `Origine` (`Vide`, `AccepteeIA`, `Manuelle` ;
`ProposeeIA` retirée par DEB-Q13) et au plus une `PropositionEnAttente` ; « Proposée » = vide avec une proposition en
attente. Une fiche est **complète** quand toutes les rubriques obligatoires ont une valeur acceptée ou manuelle.

**Statuts** : `Brouillon → Soumise → Validee | ACompleter | Refusee` ; `ACompleter → Brouillon` (l'agent reprend la
fiche). Chaque changement de statut laisse une transition (étapes Création, Soumission, Validation, Demande de
compléments, Refus, Reprise), écrite dans la même transaction que la fiche (DEB-Q73 à DEB-Q76). Refus et demande de compléments exigent un commentaire. Seule une fiche `Brouillon` est modifiable (DEB-Q16) ; la
reprise d'une fiche `ACompleter` demande une nouvelle confirmation.

## 4. Architecture (modèle générique du harnais central)

```
ExpressionBesoin/src/
  ExpressionBesoin.Domain          FicheBesoin, Rubrique, Champ, Statut, Resultat<T> ; fonctions pures (B1) :
                                   Fiche.AppliquerPropositions, ModifierChamp, AccepterProposition, RejeterProposition,
                                   Completude.Manquantes, Dialogue.ProchaineRubrique, Cycle.Soumettre, Cycle.Decider,
                                   Cycle.Reprendre (une fonction par transition, horloge en paramètre),
                                   Numerotation.Suivant(annee, dernier), ReponseIA.Lire ; ContenuDocument.Construire
                                   (I2 : textes du .docx, dates dans le fuseau, dernière décision) ; Transitions.Entre,
                                   Transitions.EtapeDe (L1 : cycle de vie)
  ExpressionBesoin.Application     cas d'usage lire -> décider -> écrire (B2) : ServiceFiches (coquille, une méthode par
                                   route du §5), DecisionsFiches (pures) ; ports IDepotFiches (fiche + historique du
                                   dialogue, écrits ensemble), IAssistantIA (contexte du tour -> texte brut, relu par
                                   ReponseIA.Lire), IGenerateurDocument, IReferentielOrganisation ; TimeProvider, fuseau
                                   et générateur d'identifiants injectés
  ExpressionBesoin.Infrastructure  SQLite (fiche en JSON + colonnes de liste ; table Transitions, schéma 2, L1) ; .docx sur le gabarit FEB embarqué
                                   (zip + XML, sans SDK OpenXML, DEB-Q34 ; placement par libellé, DEB-Q35) ;
                                   AssistantIA réel ; AssistantIA scénarisé (démonstration et tests hors réseau)
  ExpressionBesoin.Web             API minimale, deux vues Razor ; ClientApp Vite + TypeScript (features/dialogue,
                                   features/liste)
```

Performance :

- La liste lit des colonnes dénormalisées (numéro, intitulé, demandeur, statut, dates), sans désérialiser les fiches.
- Le prompt place en tête sa partie fixe (consignes, modèle de fiche) pour profiter du cache du fournisseur ; chaque
  tour n'ajoute que l'historique, l'état de la fiche et les rubriques manquantes.
- Le .docx est produit à la demande depuis la fiche validée, devenue immuable ; métadonnées du paquet fixées pour que
  la génération soit **déterministe** (testée à l'octet, DEB-Q37). Gabarit lu, décompressé et vérifié une seule fois, au
  démarrage ; image recopiée sans recompression.

## 5. Contrat de l'API

| Verbe | Route | Rôle |
| --- | --- | --- |
| `POST` / `GET` | `api/fiches` | créer un brouillon ; lister (résumés) |
| `GET` | `api/agents` | agents fictifs du sélecteur de l'en-tête (DEB-Q6) |
| `GET` | `api/fiches/{id}` | fiche complète, historique du dialogue (DEB-Q21) et cycle de vie `cycle` (DEB-Q77, L1) |
| `POST` | `api/fiches/{id}/messages` | un tour de dialogue → `{ message, horsPerimetre, fiche }` (DEB-Q22, DEB-Q23) |
| `PUT` | `api/fiches/{id}/champs/{rubrique}` | saisie manuelle |
| `POST` | `api/fiches/{id}/propositions/{rubrique}/accepter`, `…/rejeter` | décision sur une proposition |
| `POST` | `api/fiches/{id}/soumettre` | confirmation et soumission (contrôle de complétude) |
| `POST` | `api/fiches/{id}/decision` | simulation du chef de service `{ decision, commentaire }` |
| `POST` | `api/fiches/{id}/reprendre` | reprise d'une fiche à compléter (`ACompleter → Brouillon`, DEB-Q16) ; ajoutée au lot F1, le cas d'usage existait |
| `GET` | `api/fiches/{id}/document` | .docx ; 409 si la fiche n'est pas validée |

Refus de l'Application (B2), en plus de ceux du Domaine : `fiche.introuvable`, `agent.inconnu`, `rubrique.inconnue`
(DEB-Q27), `message.vide`, `document.nonValidee` (409) ; du Web (F1) : `decision.inconnue`. Statuts (DEB-Q42) :
`*.introuvable` → 404, `document.nonValidee` → 409, tout autre refus → 422, panne d'un port → 503 `panne.technique` ;
corps d'erreur `{ code, message }`. Contrat JSON des réponses (DEB-Q43) : `ExpressionBesoin.Web/Api/Vues.cs`.

Identité : sélecteur d'agent fictif dans l'en-tête, sans connexion (DEB-Q6) ; l'API reçoit l'agent courant par l'en-tête
`X-Agent` (création d'une fiche), jamais pris pour une autorisation.

## 6. Les deux pages

```
┌ Dialogue ───────────────────────────┬ Fiche EB-…  [Brouillon]  ▓▓▓▓▓░░  7/10 ┐
│ Agent : La DGCCRF doit contrôler…   │ 2. Situation actuelle  [IA acceptée]  │
│ IA : Comment les contrôles se       │ 2. Difficultés         [Proposée ✓ ✗] │
│      déroulent-ils aujourd'hui ?    │ 3. Résultat recherché  [Manuelle]     │
│ [ saisie…                ] [Envoyer]│ ☐ Je confirme…   [Soumettre]          │
│                                     │ [Simuler la décision du chef ▾]       │
│                                     │ [Télécharger la fiche (.docx)]        │
└─────────────────────────────────────┴───────────────────────────────────────┘
```

Liste, à l'accueil (DEB-Q59) : N°, intitulé, demandeur, date de création, statut (badge), actions *Ouvrir* (page
Dialogue, tout statut) et *Télécharger* (fiche validée) (DEB-Q60). Tableau HTML simple (quelques dizaines de lignes :
AG Grid inutile). Réponse de l'IA affichée entière, avec indicateur d'attente
(DEB-Q7 : pas de flux SSE en v1).

## 7. Chantiers et lots

| Lot | Contenu | Critère de fin |
| --- | --- | --- |
| P0a | Dépôt `Documents` : configuration, prompt de reprise, historique, ce plan | amorcé, sauvegardé, publié sur GitHub |
| P0, P0b | Socle .NET 10 (`Resultat<T>`, `api/sante`, tests d'architecture) ; passage au type `Projet` | compilation et tests verts ; base de couverture relevée et commitée |
| B1 | Domaine : fiche, rubriques, fusion, complétude, prochaine rubrique, cycle, numérotation, `ReponseIA.Lire` | tables de vérité vertes ; mutations détectées |
| B2 | Application : cas d'usage, ports, doublures | cas d'usage testés sans E/S |
| I1 | SQLite, référentiel fictif agent → service → chef | tests d'intégration sur le schéma de production |
| I2 | Génération .docx sur le gabarit FEB (DEB-Q9) | octets stables ; fichier ouvert dans Word |
| F1 | Socle du front (gabarit Vite, DEB-D7), composition dans `Program`, routes du §5, IA scénarisée (DEB-Q38 à DEB-Q44) | `Appliquer-Lot` vert (tests, couverture) ; `npm test` vert ; `node_modules` commité ; parcours de l'API joué sur 5080 |
| HY1 | Hygiène DEB-HY1 : paquets NuGet, outils dotnet et SDK alignés sur DEB-D7 (DEB-Q45 à DEB-Q47) | `Appliquer-Lot` vert et commité (525 tests, couverture 100 / 100) ; `Compter-Tests` compte 525 à l'ouverture suivante |
| F2 | Page Dialogue (DEB-Q48 à DEB-Q53) | `Appliquer-Lot` vert ; `npm test` vert ; parcours complet avec l'IA scénarisée, dans le navigateur : création, dialogue jusqu'à 10/10, proposition acceptée et rejetée, correction manuelle, soumission, compléments, reprise, validation, .docx ouvert dans Word |
| HY2 | Hygiène DEB-HY2 : dossier de données fixé par environnement (DEB-Q54 à DEB-Q57) | `Appliquer-Lot HY2` vert et commité ; site lancé au profil `Localhost` : `C:\Dev\Demonstrateur-data\fiches.db` créé, `GET api/sante` « ok » |
| HY3 | Correction de DEB-A2 (DEB-Q58) : `global.json` à la racine du dépôt, garde du SDK dans `Directory.Build.props`, tests d'architecture ; précédé de F2c (racine de lot) | `Appliquer-Lot HY3` vert et commité ; `dotnet --version` rend 10.0.403 à la racine ; garde éprouvée sur une version forcée ; `Compter-Tests` à l'ouverture suivante |
| F3 | Page Liste à l'accueil (DEB-Q59 à DEB-Q63) | `Appliquer-Lot F3` vert ; `npm test` vert ; dans le navigateur, statut de la liste conforme après chaque transition : création (Brouillon), soumission (Soumise, numéro), compléments (À compléter), reprise (Brouillon), validation (Validée, « Télécharger »), refus (Refusée) |
| L1 | Cycle de vie, serveur (DEB-Q73 à DEB-Q78) : `Transitions` du Domaine, table SQLite et migration du schéma 1 au schéma 2, dépôt, service, champ `cycle` de `GET api/fiches/{id}` | `Appliquer-Lot L1` vert (tests, couverture 100 / 100) et commité ; base du poste migrée au schéma 2 au premier lancement (`Localhost`) ; parcours création → soumission → compléments → reprise → nouvelle soumission → validation : six transitions dans `GET api/fiches/{id}`, dans l'ordre, acteurs conformes ; un refus ajoute « Refus » |
| L2 | Cycle de vie, front : panneau « Cycle de vie » de la page Dialogue (vue pure, coquille sans `innerHTML`) | `npm test` vert ; dans le navigateur, le panneau montre le parcours de L1 et se met à jour après chaque action |
| A1 | Adaptateur IA réel et prompt système (DEB-Q2 : accès à Albert API vérifié à l'ouverture du lot, sinon API Mistral) | scénario e-commerce joué avec l'IA réelle, sans invention ; une demande d'assistance (« pc ne démarre plus », « copieur en panne ») signalée hors périmètre, sans proposition (DEB-Q64) |
| R1 | Fiche de recette : nominal et retour pour complément | critère de fin du démonstrateur (§1) : **point de terminaison de la V1** |

L'IA scénarisée précède l'IA réelle : la démonstration reste jouable sans réseau le jour du séminaire. Dépendance :
l'étape `TestsFront` d'`Appliquer-Lot` arrive avec le lot OT2 du harnais central ; sans lui, les tests Vitest de F1 à F3
sont lancés à la main (`npm test`) et leur sortie jointe au compte rendu.

## 8. Arbitrages et décisions

| N° | Arbitrage ou décision (20/11/2026) |
| --- | --- |
| DEB-Q1 | Solution `ExpressionBesoin`, préfixe `DEB` : accepté. Racine : voir DEB-D1. |
| DEB-Q2 | Fournisseur d'IA tranché avant A1 ; tout passe par le port `IAssistantIA` jusque-là. |
| DEB-Q3 | Simulation des trois décisions du chef de service, commentaire obligatoire pour « compléments » et « refus ». |
| DEB-Q4 | Rubriques obligatoires : celles marquées * au §3. |
| DEB-Q5 | Confirmation explicite de l'agent avant soumission (cahier des charges §3.2, case de la FEB). |
| DEB-Q6 | Identité : sélecteur d'agent fictif, sans connexion. |
| DEB-Q7 | Pas de flux SSE en v1 : réponse entière, indicateur d'attente. |
| DEB-Q8 | Démonstration sur le poste local, profil `Localhost` ; pas de livraison IIS. |
| DEB-D1 | Dépôt `Demonstrateur`, nommé comme le distant `https://github.com/falyanak/Demonstrateur.git` ; racine `C:\Dev\Demonstrateur` (au lieu de `C:\Dev\ExpressionBesoin` envisagé par DEB-Q1) ; solution dans le dossier `ExpressionBesoin/`. |
| DEB-Q11 | Paquets NuGet : `nuget.config` propre au projet, nuget.org seul. |
| DEB-Q12 | Persistance (I1) : `Microsoft.Data.Sqlite` sans Entity Framework ; une table de fiches, contenu en JSON, colonnes de liste. |
| DEB-D3 | Chaque réponse de l'assistant se termine par la commande `Ouvrir-Session` de la prochaine session, prête à copier (demande du développeur). |
| DEB-D4 | Point de terminaison de la V1 : lot R1, critère du §1 ; périmètre gelé, le reste en évolutions V2 (demande du développeur). |
| DEB-Q13 | Origine d'un champ : `Vide`, `AccepteeIA`, `Manuelle` ; une proposition vit toujours dans la proposition en attente ; « Proposée » se calcule (vide + proposition). |
| DEB-Q14 | Ordre de questionnement : obligatoires dans l'ordre du modèle, Résumé en dernier ; une rubrique vide avec une proposition en attente est sautée. |
| DEB-Q15 | Fusion : toute proposition va en attente (la nouvelle remplace l'ancienne), la valeur retenue n'est jamais touchée ; proposition égale à la valeur ignorée ; acceptation possible sur une rubrique manuelle ; saisie vide → `Vide` ; valeurs sans espaces de bord ; saisie égale à la proposition → proposition retirée. |
| DEB-Q16 | Seule une fiche `Brouillon` est modifiable ; `ACompleter` passe par la reprise, qui remet la confirmation à zéro. |
| DEB-Q17 | Soumission : fiche complète, confirmation cochée, aucune proposition en attente (même facultative) ; numéro attribué à la première soumission, gardé ensuite. |
| DEB-Q18 | Circuit : liste des décisions, on ne fait qu'ajouter ; le .docx lira la dernière. |
| DEB-Q19 | `ReponseIA.Lire` : codes de rubrique casse ignorée ; une seule clôture ```` ```json ```` tolérée ; `horsPerimetre` booléen ; justification obligatoire ; doublon → la première gardée. |
| DEB-Q20 | Mutations de B1 sur une copie jetable du poste (`%TEMP%`), par une commande fournie avec le lot. |
| DEB-D5 | Adaptateur IA (A1) : Microsoft Agent Framework (`ChatClientAgent`) sur un `IChatClient` compatible OpenAI, derrière `IAssistantIA` ; une réponse structurée par tour, relue par `ReponseIA.Lire` ; ni outils, ni workflows, ni multi-agents (« cadrage ok »). |
| DEB-D2 | Dépôt **public** : aucun document interne versionné (cahier des charges, modèle FEB) tant que DEB-Q9 n'est pas tranchée ; le présent plan en tient lieu. |
| DEB-Q21 | Historique du dialogue : `Echange(Auteur, Texte, Le)` de l'Application, persisté par `IDepotFiches` à part de la fiche, transmis en entier à l'IA ; une troncature éventuelle relève d'A1. |
| DEB-Q22 | Un tour est atomique : réponse de l'IA refusée par `ReponseIA.Lire` → rien n'est écrit (ni message, ni fiche) ; fiche et échanges écrits ensemble ; panne technique d'un port = exception propagée (Web : 503), pas un refus. |
| DEB-Q23 | `horsPerimetre = true` : message affiché, indicateur renvoyé, propositions du tour ignorées. |
| DEB-Q24 | Année du numéro en heure de Paris : fuseau en paramètre de la décision (composé au Web) ; l'instant stocké reste en UTC. |
| DEB-Q25 | Auteur de la décision simulée : le valideur de l'en-tête de la fiche, jamais l'agent courant. |
| DEB-Q26 | Pas de version optimiste en V1 ; unicité du numéro par index unique (I1) ; la version optimiste rejoint les évolutions V2. |
| DEB-Q27 | Code de rubrique des routes : casse ignorée (`Catalogue.EssayerTrouver`) ; inconnu → `rubrique.inconnue`. |
| DEB-Q28 | Contenu JSON de la fiche (I1) : rubriques indexées par leur code du `Catalogue` (jamais par l'ordinal), énumérations par leur nom exact, instants ISO 8601, version `v: 1` ; propriétés nulles et champs initiaux omis ; contenu illisible = panne (`InvalidDataException`), jamais un refus. |
| DEB-Q29 | Colonnes SQLite (I1) : instants en `INTEGER` (ticks UTC), identifiant en `BLOB` de 16 octets, statut en entier ; tables `STRICT`, historique `WITHOUT ROWID` par (fiche, rang). |
| DEB-Q30 | Référentiel fictif agent → bureau → chef de service : en code (`ReferentielOrganisationFictif`), sans table. |
| DEB-Q31 | Fichier de la base : hors dépôt, `%LOCALAPPDATA%\Demonstrateur\fiches.db`, chemin lu dans `appsettings.Localhost.json` (posé au lot F1, avec la composition) ; `*.db` ignoré par git. |
| DEB-Q32 | Composition des adaptateurs dans `Program` (enregistrement, `Initialiser` au démarrage, création du dossier) : au lot F1, avec les routes ; I1 se limite à l'Infrastructure et à ses tests. |
| DEB-Q9 | Le modèle FEB vierge est versionné (« ok », 20/11/2026), à la condition qu'il ne porte aucune donnée : voir DEB-Q33. |
| DEB-Q33 | (21/11/2026, « recommandations ok ») Gabarit versionné = modèle FEB reçu le 20/11/2026, **métadonnées nettoyées** : nom réel du dernier modificateur et titre d'origine (`core.xml`), modèle d'origine (`app.xml`), lien vers un `.dotx` du profil Windows d'un autre agent (`settings.xml` et sa relation) ; corps, en-tête, pied et logo inchangés. Ressource embarquée de l'Infrastructure ; empreinte figée par un test, absence de métadonnée personnelle vérifiée par un test. |
| DEB-Q34 | (21/11/2026, « recommandations ok ») .docx produit par `System.IO.Compression` et `System.Xml.Linq`, sans SDK OpenXML : aucun paquet ajouté, octets entièrement maîtrisés. |
| DEB-Q35 | (21/11/2026, « recommandations ok ») Placement par libellé : le modèle n'a ni signet ni contrôle de contenu ; un paragraphe est repéré par le début de son premier run (libellé en gras → le texte remplace l'aide ; ligne d'en-tête → réécrite avec son préfixe) ; ligne « Chef de service » du circuit : décision et observations ; case ☒ si la fiche est confirmée ; saut de ligne → retour à la ligne Word ; caractères interdits en XML retirés. Gabarit écarté du plan → panne au démarrage (`InvalidDataException`), jamais au téléchargement. |
| DEB-Q36 | (21/11/2026, « recommandations ok ») `ContenuDocument.Construire(fiche, fuseau)` (Domaine) : dates jj/mm/aaaa dans le fuseau du numéro (DEB-Q24), culture invariante ; rubrique sans valeur retenue → « Non renseigné » ; décision = la dernière (DEB-Q18), observations « auteur, le date : commentaire ». Le port `IGenerateurDocument` reçoit ce contenu, plus la fiche. |
| DEB-Q37 | (21/11/2026, « recommandations ok ») Octets stables : parties recopiées dans l'ordre du gabarit, horodatage fixe des entrées (01/01/1980), métadonnées tirées du contenu (création de la fiche, dernière décision), aucune lecture d'horloge ; vérifié par deux générations identiques à l'octet, culture indifférente. |
| DEB-D6 | (21/11/2026, révise la recommandation de DEB-Q10) `ClientApp/node_modules` **versionné** : build hors ligne, binaires Windows x64 seulement ; dossiers `bin` ré-autorisés au `.gitignore`, caches `.vite`, `.tmp` ignorés ; octets tels que publiés (`-text` au `.gitattributes` : sans lui, un clone Windows réécrit 1 836 fichiers sur 2 233) ; `npm ci` seulement s'il manque. |
| DEB-D7 | (21/11/2026, demande du développeur) **Principe des versions** : dernière version STABLE de chaque outil et bibliothèque, dernière LTS des environnements d'exécution, épinglées exactement (lock, `save-exact`, `Directory.Packages.props`) ; prime sur la marge de 14 jours du modèle générique. Front : Node.js 26.13 LTS (`engines ^26.13.0`, 26.13.1 installé : correctif de sécurité du 11/11/2026) et son npm, TypeScript 7.0.2, Vite 8.3.8, Vitest 5.1.1, Sass 1.108.2, Bootstrap 5.3.9. .NET 10 (LTS) inchangé ; paquets NuGet et outils dotnet alignés par un lot dédié (DEB-HY1). |
| DEB-Q38 | (21/11/2026, « recommandations ok ») Découpage : F1 socle du front, composition, routes, IA scénarisée ; F2 page Dialogue ; F3 page Liste. |
| DEB-Q39 | (21/11/2026) Bibliothèques du front : `bootstrap` et `@popperjs/core` seulement (Sass) ; ni AG Grid, ni `xlsx`, ni Chart.js, ni htmx, ni jQuery. |
| DEB-Q40 | (21/11/2026) npm : lock livré par le lot, `ClientApp/.npmrc` (`ignore-scripts`, `engine-strict`, `save-exact`) ; cible `NpmInstall` = `npm ci`. |
| DEB-Q41 | (21/11/2026) IA scénarisée (`ScenarioDialogue`, Infrastructure, pure) : reprend les mots de l'agent pour la rubrique questionnée (la demande initiale pour l'intitulé), résumé = rubriques retenues, hors périmètre par mots-clés ; adaptateur par défaut jusqu'à A1. |
| DEB-Q42 | (21/11/2026) Refus → HTTP : 404, 409, 422, panne 503 (§5) ; agent courant par l'en-tête `X-Agent`. |
| DEB-Q43 | (21/11/2026) Contrat JSON : projections pures du Web, camelCase, énumérations par leur nom ; même fichier de référence testé à l'octet par xUnit et lu par Vitest. |
| DEB-Q44 | (21/11/2026) `<vite-scripts />` sans exclusion de couverture : `ViteTags` pur (table de vérité), coquille testée par ses E/S réelles. |
| DEB-Q45 | (21/11/2026, « recommandations ok ») .NET 11 (paru le 10/11/2026, non LTS) écarté : `net10.0` gardé (DEB-D7) ; paquets liés au framework (`Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.Data.Sqlite`) sur la dernière 10.0.x (10.0.15) ; Mvc.Testing 11 ne cible que net11.0, Sqlite 10.0.15 reste sur SQLitePCLRaw 2.1. |
| DEB-Q46 | (21/11/2026, « recommandations ok ») xUnit 4 (`xunit.v3` 4.0.3, Microsoft Testing Platform v2 par défaut), `xunit.runner.visualstudio` 4.0.1, `Microsoft.NET.Test.Sdk` 18.12.0, tous les projets ensemble ; révise « xUnit v3 » ; l'essai est `Appliquer-Lot HY1`, un échec retient le commit. Outils : dotnet-coverage 18.12.0, ReportGenerator 5.6.0. |
| DEB-Q47 | (21/11/2026, tranchée par le développeur, contre la recommandation « 10.0.404 minimum, `latestFeature` ») SDK **bloqué** sur 10.0.403 (`global.json`, `rollForward: disable`) pour la conformité de la chaîne CI/CD ; .NET 11 installé sur le poste, non utilisé. Écart au modèle générique (§4.1 : 10.0.100, `latestFeature`) acté ici. |
| DEB-Q48 | (21/11/2026, « recommandations ok ») Adresse de la page Dialogue (F2) : action `Home/Dialogue/{id}` (route par défaut, `id` Guid ; autre valeur → 404) rendant `<div id="app" data-page="dialogue" data-fiche="{id}">` ; bouton « Nouvelle fiche » de l'accueil : `POST api/fiches` (agent courant), puis navigation vers `Url.resolve("Home/Dialogue/{id}")` ; *Reprendre* de la Liste (F3) y mènera ; fiche inconnue → message `fiche.introuvable` dans la page |
| DEB-Q49 | (21/11/2026, « recommandations ok ») Découpage du front de la page : `features/dialogue/` : `etat.ts` (réducteur PUR état × événement → état : une requête à la fois, brouillons de saisie gardés par rubrique, dernière erreur), `vue.ts` (PUR : fiche → modèle d'affichage par bloc, badges d'origine, progression, actions permises), `PageDialogue.ts` (coquille DOM sans décision, `textContent` seulement, jamais `innerHTML`) ; exclusion de couverture du front `src/features/**` remplacée par `src/features/**/Page*.ts` (coquilles seules) ; aucune dépendance ajoutée (ni jsdom ni happy-dom : `node_modules` inchangé), la coquille est éprouvée par le parcours du navigateur |
| DEB-Q50 | (21/11/2026, « recommandations ok ») Règles métier côté front : aucune recopiée : les boutons ne dépendent que des champs calculés par le Domaine (`statut`, `modifiable`, `complete`) et de la case de confirmation ; complétude, propositions en attente, commentaire obligatoire restent arbitrés par le serveur, dont le refus `{ code, message }` est affiché tel quel |
| DEB-Q51 | (21/11/2026, « recommandations ok ») Saisie manuelle d'une rubrique : zone de texte par rubrique et bouton « Enregistrer » actif quand le brouillon diffère de la valeur retenue (`PUT …/champs/{code}`) ; pas d'enregistrement automatique à la sortie du champ (chaque saisie manuelle est un acte explicite, DEB-Q15) ; une proposition en attente s'affiche sous la zone avec ✓ (accepter) et ✗ (rejeter) |
| DEB-Q52 | (21/11/2026, « recommandations ok ») Contrat partagé des nouvelles réponses : `tour-reference.json` (`POST …/messages`) et `consultation-reference.json` (`GET api/fiches/{id}`) ajoutés à `ClientApp/tests/contrats/`, figés à l'octet par xUnit (comme `fiche-reference.json`) et lus par Vitest (`lireTour`, `lireConsultation`) |
| DEB-Q53 | (21/11/2026, « recommandations ok ») Actions selon le statut : `Brouillon` : dialogue, saisie, propositions, case « Je confirme… » et Soumettre ; `Soumise` : simulation de la décision (Valider, Demander des compléments, Refuser ; commentaire) ; `ACompleter` : Reprendre ; `Validee` : lien de téléchargement du .docx (`Url.resolve`) ; réponse hors périmètre en alerte ; propositions écartées (`ecartees`) en note discrète sous la réponse de l'IA |
| DEB-Q54 | (21/11/2026, « recommandations ok » ; révise DEB-Q31 pour le chemin) Dossier de données du poste : `C:\Dev\Demonstrateur-data\fiches.db` (nom du dépôt + `-data`), voisin du dépôt, jamais dedans ; l'ancienne base de `%LOCALAPPDATA%\Demonstrateur` n'est pas reprise (données fictives, à supprimer à la main) |
| DEB-Q55 | (21/11/2026, précisée par le développeur : « fixer les chemins du dossier de données pour chaque environnement ») Un fichier par environnement porte `Fiches:Base` : `appsettings.Localhost.json` (poste), `appsettings.Staging.json` (`D:\Donnees\Demonstrateur-data-staging\fiches.db`), `appsettings.Production.json` (`D:\Donnees\Demonstrateur-data\fiches.db`) ; chemins serveur distincts (recette et production peuvent cohabiter), à confirmer avec l'exploitation ; la variable d'environnement `Fiches__Base` les remplace si besoin (priorité .NET) ; `appsettings.json` n'en porte aucun : environnement sans valeur → arrêt au démarrage, jamais d'emplacement implicite. Sur le serveur : dossier hors du dossier publié, volume local (jamais un partage réseau : SQLite), droit Modification à l'identité du pool. Garde-fou : `ConfigurationEnvironnementsTests` |
| DEB-Q56 | (21/11/2026, « recommandations ok ») La publication sur IIS reste hors V1 (DEB-Q8, DEB-D4) : les fichiers Staging et Production sont prêts, la mise en œuvre de la livraison est une évolution V2 |
| DEB-Q57 | (21/11/2026, « recommandations ok ») Convention générique « dossier de données `C:\Dev\<Depot>-data` sur le poste, chemin par environnement » : versée au modèle générique du harnais (§3, §10) par un lot du harnais central, pas par ce projet |
| DEB-Q58 | (21/11/2026, « maintenant, avant F3 », recommandation retenue) Anomalie DEB-A2 : `dotnet` cherche `global.json` depuis le dossier courant ; lancées depuis la racine du dépôt, les commandes du harnais ne voient pas `ExpressionBesoin/global.json` et compilent avec le SDK 11.0.100 (mesuré le 21/11/2026). Correction : `global.json` déplacé à la racine du dépôt (un seul fichier ; Visual Studio le trouve aussi, en remontant depuis la solution) ; `Lot.Racines` étendu d'abord (F2c), le déplacement ensuite (HY3) ; garde au build : erreur si `NETCoreSdkVersion` diffère de la version attendue ; tests d'architecture qui lient les deux. DEB-Q47 inchangée |
| DEB-Q59 | (21/11/2026, « recommandations ok ») Page Liste (F3) à l'accueil (`GET /`, `data-page="liste"`), sous « Nouvelle fiche » : aucune route nouvelle ; `features/accueil` remplacé par `features/liste` |
| DEB-Q60 | (21/11/2026, « recommandations ok » ; précise DEB-Q48 sur le nom de l'action) Action de la liste « **Ouvrir** » (lien vers `Home/Dialogue/{id}`, tout statut), et non « Reprendre », qui désigne la transition `ACompleter → Brouillon` (un mot = une chose) ; « Télécharger » pour une fiche `Validee` seulement |
| DEB-Q61 | (21/11/2026, « recommandations ok ») Découpage comme F2 : `lireResumes` (`contrats.ts`), `client.lister()`, `features/liste/vue.ts` PUR (lignes : numéro ou « — », intitulé ou « (sans intitulé) », badge, actions), coquille `PageListe.ts` sans `innerHTML` ; badge du statut déplacé dans `core/statuts.ts` (commun aux deux pages) ; référence `liste-reference.json` figée à l'octet par xUnit, lue par Vitest ; aucune dépendance ajoutée |
| DEB-Q62 | (21/11/2026, « recommandations ok ») Date affichée : création, jj/mm/aaaa à l'heure de Paris (comme DEB-Q24, DEB-Q36), fuseau en paramètre de la fonction pure |
| DEB-Q63 | (21/11/2026, « recommandations ok ») Toutes les fiches, sans filtre par agent, ordre du serveur (plus récente d'abord), sans pagination ; rechargement à l'affichage de la page, retour arrière compris (`pageshow`), sans interrogation périodique |

| DEB-Q2 | (21/11/2026, « recommandations ok » ; tranche la question ouverte au 20/11/2026) Fournisseur de l'IA réelle (A1, technologie DEB-D5) : **Albert API** (DINUM : hébergement SecNumCloud, modèles Mistral) si le ministère y a accès, sinon **API Mistral** ; l'accès est vérifié à l'ouverture d'A1, avant tout code ; sortie contrainte par schéma vérifiée en A1 ; repli hors réseau : IA scénarisée (démonstration jouable sans réseau) |
| DEB-Q64 | (21/11/2026, « recommandations ok ») Observation du parcours de F3 : « pc ne démarre plus » (aucun mot de la liste de DEB-Q41) et « copieur en panne » (contient « panne » : signalée au tour concerné ; la fiche a pu être remplie à la main ensuite, non vérifié) ont été validées. Limite connue de l'IA scénarisée, pas un défaut de F3 : aucune correction de `ScenarioDialogue` ; la détection du hors périmètre entre dans le critère de fin d'A1, jugée sur l'IA réelle. Le système n'interdit pas de remplir à la main une demande d'assistance : la décision du chef de service reste le filtre (V1) |

| DEB-Q65 | (21/11/2026, « recommandations ok ») Suivi des états d'une demande : chantier **L** ouvert avant A1, par décision du développeur (exception au gel du périmètre, DEB-D4) |
| DEB-Q66 | (21/11/2026, « recommandations ok ») Aucun texte libre dans la trace des états : codes, statuts, acteur, instant ; le commentaire d'une décision reste dans les décisions de la fiche |
| DEB-Q67 à DEB-Q72 | (21/11/2026) Journal Serilog (frontière, dossier par environnement, configuration en code, écriture, Seq, versions) : sans objet, Serilog écarté par DEB-Q73 |
| DEB-Q73 | (21/11/2026, accord du développeur) Le cycle de vie est une donnée MÉTIER : table SQLite `Transitions`, écrite dans la transaction de la fiche ; Serilog (observabilité technique) rejoint les évolutions V2, ou A1 par une nouvelle décision si le diagnostic de l'IA réelle l'exige |
| DEB-Q74 | (21/11/2026, accord) Ligne : `FicheId`, `Rang`, `Le` (ticks UTC), `Avant` (NULL à la création), `Apres`, `Acteur` (demandeur ; valideur pour une décision, DEB-Q25) ; `STRICT`, `WITHOUT ROWID`, clé (fiche, rang) ; contrainte CHECK limitée aux six transitions du cycle ; l'étape (Creation, Soumission, Validation, DemandeComplements, Refus, Reprise) se calcule, elle n'est pas stockée |
| DEB-Q75 | (21/11/2026, accord) Transition déduite par la fonction pure `Transitions.Entre(avant, apres, instant)` du Domaine (`null` si le statut ne change pas) ; `Cycle` inchangé ; `IDepotFiches.Enregistrer` reçoit fiche, échanges et transition en une transaction ; une lecture d'horloge par cas d'usage date la fiche et sa transition |
| DEB-Q76 | (21/11/2026, accord) Migration au démarrage, en transaction : schéma 0 → création (WAL), 1 → ajout de la table, 2 → rien, autre → arrêt (`SchemaFiches.Planifier`, pur) ; aucune reconstitution : le cycle de vie d'une fiche existante commence à la migration |
| DEB-Q77 | (21/11/2026, accord) Exposition : champ `cycle` en fin de `GET api/fiches/{id}` (`etape, avant, apres, acteur, le`), référence `consultation-reference.json` figée à l'octet par xUnit et lue par Vitest ; panneau « Cycle de vie » de la page Dialogue |
| DEB-Q78 | (21/11/2026, accord) Deux lots : L1 serveur, L2 front ; L2 bâti après la vérification de L1 |

Questions ouvertes : aucune.
