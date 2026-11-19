using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using OCULIS.Controllers;
using OCULIS.Models;
using OCULIS.Services.Obavijest;
using OCULIS.Services.Termin;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Task3Clan5")]
public class OtkazivanjeIntegracijaTests
{
    [TestMethod]
    public async Task Otkazi_VlastitiTermin24SataRanije_OslobadjaTermin()
    {
        // Arrange - priprema
        await using var db = TestContextFactory.Create();

        var kupac = new Korisnik { Id = "kupac-5" };

        var poslovnica = new Poslovnica
        {
            Naziv = "Centar",
            Adresa = "Ulica 1",
            Telefon = "033 123 456",
            RadnoVrijeme = "08:00 - 18:00"
        };

        var termin = new TerminPregleda
        {
            Korisnik = kupac,
            Poslovnica = poslovnica,
            Datum = new DateTime(2026, 11, 20),
            Vrijeme = new TimeSpan(10, 0, 0),
            Status = StatusTermina.Zakazan
        };

        db.TerminPregleda.Add(termin);
        await db.SaveChangesAsync();

        var sat = new Mock<TimeProvider>();

        sat.Setup(s => s.GetUtcNow())
            .Returns(new DateTimeOffset(
                2026, 11, 19, 10, 0, 0, TimeSpan.Zero));

        sat.SetupGet(s => s.LocalTimeZone)
            .Returns(TimeZoneInfo.Utc);

        var politika = new OtkazivanjeTerminaServis(sat.Object);
        var termini = new TerminServis(db);
        var korisnici = Clan5TestContext.Korisnici(kupac);
        var http = new DefaultHttpContext();

        var kontroler = new TerminPregledaController(
            db,
            korisnici.Object,
            termini,
            Mock.Of<IObavijestServis>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = http
            },
            TempData = new TempDataDictionary(
                http, Mock.Of<ITempDataProvider>())
        };

        var dostupanPrije = await termini.JeTerminDostupanAsync(
            poslovnica.Id, termin.Datum, termin.Vrijeme);

        // Act - otkazivanje termina
        var rezultat = await kontroler.Otkazi(termin.Id, politika);

        db.ChangeTracker.Clear();

        var spremljen = await db.TerminPregleda.FindAsync(termin.Id);

        var dostupanPoslije = await termini.JeTerminDostupanAsync(
            poslovnica.Id, termin.Datum, termin.Vrijeme);

        var slobodniTermini = await termini.DohvatiDostupneTermineAsync(
            poslovnica.Id, termin.Datum);

        // Assert - provjera rezultata
        Assert.IsFalse(dostupanPrije);

        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);

        Assert.AreEqual(
            "Details",
            ((RedirectToActionResult)rezultat).ActionName);

        Assert.IsNotNull(spremljen);
        Assert.AreEqual(StatusTermina.Otkazan, spremljen.Status);

        Assert.IsTrue(dostupanPoslije);
        CollectionAssert.Contains(slobodniTermini, termin.Vrijeme);
    }
}