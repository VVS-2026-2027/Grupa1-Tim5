using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OCULIS.Models;
using OCULIS.Services.Obavijest;

namespace OCULIS.Tests;

[TestClass]
public class ObavijestTests
{
    [TestMethod]
    [DataRow("Potvrda", "Termin je zakazan.")]
    [DataRow("", "")]
    public async Task PosaljiObavijestAsync_EmailIskljucen_CuvaPorukuBezSlanja(string naslov, string tekst)
    {
        await using var db = TestContextFactory.Create();
        var email = new Mock<IEmailSender>(MockBehavior.Strict);
        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        var servis = new ObavijestServis(db, email.Object, scopeFactory.Object);

        await servis.PosaljiObavijestAsync("kupac", naslov, tekst, posaljiEmail: false);

        var obavijest = await db.Obavijest.SingleAsync();
        Assert.AreEqual("kupac", obavijest.IdKorisnik);
        Assert.AreEqual(naslov, obavijest.Naslov);
        Assert.AreEqual(tekst, obavijest.Tekst);
        Assert.IsFalse(obavijest.PoslanoEmailom);
        email.VerifyNoOtherCalls();
        scopeFactory.VerifyNoOtherCalls();
    }
}
