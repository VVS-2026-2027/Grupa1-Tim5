using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class TerminPregledaTests
{
    [TestMethod]
    [DataRow(StatusTermina.Zakazan)]
    [DataRow(StatusTermina.Otkazan)]
    public void TerminPregleda_CuvaOdabraniStatusIVrijeme(StatusTermina status)
    {
        var datum = new DateTime(2027, 1, 4);
        var vrijeme = TimeSpan.FromMinutes(510);
        var termin = new TerminPregleda { Datum = datum, Vrijeme = vrijeme, Status = status, IdPoslovnica = 2, IdKorisnik = "kupac" };
        Assert.AreEqual(datum, termin.Datum);
        Assert.AreEqual(vrijeme, termin.Vrijeme);
        Assert.AreEqual(status, termin.Status);
        Assert.AreEqual(2, termin.IdPoslovnica);
        Assert.AreEqual("kupac", termin.IdKorisnik);
    }
}
