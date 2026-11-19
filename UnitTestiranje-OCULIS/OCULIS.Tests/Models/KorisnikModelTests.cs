using System.ComponentModel.DataAnnotations;
using OCULIS.Models;

namespace OCULIS.Tests.Models;

[TestClass]
public class KorisnikModelTests
{
    [TestMethod]
    public void PunoIme_SastavljaImeIPrezime()
    {
        var user = new Korisnik { Ime = "Amira", Prezime = "Kovač" };
        Assert.AreEqual("Amira Kovač", user.PunoIme);
    }

    [TestMethod]
    public void Validacija_ImeIPrezimeNedostaju_PadajuRequiredValidaciju()
    {
        var user = new Korisnik();
        var results = Validate(user);
        CollectionAssert.AreEquivalent(new[] { "Ime", "Prezime" }, results.Select(x => x.MemberNames.Single()).ToArray());
    }

    [TestMethod]
    public void Validacija_ImeDužeOd50Karaktera_PadaStringLengthValidaciju()
    {
        var user = new Korisnik { Ime = new string('A', 51), Prezime = "Kovač" };
        Assert.IsTrue(Validate(user).Any(x => x.MemberNames.Contains(nameof(Korisnik.Ime))));
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
