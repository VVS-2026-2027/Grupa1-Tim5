using OCULIS.Models.ViewModels;

namespace OCULIS.Tests.Models;

[TestClass]
public class DashboardViewModelTests
{
    [TestMethod]
    public void Konstruktor_InicijaliziraKolekcije()
    {
        var model = new DashboardViewModel();
        Assert.IsNotNull(model.NedavneNarudzbe);
        Assert.IsNotNull(model.NarudzbePoStatusu);
        Assert.AreEqual(0, model.NedavneNarudzbe.Count);
        Assert.AreEqual(0, model.NarudzbePoStatusu.Count);
    }

    [TestMethod]
    public void Svojstva_MoguSePostavitiIVratiti()
    {
        var date = new DateTime(2026, 1, 1);
        var statistic = new NarudzbaStatistika { Id = 9, Korisnik = "A. K.", Iznos = 12.5, Status = "Kreirana", Datum = date };
        var model = new DashboardViewModel
        {
            UkupnoProizvoda = 1, UkupnoNarudzbi = 2, UkupnoTermina = 3, UkupnoKorisnika = 4,
            UkupniPrihod = 12.5, NedavneNarudzbe = [statistic], NarudzbePoStatusu = new() { ["Kreirana"] = 2 }
        };
        Assert.AreEqual(12.5, model.UkupniPrihod);
        Assert.AreSame(statistic, model.NedavneNarudzbe.Single());
        Assert.AreEqual(2, model.NarudzbePoStatusu["Kreirana"]);
        Assert.AreEqual(date, statistic.Datum);
    }
}
