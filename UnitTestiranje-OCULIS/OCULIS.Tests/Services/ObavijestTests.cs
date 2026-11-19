using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OCULIS.Data;
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

    [TestMethod]
    public async Task PosaljiObavijestAsync_EmailUspjesnoPoslan_OznacavaPorukuKaoPoslanu()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = CreateContext(databaseName);
        db.Users.Add(new Korisnik { Id = "kupac", UserName = "kupac", Email = "kupac@example.test" });
        await db.SaveChangesAsync();
        var sent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var email = new Mock<IEmailSender>(MockBehavior.Strict);
        email.Setup(x => x.SendEmailAsync("kupac@example.test", "Naslov", "Tekst"))
            .Callback(() => sent.TrySetResult(true))
            .Returns(Task.CompletedTask);
        using var provider = CreateScopeFactory(databaseName, email.Object);
        var service = new ObavijestServis(db, email.Object, provider.GetRequiredService<IServiceScopeFactory>());

        await service.PosaljiObavijestAsync("kupac", "Naslov", "Tekst");
        await sent.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitUntilEmailFlagIsSaved(databaseName);

        await using var verifyDb = CreateContext(databaseName);
        var notice = await verifyDb.Obavijest.SingleAsync();
        Assert.IsTrue(notice.PoslanoEmailom);
        email.Verify(x => x.SendEmailAsync("kupac@example.test", "Naslov", "Tekst"), Times.Once);
    }

    [TestMethod]
    public async Task PosaljiObavijestNarudzbaAsync_SaljePotvrduSaBrojemNarudzbe()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = CreateContext(databaseName);
        db.Users.Add(new Korisnik { Id = "kupac", UserName = "kupac", Email = "kupac@example.test" });
        await db.SaveChangesAsync();
        var sent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var email = new Mock<IEmailSender>();
        email.Setup(x => x.SendEmailAsync("kupac@example.test", It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, _, _) => sent.TrySetResult(true)).Returns(Task.CompletedTask);
        using var provider = CreateScopeFactory(databaseName, email.Object);
        var service = new ObavijestServis(db, email.Object, provider.GetRequiredService<IServiceScopeFactory>());

        await service.PosaljiObavijestNarudzbaAsync("kupac", 27);
        await sent.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var notice = await db.Obavijest.SingleAsync();
        Assert.AreEqual("Potvrda narudžbe - OCULIS", notice.Naslov);
        StringAssert.Contains(notice.Tekst, "#27");
        email.Verify(x => x.SendEmailAsync("kupac@example.test", "Potvrda narudžbe - OCULIS", It.Is<string>(s => s.Contains("#27"))), Times.Once);
    }

    [TestMethod]
    public async Task PosaljiObavijestTerminAsync_DohvataPoslovnicuIIspisujeVrijeme()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = CreateContext(databaseName);
        db.Users.Add(new Korisnik { Id = "kupac", UserName = "kupac", Email = "kupac@example.test" });
        var branch = new Poslovnica { Naziv = "Sarajevo", Adresa = "Ulica 1", Telefon = "123", RadnoVrijeme = "09-17" };
        db.Poslovnica.Add(branch);
        var appointment = new TerminPregleda
        {
            Id = 11, IdKorisnik = "kupac", IdPoslovnica = 1, Poslovnica = branch,
            Datum = new DateTime(2026, 12, 5), Vrijeme = new TimeSpan(14, 30, 0)
        };
        db.TerminPregleda.Add(appointment);
        await db.SaveChangesAsync();
        var sent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var email = new Mock<IEmailSender>();
        email.Setup(x => x.SendEmailAsync("kupac@example.test", It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, _, _) => sent.TrySetResult(true)).Returns(Task.CompletedTask);
        using var provider = CreateScopeFactory(databaseName, email.Object);
        var service = new ObavijestServis(db, email.Object, provider.GetRequiredService<IServiceScopeFactory>());

        await service.PosaljiObavijestTerminAsync("kupac", 11);
        await sent.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var notice = await db.Obavijest.SingleAsync();
        Assert.AreEqual("Potvrda termina pregleda - OCULIS", notice.Naslov);
        StringAssert.Contains(notice.Tekst, "Sarajevo");
        StringAssert.Contains(notice.Tekst, "05.12.2026");
        StringAssert.Contains(notice.Tekst, "14:30");
    }

    private static ApplicationDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName).Options;
        return new ApplicationDbContext(options);
    }

    private static ServiceProvider CreateScopeFactory(string databaseName, IEmailSender sender) => new ServiceCollection()
        .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName))
        .AddSingleton(sender)
        .BuildServiceProvider();

    private static async Task WaitUntilEmailFlagIsSaved(string databaseName)
    {
        var until = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < until)
        {
            await using var db = CreateContext(databaseName);
            if (await db.Obavijest.AnyAsync(x => x.PoslanoEmailom)) return;
            await Task.Delay(10);
        }
        Assert.Fail("Email poslano polje nije sačuvano u zadatom vremenu.");
    }
}
