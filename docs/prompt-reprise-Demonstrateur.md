# Prompt de reprise — Demonstrateur

> Mis à jour le 20/11/2026 par le lot P0a. Règles génériques : harnais central, `regles\regles-de-travail.md` (v5) ;
> modèle de projet : `regles\harnais-modele-generique.md` (v2) ; front : `regles\gabarit-frontend-vite-aspnet.md` ;
> outillage : `C:\Dev\Harnais\docs\specification-harnais-central.md` (fait foi).
> Historique détaillé : `docs/historique-Demonstrateur.md` (non envoyé par défaut). Documents hors dépôt dont dépend ce
> prompt (DEB-D2, tenus par le développeur, joints aux sessions qui en ont besoin) : cahier des charges
> `Expression_de_besoin_demonstrateur_IA_SICCRF.docx` (document de cadrage, novembre 2026) et modèle de fiche
> `FEB.docx`.

## ▶ ÉTAT AU 20/11/2026 (P0a) — À LIRE D'ABORD

**Où on en est** : P0a livré (dépôt `Documents`, plan v1.0 dans `specifications/plan-demonstrateur.md`) ; aucun code.
Préalable : lot OT11 du harnais central (inscription au registre de sauvegarde).
**Critère de fin de P0a** : OT11 commité dans le harnais ; dépôt amorcé dans `C:\Dev\Demonstrateur` ; sauvegardé ;
branche `main` publiée sur GitHub ; `Ouvrir-Session` conforme.
**Prochain pas** : lot P0 (socle .NET 10, passage au type `Projet`), puis P0b (comptes mesurés, base de couverture).
**Ouverture de la session suivante** :

    Set-Location C:\Dev\Demonstrateur; . C:\Dev\Harnais\outillage\Charger-Harnais.ps1; Ouvrir-Session -BrancheAttendue main
    # attendu : « SESSION PRÉPARÉE : 8 pièce(s) dans C:\Dev\Sessions\Demonstrateur\<horodatage> » ; joindre toutes les
    # pièces et donner leur compte.

**Décisions récentes** : DEB-D1, DEB-D2 ; arbitrages DEB-Q1 à DEB-Q8. **Anomalies ouvertes** : aucune. **Questions
ouvertes** : DEB-Q9 (modèle FEB versionné ?), DEB-Q10 (`node_modules` versionné ?).

## §0. Protocole (propre au projet)

| Emplacement | Valeur |
| --- | --- |
| Racine | `C:\Dev\Demonstrateur` ; solution `ExpressionBesoin/ExpressionBesoin.slnx` (à partir de P0) |
| Configuration | `harnais.config.psd1` (dépôt « Demonstrateur », type `Documents`, `HarnaisMinimal` 1.9.0) |
| Projets de tests | à partir de P0 : `ExpressionBesoin/tests/ExpressionBesoin.<Couche>.Tests`, `ExpressionBesoin.Architecture.Tests` |
| Couverture | à partir de P0 : `ExpressionBesoin/coverage-baseline.json`, `ExpressionBesoin/coverage-dette.txt` |
| Tests du front | à partir de F1 : `ExpressionBesoin/src/ExpressionBesoin.Web/ClientApp/tests` |
| Données | fictives, jamais réelles ; jeux d'essai hors dépôt |
| Dépôt distant | `https://github.com/falyanak/Demonstrateur.git`, **public**, branche `main` ; publication par le développeur seul (`Publier-Branche -Confirmer`) |
| Identifiants | `DEB-D…`, `DEB-A…`, `DEB-Q…` ; pris : DEB-D1, DEB-D2, DEB-Q1 à DEB-Q10 |

Ligne enchaînée du projet (zip portant `DEPOT.txt` = « Demonstrateur ») :

    Set-Location C:\Dev\Demonstrateur; . C:\Dev\Harnais\outillage\Charger-Harnais.ps1; Appliquer-Lot NOM; if ($?) { git add -A; git commit -m "NOM : …" }

## §1. Objectif et état

Démonstrateur d'un assistant conversationnel qui aide un agent à rédiger la fiche d'expression de besoin d'une
demande d'évolution du SI ; décision du chef de service simulée ; fiche validée produite en .docx au modèle FEB ;
page de liste des fiches et de leur statut. Plan, périmètre et critère de fin : `specifications/plan-demonstrateur.md`.

| Chantier | Lots | État |
| --- | --- | --- |
| P — socle | P0a, P0, P0b | P0a livré le 20/11/2026 |
| B — métier (Domain, Application) | B1, B2 | à faire |
| I — infrastructure | I1 (SQLite), I2 (.docx) | à faire |
| F — front | F1 (Dialogue), F2 (Liste) | à faire ; étape `TestsFront` attendue du lot OT2 du harnais |
| A — IA réelle | A1 | à faire ; fournisseur à trancher (DEB-Q2) |
| R — recette | R1 | à faire |

## §2. Points ouverts

1. Fournisseur d'IA (DEB-Q2) : à trancher avant A1 ; l'IA scénarisée suffit jusque-là.
2. DEB-Q9 et DEB-Q10 (plan §8), à trancher au plus tard à I2 et F1.
3. OT2 du harnais central (`TestsFront`) : sans lui, Vitest est lancé à la main pour F1 et F2.

## §3. Décisions ACTÉES — ne pas rouvrir

| N° | Décision |
| --- | --- |
| DEB-Q1 à DEB-Q8 | Arbitrages du 20/11/2026, « recommandations ok » : plan §8. |
| DEB-D1 | Dépôt `Demonstrateur` (nom du distant GitHub), racine `C:\Dev\Demonstrateur`, solution `ExpressionBesoin/`, préfixe `DEB` ; révise DEB-Q1 pour la racine (20/11/2026). |
| DEB-D2 | Dépôt public : aucun document interne versionné (cahier des charges, modèle FEB) tant que DEB-Q9 n'est pas tranchée ; le plan en tient lieu (20/11/2026). |

**Architecture** : clean architecture du modèle générique (Domain, Application sans E/S, Infrastructure, Web) ; cas
d'usage lire → décider (pur, horloge en paramètre) → écrire ; ce qui relève de l'IA (comprendre, reformuler,
proposer) est séparé de l'automatisation déterministe (rubriques, fusion, complétude, statuts, numérotation,
document) : plan §2.

## §4. Cartographie (HEAD fait foi)

```
harnais.config.psd1  README.md  .gitignore  .gitattributes
docs/            prompt-reprise-Demonstrateur.md  historique-Demonstrateur.md
specifications/  plan-demonstrateur.md
```

## §5. Contrats

À venir : réponse de l'IA (plan §2), API (plan §5). Chacun sera figé par ses tests au lot qui l'introduit.

## §6. Reliquat, hygiène, leçons

Aucune anomalie. Évolutions hors périmètre (plan §1) : courriel, habilitations, instruction XA / SICCRF,
rapprochement, tableau de bord, export PDF.

## §7. Prochaine session

Acquis : plan v1.0, dépôt amorcé. Plan ordonné : P0, P0b, B1, B2, I1, I2, F1, F2, A1, R1. Pièces à envoyer : toutes
celles d'`Ouvrir-Session` (8), avec leur compte ; pour I2, le modèle `FEB.docx` (hors dépôt, DEB-D2).
