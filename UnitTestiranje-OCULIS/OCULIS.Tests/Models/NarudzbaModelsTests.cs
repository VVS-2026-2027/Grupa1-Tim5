using System.ComponentModel.DataAnnotations;
using OCULIS.Models;
using OCULIS.Models.ViewModels;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan2")]
public class NarudzbaModelsTests
{
    [TestMethod]
    [DataRow(0, false)]
    [DataRow(0.01, true)]
    [DataRow(100, true)]
    public void Placanje_IznosValidacija(double amount, bool valid)
    {
        var errors = Validate(new Placanje { Iznos = amount });
        Assert.AreEqual(valid, !errors.Any(e => e.MemberNames.Contains(nameof(Placanje.Iznos))));
    }

    [TestMethod]
    [DataRow(null, false)]
    [DataRow("", false)]
    [DataRow("Reklamacija", true)]
    public void Reklamacija_OpisValidacija(string? description, bool valid)
    {
        var errors = Validate(new Reklamacija { Opis = description! });
        Assert.AreEqual(valid, !errors.Any(e => e.MemberNames.Contains(nameof(Reklamacija.Opis))));
    }

    [TestMethod]
    [DataRow("Ulica 12", "Sarajevo", "71000", "061 234 567", true)]
    [DataRow("Ulica", "Sarajevo", "71000", "061 234 567", false)]
    [DataRow("Ulica 12", "S1", "71000", "061 234 567", false)]
    [DataRow("Ulica 12", "Sarajevo", "7100", "061 234 567", false)]
    [DataRow("Ulica 12", "Sarajevo", "71000", "061234567", true)]
    [DataRow("Ulica 12", "Sarajevo", "71000", "061-234-567", false)]
    public void CheckoutViewModel_ValidiraAdreseIBrojTelefona(string address, string city, string postal, string phone, bool valid)
    {
        var model = new NarudzbaCheckoutViewModel { AdresaIsporuke = address, Grad = city, PostanskiBroj = postal, Telefon = phone };
        Assert.AreEqual(valid, Validate(model).Count == 0);
    }

    [TestMethod]
    [DataRow("1234567890123456", "12/30", "123", true)]
    [DataRow("123456789012345", "12/30", "123", false)]
    [DataRow("1234567890123456", "13/30", "123", false)]
    [DataRow("1234567890123456", "12/30", "12", false)]
    public void PlacanjeViewModel_ValidiraKarticnePodatke(string card, string expiry, string cvv, bool valid)
    {
        var model = new PlacanjeViewModel { NacinPlacanja = NacinPlacanja.KreditnaKartica, BrojKartice = card, DatumIsteka = expiry, CVV = cvv };
        Assert.AreEqual(valid, Validate(model).Count == 0);
    }

    [TestMethod]
    public void Narudzba_InicijaliziraKolekcije()
    {
        var order = new Narudzba();
        Assert.IsNotNull(order.Stavke);
        Assert.IsNotNull(order.Placanja);
        Assert.IsNotNull(order.Reklamacije);
    }

    [TestMethod]
    public void StavkaNarudzbe_PostavljenaSvojstva_VracajuIsteVrijednosti()
    {
        var order = new Narudzba { Id = 2 };
        var product = new Proizvod { Id = 3 };
        var item = new StavkaNarudzbe { Id = 1, IdNarudzba = 2, Narudzba = order, IdProizvod = 3, Proizvod = product, NazivProizvoda = new string('a', 150), Kolicina = 4, Cijena = 12.5 };
        Assert.AreEqual(1, item.Id);
        Assert.AreEqual(2, item.IdNarudzba);
        Assert.AreSame(order, item.Narudzba);
        Assert.AreEqual(3, item.IdProizvod);
        Assert.AreSame(product, item.Proizvod);
        Assert.AreEqual(150, item.NazivProizvoda.Length);
        Assert.AreEqual(4, item.Kolicina);
        Assert.AreEqual(12.5, item.Cijena);
    }

    [TestMethod]
    public void Reklamacija_OpisPrekoMaksimalneDuzine_NijeValidan()
    {
        var errors = Validate(new Reklamacija { Opis = new string('x', 1001) });
        Assert.IsTrue(errors.Any(e => e.MemberNames.Contains(nameof(Reklamacija.Opis))));
    }

    private static List<ValidationResult> Validate(object value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);
        return results;
    }
}
