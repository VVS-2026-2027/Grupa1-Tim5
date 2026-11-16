using System.ComponentModel.DataAnnotations;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
public class ValidacijaProizvodaTests
{
    [TestMethod]
    [DataRow("", 1d, false)]
    [DataRow(null, 1d, false)]
    [DataRow("Okvir", 0d, false)]
    [DataRow("Okvir", 0.01d, true)]
    public void TryValidateObject_GraniceModelaProizvod_ProvjeravaNazivICijenu(string? naziv, double cijena, bool ocekivano)
    {
        var proizvod = new Proizvod { Naziv = naziv!, Cijena = cijena, Opis = "Okvir", Proizvodjac = "Test" };
        var greske = new List<ValidationResult>();

        var validno = Validator.TryValidateObject(proizvod, new ValidationContext(proizvod), greske, validateAllProperties: true);

        Assert.AreEqual(ocekivano, validno);
    }
}
