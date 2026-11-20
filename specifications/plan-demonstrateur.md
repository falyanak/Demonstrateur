# Plan du démonstrateur « Expression de besoin » (version simplifiée)

> Version 1.2 du 20/11/2026 (lot P0c ; 1.1 au lot P0, 1.0 au lot P0a). Arbitrages DEB-Q1 à DEB-Q8, DEB-Q11, DEB-Q12 : « recommandations
> ok » (20/11/2026). Décisions :
> §8. Documents d'origine, **hors dépôt** (DEB-D2) : cahier des charges « Document de cadrage » (novembre 2026) et
> modèle de fiche d'expression de besoin (FEB), tenus par le développeur.

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
refus explicite, la fiche reste inchangée. Ce contrat est figé par des tests sur des réponses d'exemple.

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

Chaque champ porte `Valeur`, `Origine` (`Vide`, `ProposeeIA`, `AccepteeIA`, `Manuelle`) et au plus une
`PropositionEnAttente`. Une fiche est **complète** quand toutes les rubriques obligatoires ont une valeur acceptée ou
manuelle.

**Statuts** : `Brouillon → Soumise → Validee | ACompleter | Refusee` ; `ACompleter → Brouillon` (l'agent reprend la
fiche). Refus et demande de compléments exigent un commentaire. Une fiche `Validee` ou `Refusee` n'est plus modifiable.

## 4. Architecture (modèle générique du harnais central)

```
ExpressionBesoin/src/
  ExpressionBesoin.Domain          FicheBesoin, Rubrique, Champ, Statut, Resultat<T> ; fonctions pures :
                                   Fiche.AppliquerPropositions, Fiche.ModifierChamp, Completude.Manquantes,
                                   Dialogue.ProchaineRubrique, Cycle.Appliquer(fiche, evenement, maintenant),
                                   Numerotation.Suivant(annee, dernier), ReponseIA.Lire, ContenuDocument.Construire
  ExpressionBesoin.Application     cas d'usage lire -> décider -> écrire ; ports IDepotFiches, IAssistantIA,
                                   IGenerateurDocument, IReferentielOrganisation ; TimeProvider en paramètre
  ExpressionBesoin.Infrastructure  SQLite (fiche en JSON + colonnes de liste) ; OpenXML sur le gabarit FEB ;
                                   AssistantIA réel ; AssistantIA scénarisé (démonstration et tests hors réseau)
  ExpressionBesoin.Web             API minimale, deux vues Razor ; ClientApp Vite + TypeScript (features/dialogue,
                                   features/liste)
```

Performance :

- La liste lit des colonnes dénormalisées (numéro, intitulé, demandeur, statut, dates), sans désérialiser les fiches.
- Le prompt place en tête sa partie fixe (consignes, modèle de fiche) pour profiter du cache du fournisseur ; chaque
  tour n'ajoute que l'historique, l'état de la fiche et les rubriques manquantes.
- Le .docx est produit à la demande depuis la fiche validée, devenue immuable ; métadonnées du paquet fixées pour que
  la génération soit **déterministe** (testée à l'octet).

## 5. Contrat de l'API

| Verbe | Route | Rôle |
| --- | --- | --- |
| `POST` / `GET` | `api/fiches` | créer un brouillon ; lister (résumés) |
| `GET` | `api/fiches/{id}` | fiche complète |
| `POST` | `api/fiches/{id}/messages` | un tour de dialogue → `{ message, fiche }` |
| `PUT` | `api/fiches/{id}/champs/{rubrique}` | saisie manuelle |
| `POST` | `api/fiches/{id}/propositions/{rubrique}/accepter`, `…/rejeter` | décision sur une proposition |
| `POST` | `api/fiches/{id}/soumettre` | confirmation et soumission (contrôle de complétude) |
| `POST` | `api/fiches/{id}/decision` | simulation du chef de service `{ decision, commentaire }` |
| `GET` | `api/fiches/{id}/document` | .docx ; 409 si la fiche n'est pas validée |

Identité : sélecteur d'agent fictif dans l'en-tête, sans connexion (DEB-Q6) ; l'API reçoit l'agent courant par un
en-tête simple, jamais pris pour une autorisation.

## 6. Les deux pages

```
┌ Dialogue ───────────────────────────┬ Fiche EB-…  [Brouillon]  ▓▓▓▓▓░░ 11/13 ┐
│ Agent : La DGCCRF doit contrôler…   │ 2. Situation actuelle  [IA acceptée]  │
│ IA : Comment les contrôles se       │ 2. Difficultés         [Proposée ✓ ✗] │
│      déroulent-ils aujourd'hui ?    │ 3. Résultat recherché  [Manuelle]     │
│ [ saisie…                ] [Envoyer]│ ☐ Je confirme…   [Soumettre]          │
│                                     │ [Simuler la décision du chef ▾]       │
│                                     │ [Télécharger la fiche (.docx)]        │
└─────────────────────────────────────┴───────────────────────────────────────┘
```

Liste : N°, intitulé, demandeur, date, statut (badge), actions *Reprendre* et *Télécharger*. Tableau HTML simple
(quelques dizaines de lignes : AG Grid inutile). Réponse de l'IA affichée entière, avec indicateur d'attente
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
| F1 | Front (gabarit Vite) et page Dialogue | parcours complet avec l'IA scénarisée |
| F2 | Page Liste | statuts conformes après chaque transition |
| A1 | Adaptateur IA réel et prompt système (DEB-Q2) | scénario e-commerce joué avec l'IA réelle, sans invention |
| R1 | Fiche de recette : nominal et retour pour complément | critère de fin du démonstrateur (§1) : **point de terminaison de la V1** |

L'IA scénarisée précède l'IA réelle : la démonstration reste jouable sans réseau le jour du séminaire. Dépendance :
l'étape `TestsFront` d'`Appliquer-Lot` arrive avec le lot OT2 du harnais central ; sans lui, les tests Vitest de F1 et
F2 sont lancés à la main (`npm test`) et leur sortie jointe au compte rendu.

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
| DEB-D2 | Dépôt **public** : aucun document interne versionné (cahier des charges, modèle FEB) tant que DEB-Q9 n'est pas tranchée ; le présent plan en tient lieu. |

Questions ouvertes :

| N° | Question | Recommandation |
| --- | --- | --- |
| DEB-Q9 | Le modèle FEB vierge (gabarit du générateur .docx) peut-il être versionné dans ce dépôt public ? | Oui au lot I2 s'il ne contient aucune donnée (modèle de formulaire) ; sinon, gabarit lu hors dépôt par un chemin configuré |
| DEB-Q10 | `node_modules` versionné (gabarit F3) ? | Non : dépôt public, poste avec accès npm ; `package-lock.json` versionné, `npm ci` |
