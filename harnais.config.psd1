# Configuration du dépôt Demonstrateur (spécification du harnais central §4). Type Projet depuis le lot P0 (Documents
# au lot P0a) ; solution ExpressionBesoin (DEB-D1). Données seulement, lues par Import-PowerShellDataFile.
@{
    Schema         = 1
    Depot          = 'Demonstrateur'
    Type           = 'Projet'
    HarnaisMinimal = '1.9.0'

    Archive = @{
        Nom           = 'Demonstrateur.zip'
        Solution      = 'ExpressionBesoin/ExpressionBesoin.slnx'
        ProjetsExclus = @()
        Exclusions    = @('**/ClientApp/node_modules/**')
    }

    Masquage = @{
        Fichiers      = @('**/appsettings*.json')
        Masque        = '***'
        Chemins       = @()
        NomsSensibles = @('*password*', '*secret*', '*token*', '*apikey*', '*connectionstring*', '*credential*', '*privatekey*')
        ClesSecretes  = @('password', 'pwd', 'accountkey', 'sharedaccesskey')
    }

    Lot = @{
        Racines        = @('ExpressionBesoin', 'docs', 'specifications', 'harnais.config.psd1', '.gitignore', '.gitattributes', 'README.md')
        ProjetsTests   = @('ExpressionBesoin/tests/ExpressionBesoin.Domain.Tests', 'ExpressionBesoin/tests/ExpressionBesoin.Application.Tests', 'ExpressionBesoin/tests/ExpressionBesoin.Infrastructure.Tests', 'ExpressionBesoin/tests/ExpressionBesoin.Web.Tests', 'ExpressionBesoin/tests/ExpressionBesoin.Architecture.Tests')
        TestsUnitaires = @('ExpressionBesoin/tests/ExpressionBesoin.Domain.Tests', 'ExpressionBesoin/tests/ExpressionBesoin.Application.Tests', 'ExpressionBesoin/tests/ExpressionBesoin.Infrastructure.Tests', 'ExpressionBesoin/tests/ExpressionBesoin.Web.Tests', 'ExpressionBesoin/tests/ExpressionBesoin.Architecture.Tests')
    }

    Couverture = @{
        Solution            = 'ExpressionBesoin/ExpressionBesoin.slnx'
        ProjetsTests        = @('ExpressionBesoin/tests/ExpressionBesoin.Domain.Tests', 'ExpressionBesoin/tests/ExpressionBesoin.Application.Tests', 'ExpressionBesoin/tests/ExpressionBesoin.Infrastructure.Tests', 'ExpressionBesoin/tests/ExpressionBesoin.Web.Tests')
        AssembliesAttendues = @('ExpressionBesoin.Domain', 'ExpressionBesoin.Application', 'ExpressionBesoin.Infrastructure', 'ExpressionBesoin.Web')
        Instrumentees       = '.*ExpressionBesoin\.(Domain|Application|Infrastructure|Web)\.dll$'
        Exclusions          = @('Program', 'AspNetCoreGeneratedDocument.*')
        Base                = 'ExpressionBesoin/coverage-baseline.json'
        Dette               = 'ExpressionBesoin/coverage-dette.txt'
        Rapport             = 'ExpressionBesoin/coverage-report'
    }

    Session = @{
        Documents        = @('docs/prompt-reprise-Demonstrateur.md', 'specifications/plan-demonstrateur.md')
        DocumentsHarnais = @('regles/regles-de-travail.md', 'regles/harnais-modele-generique.md', 'regles/gabarit-frontend-vite-aspnet.md')
        AvecHarnais      = $false
        Verifications    = @('Etat-Depot', 'Compter-Tests')
        Sortie           = 'C:\Dev\Sessions'
    }
}
