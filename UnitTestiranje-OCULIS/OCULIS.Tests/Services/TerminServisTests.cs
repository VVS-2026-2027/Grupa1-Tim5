using OCULIS.Models;
using OCULIS.Services.Termin;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class TerminServisTests
{
    [TestMethod]
    public async Task DohvatiDostupneTermineAsync_SviSlotoviZauzeti_VracaPraznuListu()
    {
        await using var db = TestContextFactory.Create();
        var datum = new DateTime(2027, 1, 4);
        db.TerminPregleda.AddRange(Enumerable.Range(0, 20).Select(i => new TerminPregleda
        {
            Datum = datum, Vrijeme = TimeSpan.FromMinutes(480 + 30 * i),
            IdPoslovnica = 1, Status = StatusTermina.Realizovan
        }));
        await db.SaveChangesAsync();

        var servis = new TerminServis(db);
        Assert.AreEqual(0, (await servis.DohvatiDostupneTermineAsync(1, datum)).Count);
        Assert.AreEqual(20, (await servis.DohvatiDostupneTermineAsync(1, datum.AddDays(1))).Count);
    }
}
