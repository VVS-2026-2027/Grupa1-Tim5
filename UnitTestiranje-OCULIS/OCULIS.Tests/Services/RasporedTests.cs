using OCULIS.Models;
using OCULIS.Services.Termin;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class RasporedTests
{
    [TestMethod]
    [DataRow(StatusTermina.Zakazan, false, false)]
    [DataRow(StatusTermina.Zakazan, true, true)]
    [DataRow(StatusTermina.Otkazan, false, true)]
    [DataRow(StatusTermina.Realizovan, false, false)]
    public async Task JeTerminDostupanAsync_PostojeciTermin_UvazavaStatusIIzuzetiId(StatusTermina status, bool izuzmi, bool ocekujeDostupnost)
    {
        await using var db = TestContextFactory.Create();
        var datum = new DateTime(2026, 12, 1);
        var vrijeme = new TimeSpan(8, 30, 0);
        db.TerminPregleda.Add(new TerminPregleda { Id = 4, Datum = datum, Vrijeme = vrijeme, IdPoslovnica = 2, Status = status });
        await db.SaveChangesAsync();
        var servis = new TerminServis(db);

        var dostupno = await servis.JeTerminDostupanAsync(2, datum.AddHours(12), vrijeme, izuzmi ? 4 : null);

        Assert.AreEqual(ocekujeDostupnost, dostupno);
    }

    [TestMethod]
    public async Task DohvatiDostupneTermineAsync_ZauzetTermin_IzostavljaSamoTerminTrazenePoslovnice()
    {
        await using var db = TestContextFactory.Create();
        var datum = new DateTime(2026, 12, 1);
        var prvi = new TimeSpan(8, 0, 0);
        var drugi = new TimeSpan(8, 30, 0);
        db.TerminPregleda.AddRange(
            new TerminPregleda { Datum = datum, Vrijeme = prvi, IdPoslovnica = 2, Status = StatusTermina.Zakazan },
            new TerminPregleda { Datum = datum, Vrijeme = drugi, IdPoslovnica = 3, Status = StatusTermina.Zakazan });
        await db.SaveChangesAsync();

        var termini = await new TerminServis(db).DohvatiDostupneTermineAsync(2, datum);

        Assert.AreEqual(19, termini.Count);
        Assert.AreEqual(drugi, termini[0]);
        Assert.AreEqual(new TimeSpan(17, 30, 0), termini[^1]);
        Assert.DoesNotContain(prvi, termini);
    }
}
