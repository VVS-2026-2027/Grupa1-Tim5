using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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

    public sealed record ScenarijProizvoda(string Naziv, int DuzinaOpisa, double Cijena,
        int DuzinaProizvodjaca, int Zaliha, string[] Greske);

    public static IEnumerable<object[]> Proizvodi()
    {
        var putanja = Path.Combine(AppContext.BaseDirectory, "TestData", "proizvodi-validacija.json");
        var scenariji = JsonSerializer.Deserialize<ScenarijProizvoda[]>(File.ReadAllText(putanja))
            ?? throw new InvalidDataException("Nedostaju scenariji validacije proizvoda.");
        return scenariji.Select(s => new object[]
        {
            s.Naziv, s.DuzinaOpisa, s.Cijena, s.DuzinaProizvodjaca, s.Zaliha, s.Greske
        });
    }

    [TestMethod]
    [DynamicData(nameof(Proizvodi), DynamicDataSourceType.Method)]
    public void TryValidateObject_VanjskeGraniceProizvoda_PrijavljujeTacnaPolja(string naziv,
        int duzinaOpisa, double cijena, int duzinaProizvodjaca, int zaliha, string[] ocekivaneGreske)
    {
        var proizvod = new Proizvod
        {
            Naziv = naziv, Opis = new string('A', duzinaOpisa), Cijena = cijena,
            Proizvodjac = new string('P', duzinaProizvodjaca), DostupnaKolicina = zaliha
        };
        var greske = new List<ValidationResult>();

        var validan = Validator.TryValidateObject(proizvod, new ValidationContext(proizvod), greske, true);

        Assert.AreEqual(ocekivaneGreske.Length == 0, validan);
        CollectionAssert.AreEquivalent(ocekivaneGreske, greske.SelectMany(g => g.MemberNames).ToArray());
    }
}
