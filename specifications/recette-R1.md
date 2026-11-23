# Fiche de recette R1 — Demonstrateur (point de terminaison de la V1)

> Version 1 du 23/11/2026 (lot R1). Critère prouvé : plan §1, « Critère de fin du démonstrateur (V1) ». Décisions :
> DEB-Q124 (site journalisé, DEB-A6), DEB-Q135 (DEB-A7 en V2 : un message est dicté ENTIER), DEB-Q136 (proposition
> inventée rejetée), DEB-Q137 (réponses de l'agent écrites ci-dessous). Données fictives seulement : agents du
> référentiel fictif, faits inventés pour la recette, aucune donnée réelle.
> Les étapes s'enchaînent dans l'ordre : chacune part de l'état laissé par la précédente. Résultats : une ligne par
> étape (OK / KO, observation), rapportée par le développeur et versée à l'historique par le lot R1b.

## 0. Préalables (poste, PowerShell)

| N° | Action | Attendu |
| --- | --- | --- |
| R0.1 | Lot R1 appliqué et commité (ligne enchaînée de la réponse) | « LOT APPLIQUÉ », commit créé |
| R0.2 | Console 1 : commande R0.2 ci-dessous | Vite écoute sur 5173 |
| R0.3 | Console 2, NOUVELLE (profil `Albert` de `appsettings.Localhost.json` ; aucune variable `IA__Profil`) : commande R0.3 ci-dessous | site sur 5080, aucune erreur au démarrage |
| R0.4 | Navigateur (Edge ou Chrome, micro branché) : `http://localhost:5080/api/sante` | `"etat":"ok"` |
| R0.5 | `http://localhost:5080/` | accueil : « Nouvelle fiche » et la liste ; les fiches des lots précédents restent, on n'y touche pas |

Commande R0.2 :

    Set-Location C:\Dev\Demonstrateur\ExpressionBesoin\src\ExpressionBesoin.Web\ClientApp; npm run dev

Commande R0.3 (journal du site, DEB-Q124) :

    Set-Location C:\Dev\Demonstrateur\ExpressionBesoin\src\ExpressionBesoin.Web; dotnet run --launch-profile Localhost 2>&1 | Tee-Object -FilePath C:\Dev\Demonstrateur-data\site-R1.log

## 1. Réponses de l'agent (DEB-Q137)

Chaque question de l'assistant commence par le libellé de sa rubrique (« Critères de réussite : … ? », DEB-Q118).
L'agent répond par le texte de la ligne de CETTE rubrique, copié tel quel, une seule réponse par tour. Une rubrique
déjà proposée n'est plus questionnée : sa ligne ne sert pas. Ces textes sont les SEULS faits de la demande : toute
valeur qui en contient un autre est une invention (§3).

**Premier message** (scénario A : dicté ; scénario B : tapé) :

> J'ai besoin d'aide pour le contrôle des plates-formes e-commerce et les prélèvements à distance.

| Rubrique questionnée | Réponse de l'agent |
| --- | --- |
| Application ou service concerné | Aucune application dédiée : nous utilisons un tableur partagé et la messagerie. |
| Intitulé court | Suivi des contrôles des plates-formes e-commerce |
| Situation actuelle | Chaque enquêteur note ses contrôles de sites marchands dans un tableur partagé ; les captures d'écran et les preuves des prélèvements à distance (achats tests) sont rangées à la main dans un dossier réseau. |
| Difficultés rencontrées | Le tableur est modifié par plusieurs personnes à la fois, des lignes sont écrasées, et on retrouve mal les preuves d'un achat test quand le dossier part au contentieux. |
| Exemple représentatif | En octobre, les captures d'un achat test sur une place de marché ont été retrouvées trois semaines après la demande du service contentieux. |
| Résultat recherché | Un registre unique des contrôles, où chaque achat test est rattaché à ses preuves : captures, factures, colis reçus. |
| Critères de réussite | Retrouver toutes les preuves d'un contrôle en moins de cinq minutes, et plus aucune ligne écrasée. |
| Solution envisagée | Je n'ai pas de solution en tête. |
| Utilisateurs ou services concernés | Les 12 enquêteurs du bureau 1A, et le service contentieux en consultation. |
| Processus métier | Le contrôle des sites marchands : choix du site, achat test, réception du colis, constat, puis suites contentieuses. |
| Liens avec d'autres outils ou demandes | Aucun lien connu. |
| Bénéfices attendus | Moins de temps perdu à chercher les preuves, et des dossiers contentieux complets du premier coup. |
| Échéance et justification | Avant la campagne de contrôle de mars 2027, qui doit utiliser le registre. |
| Contraintes identifiées | Les preuves doivent rester sur le réseau du ministère. |
| Documents ou références | Aucun. |

Relance « Toutes les rubriques obligatoires sont renseignées : envoyez « résumé »… » : l'agent envoie `résumé`.

## 2. Scénario A — nominal, agent Camille Martin (valideur Dominique Bernard)

| N° | Action | Attendu |
| --- | --- | --- |
| A1 | En-tête : agent « Camille Martin » ; « Nouvelle fiche » | page Dialogue, badge « Brouillon », 0/10 ; boutons « 🎙 Dicter » présents (`data-dictee="oui"`) |
| A2 | Zone « Votre message » : « 🎙 Dicter », lire le premier message (§1) d'un trait, second clic | « Transcription… », puis le texte inséré dans la zone ; rien n'est envoyé de lui-même |
| A3 | Relire le texte dicté (corriger à la main un mot mal transcrit s'il y en a un, et le noter), « Envoyer » | réponse de l'assistant : prestation du service informatique nommée, aucune alerte hors périmètre ; une question précédée du libellé d'une rubrique |
| A4 | Répondre à chaque question par la ligne du §1 de la rubrique questionnée ; à chaque tour, accepter ✓ les propositions conformes au §1 ; une proposition qui porte un fait absent du §1 est rejetée ✗ et notée (DEB-Q136) | les propositions passent en « IA acceptée » ; progression croissante ; aucune question sur une rubrique déjà proposée ou renseignée |
| A5 | À la relance du résumé, envoyer `résumé` ; REJETER ✗ la première proposition de résumé ; renvoyer `résumé` ; accepter ✓ la seconde si elle est conforme au §3 | rejet : rubrique « Résumé » de nouveau vide ; puis 10/10, « Rubriques obligatoires renseignées » |
| A6 | Correction de l'agent : rubrique « Utilisateurs ou services concernés », remplacer le texte par « Les 12 enquêteurs du bureau 1A et les 3 juristes du service contentieux, en consultation. », « Enregistrer » | badge « Manuelle », valeur corrigée affichée |
| A7 | Accepter ou rejeter toute proposition encore en attente (facultatives comprises : DEB-Q17) | aucune proposition en attente |
| A8 | Contrôle « sans information inventée » : grille du §3 sur cette fiche | 10 lignes OK |
| A9 | Cocher « Je confirme que le contenu de la fiche reflète fidèlement mon besoin. », « Soumettre » | badge « Soumise », numéro `EB-2026-NNNN` affiché (le noter) ; panneau « Cycle de vie » : Création, Soumission |
| A10 | Décision : « Valider », commentaire vide, « Décider » | badge « Validée » ; « Télécharger la fiche (.docx) » ; cycle : 3 transitions, Validation par Dominique Bernard |
| A11 | « Télécharger la fiche (.docx) », ouvrir le fichier dans Word | ouverture sans message de réparation ; modèle FEB (logo, en-tête, blocs 1 à 6) ; numéro, demandeur Camille Martin, Bureau 1A – Commerce électronique, valideur ; rubriques retenues, « Utilisateurs » corrigée (A6) ; case de confirmation cochée ; décision « Validée » du chef de service |
| A12 | Accueil (`http://localhost:5080/`) | ligne de la fiche : numéro, « Suivi des contrôles… » (ou l'intitulé retenu), Camille Martin, badge « Validée », « Ouvrir » et « Télécharger » |

## 3. Grille « sans information inventée » (DEB-Q136)

Pour chaque rubrique obligatoire, la valeur retenue doit se ramener aux réponses du §1 (premier message compris) :
reformulation permise, aucun fait ajouté (nombre, date, nom d'outil, d'application, de service ou de personne, cause,
conséquence). Pour le résumé : aucun fait absent des autres rubriques retenues.

| Rubrique | Source attendue au §1 | Verdict (OK / KO) | Fait ajouté, s'il y en a |
| --- | --- | --- | --- |
| Application ou service concerné | ligne « Application… » (tableur partagé, messagerie) | | |
| Intitulé court | ligne « Intitulé court » ou premier message | | |
| Résumé | rubriques retenues | | |
| Situation actuelle | ligne « Situation actuelle » | | |
| Difficultés rencontrées | ligne « Difficultés rencontrées » | | |
| Résultat recherché | ligne « Résultat recherché » | | |
| Critères de réussite | ligne « Critères de réussite » | | |
| Utilisateurs ou services concernés | correction A6 (scénario A) ; ligne du §1 (scénario B) | | |
| Processus métier | ligne « Processus métier » | | |
| Bénéfices attendus | ligne « Bénéfices attendus » | | |

Verdict (DEB-Q136) : la fiche FINALE fait foi. Une proposition inventée REJETÉE en A4 laisse le critère tenu ; elle
est notée (rubrique, fait ajouté). Une valeur RETENUE qui porte un fait ajouté = KO du critère : signalée comme
anomalie, à arbitrer avant la clôture de la V1.

## 4. Scénario B — retour pour complément, agent Louis Petit (valideur Dominique Bernard)

| N° | Action | Attendu |
| --- | --- | --- |
| B1 | Accueil, en-tête : agent « Louis Petit » ; « Nouvelle fiche » ; premier message du §1 TAPÉ, « Envoyer » | comme A3 |
| B2 | Dialogue jusqu'à 10/10 avec les réponses du §1, propositions acceptées (rejet d'un fait ajouté comme en A4), résumé ; aucune proposition en attente | 10/10 ; grille du §3 OK |
| B3 | Confirmer, « Soumettre » | « Soumise », numéro `EB-2026-NNNN` (le noter, différent de A9) |
| B4 | Décision « Demander des compléments » SANS commentaire, « Décider » | refus affiché (commentaire obligatoire), statut inchangé « Soumise » |
| B5 | Commentaire « Préciser le nombre de contrôles par an. », « Décider » | badge « À compléter » ; « Commentaire de Dominique Bernard : Préciser le nombre de contrôles par an. » ; « Reprendre la fiche » |
| B6 | Accueil | ligne de Louis Petit : badge « À compléter » |
| B7 | « Ouvrir » la fiche ; « Reprendre la fiche » | badge « Brouillon », case de confirmation décochée, fiche modifiable |
| B8 | Rubrique « Situation actuelle » : ajouter à la fin « Environ 400 contrôles par an. », « Enregistrer » | badge « Manuelle » |
| B9 | Cocher la confirmation, « Soumettre » | « Soumise », MÊME numéro qu'en B3 (DEB-Q17) |
| B10 | Décision « Valider », « Décider » | « Validée » ; .docx téléchargé et ouvert dans Word : situation actuelle complétée, décision « Validée » |
| B11 | Panneau « Cycle de vie » | six lignes, dans l'ordre : Création (— → Brouillon, Louis Petit) ; Soumission (Brouillon → Soumise, Louis Petit) ; Demande de compléments (Soumise → À compléter, Dominique Bernard) ; Reprise (À compléter → Brouillon, Louis Petit) ; Soumission (Brouillon → Soumise, Louis Petit) ; Validation (Soumise → Validée, Dominique Bernard) ; dates à l'heure de Paris |
| B12 | Accueil | deux lignes « Validée » (A et B), chacune avec « Télécharger » |

## 5. Clôture

| N° | Action | Attendu |
| --- | --- | --- |
| C1 | Arrêter le site (Ctrl+C, console 2) et Vite (console 1) | DLL de `bin\Debug` libérées |
| C2 | `Select-String -Path C:\Dev\Demonstrateur-data\site-R1.log -Pattern 'Panne pendant','DEB-A5' -SimpleMatch` (motifs sans accent : le journal de `Tee-Object` peut abîmer les accents ; « Panne pendant » = 503, « DEB-A5 » = réponse vide) | aucune ligne : DEB-A6 close (DEB-Q124) ; sinon joindre les lignes |
| C3 | Envoyer la grille des résultats (A1 à A12, §3 pour A et B, B1 à B12, C2 ; numéros des deux fiches ; observations) | lot R1b livré : historique, prompt, plan |
| C4 | R1b appliqué et commité, PUIS publication et sauvegarde (ligne donnée avec R1b : `Publier-Branche -Confirmer`, `Sauvegarder-Depots`) | publication sans erreur ; sauvegarde des dépôts |
| C5 | `Ouvrir-Session -BrancheAttendue main` | « Commits à publier : 0 » ; sauvegarde sans commit non sauvegardé : **critère de la V1 prouvé** |

Tests verts et couverture conforme : mesurés au lot S1 (879, 0 échec ; 100 / 100), aucun code modifié depuis ;
`Compter-Tests` de C5 le confirme.
