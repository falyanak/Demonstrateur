# Configuration du dépôt Demonstrateur (spécification du harnais central §4 ; type Documents, §4.2b).
# Données seulement, lues par Import-PowerShellDataFile. Devient type Projet au lot P0 (solution ExpressionBesoin, DEB-D1).
@{
    Schema         = 1
    Depot          = 'Demonstrateur'
    Type           = 'Documents'
    HarnaisMinimal = '1.9.0'

    Archive = @{
        Nom        = 'Demonstrateur.zip'
        Exclusions = @()
    }

    Masquage = @{
        Fichiers      = @('**/appsettings*.json')
        Masque        = '***'
        Chemins       = @()
        NomsSensibles = @('*password*', '*secret*', '*token*', '*apikey*', '*connectionstring*', '*credential*', '*privatekey*')
        ClesSecretes  = @('password', 'pwd', 'accountkey', 'sharedaccesskey')
    }

    Lot = @{
        Racines = @('ExpressionBesoin', 'docs', 'specifications', 'harnais.config.psd1', '.gitignore', '.gitattributes', 'README.md')
    }

    Session = @{
        Documents        = @('docs/prompt-reprise-Demonstrateur.md', 'specifications/plan-demonstrateur.md')
        DocumentsHarnais = @('regles/regles-de-travail.md', 'regles/harnais-modele-generique.md', 'regles/gabarit-frontend-vite-aspnet.md')
        AvecHarnais      = $false
        Verifications    = @('Etat-Depot')
        Sortie           = 'C:\Dev\Sessions'
    }
}
