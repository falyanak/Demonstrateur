namespace ExpressionBesoin.Domain.Tests;

public sealed class ResultatTests
{
    private static readonly Refus RefusA = new("A", "premier refus");
    private static readonly Refus RefusB = new("B", "second refus");

    private static Resultat<int> Succes(int valeur) => Resultat<int>.Succes(valeur);

    private static Resultat<int> Echec(Refus refus) => Resultat<int>.Echec(refus);

    // --- Construction ---

    [Fact]
    public void Succes_est_un_succes() => Assert.True(Succes(1).EstSucces);

    [Fact]
    public void Echec_n_est_pas_un_succes() => Assert.False(Echec(RefusA).EstSucces);

    [Fact]
    public void Echec_refuse_un_refus_null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Resultat<int>.Echec(null!));
        Assert.Equal("refus", exception.ParamName);
    }

    [Fact]
    public void Conversion_implicite_depuis_un_refus_donne_un_echec()
    {
        Resultat<int> resultat = RefusA;
        Assert.Equal("A", resultat.Match(_ => "succès", refus => refus.Code));
    }

    [Fact]
    public void Aide_non_generique_infere_le_type()
    {
        var resultat = Resultat.Succes("texte");
        Assert.Equal("texte", resultat.Match(valeur => valeur, _ => "échec"));
    }

    [Fact]
    public void Succes_accepte_une_valeur_null()
    {
        var resultat = Resultat<string?>.Succes(null);
        Assert.True(resultat.EstSucces);
        Assert.Null(resultat.Match(valeur => valeur, _ => "échec"));
    }

    // --- Match ---

    [Fact]
    public void Match_sur_succes_appelle_la_branche_succes()
    {
        var appelsEchec = 0;
        var sortie = Succes(4).Match(valeur => valeur * 10, _ => { appelsEchec++; return -1; });
        Assert.Equal(40, sortie);
        Assert.Equal(0, appelsEchec);
    }

    [Fact]
    public void Match_sur_echec_appelle_la_branche_echec_avec_le_refus()
    {
        var appelsSucces = 0;
        var sortie = Echec(RefusA).Match(_ => { appelsSucces++; return "succès"; }, refus => refus.Code + "|" + refus.Message);
        Assert.Equal("A|premier refus", sortie);
        Assert.Equal(0, appelsSucces);
    }

    // --- Map, Select ---

    [Fact]
    public void Map_sur_succes_transforme_la_valeur()
    {
        var resultat = Succes(3).Map(valeur => valeur + 1);
        Assert.Equal(4, resultat.Match(valeur => valeur, _ => -1));
    }

    [Fact]
    public void Map_sur_echec_propage_le_refus_sans_appeler_la_transformation()
    {
        var appels = 0;
        var resultat = Echec(RefusA).Map(valeur => { appels++; return valeur.ToString(); });
        Assert.Equal("A", resultat.Match(_ => "succès", refus => refus.Code));
        Assert.Equal(0, appels);
    }

    [Fact]
    public void Select_sur_succes_transforme_la_valeur()
    {
        var resultat = Succes(5).Select(valeur => valeur * 2);
        Assert.Equal(10, resultat.Match(valeur => valeur, _ => -1));
    }

    [Fact]
    public void Select_sur_echec_propage_le_refus()
    {
        var resultat = Echec(RefusB).Select(valeur => valeur * 2);
        Assert.Equal("B", resultat.Match(_ => "succès", refus => refus.Code));
    }

    // --- Bind ---

    [Fact]
    public void Bind_sur_succes_enchaine_un_succes()
    {
        var resultat = Succes(2).Bind(valeur => Succes(valeur + 5));
        Assert.Equal(7, resultat.Match(valeur => valeur, _ => -1));
    }

    [Fact]
    public void Bind_sur_succes_enchaine_un_echec()
    {
        var resultat = Succes(2).Bind(_ => Echec(RefusB));
        Assert.Equal("B", resultat.Match(_ => "succès", refus => refus.Code));
    }

    [Fact]
    public void Bind_sur_echec_court_circuite()
    {
        var appels = 0;
        var resultat = Echec(RefusA).Bind(valeur => { appels++; return Succes(valeur); });
        Assert.Equal("A", resultat.Match(_ => "succès", refus => refus.Code));
        Assert.Equal(0, appels);
    }

    // --- SelectMany (syntaxe de requête) ---

    [Fact]
    public void Requete_sur_deux_succes_projette_les_deux_valeurs()
    {
        var resultat =
            from a in Succes(2)
            from b in Succes(3)
            select a * 10 + b;
        Assert.Equal(23, resultat.Match(valeur => valeur, _ => -1));
    }

    [Fact]
    public void Requete_arrete_au_premier_refus_sans_evaluer_la_suite()
    {
        var evaluationsSuite = 0;
        var projections = 0;
        var resultat =
            from a in Echec(RefusA)
            from b in Suivant(a)
            select Projeter(a, b);
        Assert.Equal("A", resultat.Match(_ => "succès", refus => refus.Code));
        Assert.Equal(0, evaluationsSuite);
        Assert.Equal(0, projections);

        Resultat<int> Suivant(int valeur)
        {
            evaluationsSuite++;
            return Succes(valeur);
        }

        int Projeter(int a, int b)
        {
            projections++;
            return a + b;
        }
    }

    [Fact]
    public void Requete_propage_le_refus_de_la_seconde_etape_sans_projeter()
    {
        var projections = 0;
        var resultat =
            from a in Succes(1)
            from b in Echec(RefusB)
            select Projeter(a, b);
        Assert.Equal("B", resultat.Match(_ => "succès", refus => refus.Code));
        Assert.Equal(0, projections);

        int Projeter(int a, int b)
        {
            projections++;
            return a + b;
        }
    }

    [Fact]
    public void Requete_a_trois_etapes_garde_l_ordre_des_valeurs()
    {
        var resultat =
            from a in Succes(1)
            from b in Succes(2)
            from c in Succes(3)
            select $"{a}{b}{c}";
        Assert.Equal("123", resultat.Match(valeur => valeur, _ => "échec"));
    }

    // --- Valeur default : ni succès ni échec ---

    [Fact]
    public void Default_EstSucces_leve() => AssertNonInitialise(() => _ = default(Resultat<int>).EstSucces);

    [Fact]
    public void Default_Match_leve() => AssertNonInitialise(() => default(Resultat<int>).Match(_ => 0, _ => 1));

    [Fact]
    public void Default_Map_leve() => AssertNonInitialise(() => default(Resultat<int>).Map(valeur => valeur));

    [Fact]
    public void Default_Bind_leve() => AssertNonInitialise(() => default(Resultat<int>).Bind(valeur => Succes(valeur)));

    [Fact]
    public void Default_Select_leve() => AssertNonInitialise(() => default(Resultat<int>).Select(valeur => valeur));

    [Fact]
    public void Default_SelectMany_leve() =>
        AssertNonInitialise(() => default(Resultat<int>).SelectMany(valeur => Succes(valeur), (a, b) => a + b));

    private static void AssertNonInitialise(Action action)
    {
        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("Résultat non initialisé (valeur default) : ni succès ni échec.", exception.Message);
    }
}
