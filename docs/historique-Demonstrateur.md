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
