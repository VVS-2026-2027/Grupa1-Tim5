using Microsoft.AspNetCore.Mvc;
using Moq;
using OCULIS.Constants;
using OCULIS.Controllers;
using OCULIS.Models;
using OCULIS.Models.ViewModels;
using OCULIS.Services.Obavijest;
using OCULIS.Services.Termin;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class TerminPregledaControllerTests
{
    [TestMethod]
    public async Task Zakazi_DostupanTermin_SpremaRezervacijuISaljeObavijest()
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik();
        var model = new TerminZakazivanjeViewModel { IdPoslovnica = 2, Datum = new(2027, 1, 4, 13, 0, 0), Vrijeme = TimeSpan.FromHours(9) };
        var servis = new Mock<ITerminServis>(MockBehavior.Strict);
        servis.Setup(s => s.JeTerminDostupanAsync(2, model.Datum, model.Vrijeme, null)).ReturnsAsync(true);
        var obavijesti = new Mock<IObavijestServis>();
        var controller = Clan3TestContext.Controller(new TerminPregledaController(db,
            Clan3TestContext.Users(user).Object, servis.Object, obavijesti.Object), Uloge.Kupac);

        Assert.IsInstanceOfType<ViewResult>(await controller.Zakazi());
        var result = (RedirectToActionResult)await controller.Zakazi(model);
        var saved = db.TerminPregleda.Single();
        Assert.AreEqual("Details", result.ActionName);
        Assert.AreEqual(saved.Id, result.RouteValues!["id"]);
        Assert.AreEqual(model.Datum.Date, saved.Datum);
        Assert.AreEqual(model.Vrijeme, saved.Vrijeme);
        Assert.AreEqual(model.IdPoslovnica, saved.IdPoslovnica);
        Assert.AreEqual(user.Id, saved.IdKorisnik);
        Assert.AreEqual(StatusTermina.Zakazan, saved.Status);
        obavijesti.Verify(s => s.PosaljiObavijestTerminAsync(user.Id, saved.Id), Times.Once);
    }

    [TestMethod]
    public async Task Zakazi_ZauzetTermin_PrikazujeAlternativeBezUpisa()
    {
        await using var db = TestContextFactory.Create();
        var model = new TerminZakazivanjeViewModel { IdPoslovnica = 2, Vrijeme = TimeSpan.FromHours(9) };
        var alternativa = TimeSpan.FromHours(10);
        var servis = new Mock<ITerminServis>(MockBehavior.Strict);
        servis.Setup(s => s.JeTerminDostupanAsync(2, model.Datum, model.Vrijeme, null)).ReturnsAsync(false);
        servis.Setup(s => s.DohvatiDostupneTermineAsync(2, model.Datum)).ReturnsAsync([alternativa]);
        var obavijesti = new Mock<IObavijestServis>(MockBehavior.Strict);
        var controller = new TerminPregledaController(db, Clan3TestContext.Users().Object, servis.Object, obavijesti.Object);

        Assert.AreSame(model, ((ViewResult)await controller.Zakazi(model)).Model);
        Assert.IsFalse(controller.ModelState.IsValid);
        CollectionAssert.AreEqual(new[] { alternativa }, model.DostupniTermini);
        Assert.IsFalse(db.TerminPregleda.Any());
        obavijesti.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow(Uloge.Kupac, true)]
    [DataRow(Uloge.Kupac, false)]
    [DataRow(Uloge.Zaposlenik, false)]
    [DataRow(Uloge.Administrator, false)]
    public async Task Details_ProvjeravaVlasnistvoIUlogu(string role, bool vlasnik)
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik();
        var termin = new TerminPregleda { Korisnik = user, Poslovnica = new Poslovnica() };
        db.Add(termin);
        await db.SaveChangesAsync();
        var controller = Clan3TestContext.Controller(new TerminPregledaController(db,
            Clan3TestContext.Users(vlasnik ? user : new Korisnik()).Object,
            Mock.Of<ITerminServis>(), Mock.Of<IObavijestServis>()), role);

        Assert.IsInstanceOfType<NotFoundResult>(await controller.Details(null));
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Details(-1));
        var result = await controller.Details(termin.Id);
        if (vlasnik || role != Uloge.Kupac)
            Assert.AreEqual(termin.Id, ((TerminPregleda)((ViewResult)result).Model!).Id);
        else
            Assert.IsInstanceOfType<ForbidResult>(result);

        var lista = (List<TerminPregleda>)((ViewResult)await controller.Index()).Model!;
        Assert.AreEqual(vlasnik || role != Uloge.Kupac ? 1 : 0, lista.Count);
    }

    [TestMethod]
    public async Task Edit_MijenjaSamoStatus()
    {
        await using var db = TestContextFactory.Create();
        var termin = new TerminPregleda { Vrijeme = TimeSpan.FromHours(9), Status = StatusTermina.Zakazan };
        db.Add(termin);
        await db.SaveChangesAsync();
        var controller = Clan3TestContext.Controller(new TerminPregledaController(db,
            Clan3TestContext.Users().Object, Mock.Of<ITerminServis>(), Mock.Of<IObavijestServis>()), Uloge.Zaposlenik);
        var izmjena = new TerminPregleda { Id = termin.Id, Status = StatusTermina.Otkazan };

        Assert.IsInstanceOfType<NotFoundResult>(await controller.Edit(null));
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Edit(-1));
        Assert.AreSame(termin, ((ViewResult)await controller.Edit(termin.Id)).Model);
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Edit(-1, izmjena));
        Assert.AreEqual("Details", ((RedirectToActionResult)await controller.Edit(termin.Id, izmjena)).ActionName);

        db.ChangeTracker.Clear();
        var saved = db.TerminPregleda.Single();
        Assert.AreEqual(StatusTermina.Otkazan, saved.Status);
        Assert.AreEqual(TimeSpan.FromHours(9), saved.Vrijeme);
    }
}
