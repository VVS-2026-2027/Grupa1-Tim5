using System.ComponentModel.DataAnnotations;
using OCULIS.Models;

namespace OCULIS.Tests.Models;

[TestClass]
public class PoslovnicaTests
{
    public static IEnumerable<object[]> PoslovniceIzCsv => File.ReadAllLines(DataPath("poslovnice-validacija.csv"))
        .Skip(1)
        .Select(line => line.Split(','))
        .Select(fields => new object[] { fields[0], fields[1], fields[2], fields[3], bool.Parse(fields[4]) });

    [TestMethod]
    [DynamicData(nameof(PoslovniceIzCsv))]
    public void Validacija_PodaciIzCsv_OdgovaraOcekivanju(string naziv, string adresa, string telefon, string radnoVrijeme, bool validno)
    {
        var branch = new Poslovnica { Naziv = naziv, Adresa = adresa, Telefon = telefon, RadnoVrijeme = radnoVrijeme };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(branch, new ValidationContext(branch), results, true);
        Assert.AreEqual(validno, isValid);
    }

    [TestMethod]
    public void Validacija_NazivPreko100Karaktera_NijeValidan()
    {
        var branch = ValidBranch();
        branch.Naziv = new string('x', 101);
        var results = new List<ValidationResult>();
        Assert.IsFalse(Validator.TryValidateObject(branch, new ValidationContext(branch), results, true));
        Assert.IsTrue(results.Any(x => x.MemberNames.Contains(nameof(Poslovnica.Naziv))));
    }

    [TestMethod]
    public void Validacija_NeobaveznaGeolokacijaPrihvataNulteKoordinate()
    {
        var branch = ValidBranch();
        Assert.AreEqual(0d, branch.Latitude);
        Assert.AreEqual(0d, branch.Longitude);
        Assert.IsTrue(Validator.TryValidateObject(branch, new ValidationContext(branch), new List<ValidationResult>(), true));
    }

    private static Poslovnica ValidBranch() => new()
    {
        Naziv = "Centar", Adresa = "Ulica 1", Telefon = "033-123456", RadnoVrijeme = "Pon-Pet 09-17"
    };

    private static string DataPath(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", name);
}
