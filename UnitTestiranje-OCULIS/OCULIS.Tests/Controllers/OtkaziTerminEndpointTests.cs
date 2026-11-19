using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using OCULIS.Constants;
using OCULIS.Controllers;
using OCULIS.Data;
using OCULIS.Models;
using OCULIS.Services.Obavijest;
using OCULIS.Services.Termin;
using System.Reflection;

namespace OCULIS.Tests;

[TestClass]
public class OtkaziTerminEndpointTests
{
    private static MethodInfo Akcija => typeof(TerminPregledaController)
        .GetMethod(nameof(TerminPregledaController.Otkazi), [typeof(int), typeof(IOtkazivanjeTerminaServis)])
        ?? throw new AssertFailedException("Nedostaje akcija Otkazi(int, IOtkazivanjeTerminaServis).");

    [TestMethod]
    public void Otkazi_ZasticenPostZaKupca()
    {
        Assert.IsNotNull(Akcija.GetCustomAttribute<HttpPostAttribute>());
        Assert.IsNotNull(Akcija.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.AreEqual(Uloge.Kupac, Akcija.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        Assert.IsNotNull(Akcija.GetParameters()[1].GetCustomAttribute<FromServicesAttribute>());
    }

    [TestMethod]
    public async Task Otkazi_BezIdentiteta_VracaIzazovZaPrijavu()
    {
        await using var db = TestContextFactory.Create();
        var termin = await DodajTermin(db, new Korisnik());
        var politika = new Mock<IOtkazivanjeTerminaServis>(MockBehavior.Strict);
        var controller = Kreiraj(db, null);

        Assert.IsInstanceOfType<ChallengeResult>(await controller.Otkazi(termin.Id, politika.Object));
        Assert.AreEqual(StatusTermina.Zakazan, termin.Status);
        politika.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Otkazi_NepostojeciTermin_VracaNotFound()
    {
        await using var db = TestContextFactory.Create();
        var politika = new Mock<IOtkazivanjeTerminaServis>(MockBehavior.Strict);
        var controller = Kreiraj(db, new Korisnik());

        Assert.IsInstanceOfType<NotFoundResult>(await controller.Otkazi(123, politika.Object));
        politika.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Otkazi_TudjiTermin_VracaForbidBezPromjene()
    {
        await using var db = TestContextFactory.Create();
        var termin = await DodajTermin(db, new Korisnik());
        var politika = new Mock<IOtkazivanjeTerminaServis>(MockBehavior.Strict);
        var controller = Kreiraj(db, new Korisnik());

        Assert.IsInstanceOfType<ForbidResult>(await controller.Otkazi(termin.Id, politika.Object));
        db.ChangeTracker.Clear();
        Assert.AreEqual(StatusTermina.Zakazan, (await db.TerminPregleda.FindAsync(termin.Id))!.Status);
        politika.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Otkazi_Vlasnik_PostujeOdlukuPolitikeIPohranjujeSamoOtkazivanje(bool dozvoljeno)
    {
        await using var db = TestContextFactory.Create();
        var korisnik = new Korisnik();
        var termin = await DodajTermin(db, korisnik);
        var politika = new Mock<IOtkazivanjeTerminaServis>(MockBehavior.Strict);
        politika.Setup(p => p.MozeOtkazati(termin)).Returns(dozvoljeno);
        var controller = Kreiraj(db, korisnik);

        var rezultat = await controller.Otkazi(termin.Id, politika.Object);

        politika.Verify(p => p.MozeOtkazati(termin), Times.Once);
        db.ChangeTracker.Clear();
        var spremljen = (await db.TerminPregleda.FindAsync(termin.Id))!;
        if (dozvoljeno)
        {
            var redirect = (RedirectToActionResult)rezultat;
            Assert.AreEqual(nameof(TerminPregledaController.Details), redirect.ActionName);
            Assert.AreEqual(termin.Id, redirect.RouteValues!["id"]);
            Assert.AreEqual(StatusTermina.Otkazan, spremljen.Status);
            Assert.IsTrue(controller.TempData.ContainsKey("Success"));
        }
        else
        {
            Assert.IsInstanceOfType<ConflictObjectResult>(rezultat);
            Assert.AreEqual(StatusTermina.Zakazan, spremljen.Status);
            Assert.IsFalse(controller.TempData.ContainsKey("Success"));
        }
    }

    [TestMethod]
    public async Task Otkazi_NeuspjesanUpis_Vraca503IZadrzavaZakazaniStatus()
    {
        var kvar = new KvarUpisa();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(kvar)
            .Options;
        await using var db = new ApplicationDbContext(options);
        var korisnik = new Korisnik();
        var termin = await DodajTermin(db, korisnik);
        var politika = new Mock<IOtkazivanjeTerminaServis>();
        politika.Setup(p => p.MozeOtkazati(termin)).Returns(true);
        var controller = Kreiraj(db, korisnik);
        kvar.Aktivan = true;

        var rezultat = (ObjectResult)await controller.Otkazi(termin.Id, politika.Object);

        Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, rezultat.StatusCode);
        Assert.AreEqual(StatusTermina.Zakazan, termin.Status);
        Assert.IsFalse(controller.TempData.ContainsKey("Success"));
        await using var provjera = new ApplicationDbContext(options);
        Assert.AreEqual(StatusTermina.Zakazan, (await provjera.TerminPregleda.FindAsync(termin.Id))!.Status);
    }

    private static async Task<TerminPregleda> DodajTermin(ApplicationDbContext db, Korisnik korisnik)
    {
        var termin = new TerminPregleda { IdKorisnik = korisnik.Id, Status = StatusTermina.Zakazan };
        db.Add(termin);
        await db.SaveChangesAsync();
        return termin;
    }

    private static TerminPregledaController Kreiraj(ApplicationDbContext db, Korisnik? korisnik)
    {
        var users = Clan3TestContext.Users(korisnik);
        var controller = Clan3TestContext.Controller(new TerminPregledaController(db,
            users.Object, Mock.Of<ITerminServis>(), Mock.Of<IObavijestServis>()), Uloge.Kupac);
        users.Setup(m => m.GetUserAsync(controller.User)).ReturnsAsync(korisnik!);
        return controller;
    }

    private sealed class KvarUpisa : SaveChangesInterceptor
    {
        internal bool Aktivan { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Aktivan)
                throw new DbUpdateException("Simulirani kvar pri upisu.");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
