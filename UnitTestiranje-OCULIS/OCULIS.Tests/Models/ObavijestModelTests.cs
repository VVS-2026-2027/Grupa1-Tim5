using System.ComponentModel.DataAnnotations;
using OCULIS.Models;

namespace OCULIS.Tests.Models;

[TestClass]
public class ObavijestModelTests
{
    [TestMethod]
    public void Validacija_IspravanNaslovITekst_Prolazi()
    {
        Assert.IsTrue(Validate(new Obavijest { Naslov = "Podsjetnik", Tekst = "Termin sutra." }).Count == 0);
    }

    [TestMethod]
    public void Validacija_PrazanNaslovINedostajuciTekst_PadaRequiredValidaciju()
    {
        var result = Validate(new Obavijest { Naslov = "", Tekst = null! });
        CollectionAssert.AreEquivalent(new[] { nameof(Obavijest.Naslov), nameof(Obavijest.Tekst) },
            result.Select(x => x.MemberNames.Single()).ToArray());
    }

    [TestMethod]
    public void Validacija_NaslovPreko150Karaktera_NijeValidan()
    {
        Assert.IsTrue(Validate(new Obavijest { Naslov = new string('N', 151), Tekst = "tekst" })
            .Any(x => x.MemberNames.Contains(nameof(Obavijest.Naslov))));
    }

    [TestMethod]
    public void Validacija_TekstPreko2000Karaktera_NijeValidan()
    {
        Assert.IsTrue(Validate(new Obavijest { Naslov = "Naslov", Tekst = new string('T', 2001) })
            .Any(x => x.MemberNames.Contains(nameof(Obavijest.Tekst))));
    }

    private static List<ValidationResult> Validate(Obavijest item)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(item, new ValidationContext(item), results, true);
        return results;
    }
}
