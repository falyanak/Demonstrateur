namespace ExpressionBesoin.Domain.Tests;

public sealed class ListeImmuableTests
{
    [Fact]
    public void Vide_ne_contient_rien()
    {
        Assert.Empty(ListeImmuable<int>.Vide);
        Assert.Empty(ListeImmuable<int>.Vide.ToArray());
    }

    [Fact]
    public void De_copie_les_elements_dans_l_ordre()
    {
        var source = new[] { 3, 1, 2 };
        var liste = ListeImmuable<int>.De(source);
        source[0] = 9;

        Assert.Equal(3, liste.Count);
        Assert.Equal(3, liste[0]);
        Assert.Equal(1, liste[1]);
        Assert.Equal(new[] { 3, 1, 2 }, liste.ToArray());
    }

    [Fact]
    public void Ajouter_rend_une_nouvelle_liste_sans_toucher_l_originale()
    {
        var originale = ListeImmuable<int>.De(new[] { 1 });
        var allongee = originale.Ajouter(2);

        Assert.Equal(new[] { 1 }, originale.ToArray());
        Assert.Equal(new[] { 1, 2 }, allongee.ToArray());
    }

    [Fact]
    public void Remplacer_rend_une_nouvelle_liste_sans_toucher_l_originale()
    {
        var originale = ListeImmuable<int>.De(new[] { 1, 2, 3 });
        var modifiee = originale.Remplacer(1, 20);

        Assert.Equal(new[] { 1, 2, 3 }, originale.ToArray());
        Assert.Equal(new[] { 1, 20, 3 }, modifiee.ToArray());
    }

    [Fact]
    public void Deux_listes_de_memes_elements_sont_egales_et_de_meme_empreinte()
    {
        var a = ListeImmuable<int>.De(new[] { 1, 2 });
        var b = ListeImmuable<int>.Vide.Ajouter(1).Ajouter(2);

        Assert.NotSame(a, b);
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Des_listes_d_elements_differents_ne_sont_pas_egales()
    {
        var a = ListeImmuable<int>.De(new[] { 1, 2 });

        Assert.False(a.Equals(ListeImmuable<int>.De(new[] { 1, 3 })));
        Assert.False(a.Equals(ListeImmuable<int>.De(new[] { 1, 2, 3 })));
        Assert.False(a.Equals((ListeImmuable<int>?)null));
        Assert.False(a.Equals((object?)null));
        Assert.False(a.Equals((object)"1, 2"));
    }

    [Fact]
    public void Enumeration_directe_generique_et_non_generique_rendent_les_elements_dans_l_ordre()
    {
        var liste = ListeImmuable<int>.De(new[] { 4, 5 });

        var directs = new List<int>();
        foreach (var element in liste)
        {
            directs.Add(element);
        }

        var generiques = new List<int>();
        IEnumerable<int> sequence = liste;
        foreach (var element in sequence)
        {
            generiques.Add(element);
        }

        var nonGeneriques = new List<object>();
        foreach (var element in (System.Collections.IEnumerable)liste)
        {
            nonGeneriques.Add(element);
        }

        Assert.Equal(new[] { 4, 5 }, directs.ToArray());
        Assert.Equal(new[] { 4, 5 }, generiques.ToArray());
        Assert.Equal(new object[] { 4, 5 }, nonGeneriques.ToArray());
    }
}
