using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OCULIS.Controllers;
using OCULIS.Models;
using OCULIS.Services.Obavijest;
using OCULIS.Services.Termin;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class TerminTests
{
    [TestMethod]
    [DataRow(7, 59, false)]
    [DataRow(8, 0, true)]
    [DataRow(17, 30, true)]
    [DataRow(18, 0, false)]
    public async Task JeTerminDostupanAsync_GraniceRadnogVremena_VracaDostupnost(int sat, int minuta, bool dostupno)
    {
        await using var db = TestContextFactory.Create();
        var servis = new TerminServis(db);

        var rezultat = await servis.JeTerminDostupanAsync(1, new DateTime(2026, 12, 1), new TimeSpan(sat, minuta, 0));

        Assert.AreEqual(dostupno, rezultat);
    }

    [TestMethod]
    public async Task DostupniTermini_ZakazaniSati_FormatiraVrijednosti()
    {
        var datum = new DateTime(2026, 12, 1);
        var servis = new Mock<ITerminServis>(MockBehavior.Strict);
        servis.Setup(s => s.DohvatiDostupneTermineAsync(2, datum))
            .ReturnsAsync([new TimeSpan(8, 0, 0), new TimeSpan(17, 30, 0)]);
        await using var db = TestContextFactory.Create();
        var users = new Mock<UserManager<Korisnik>>(Mock.Of<IUserStore<Korisnik>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        var controller = new TerminPregledaController(db, users.Object, servis.Object, Mock.Of<IObavijestServis>());

        var rezultat = await controller.DostupniTermini(2, datum);

        var json = (JsonResult)rezultat;
        CollectionAssert.AreEqual(new[] { "08:00", "17:30" }, ((IEnumerable<string>)json.Value!).ToArray());
        servis.VerifyAll();
    }

    public static IEnumerable<object[]> GraniceRadnogVremena()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "termini-clan3.csv");
        return File.ReadLines(path).Skip(1).Select(line =>
        {
            var parts = line.Split(',');
            return new object[] { int.Parse(parts[0]), int.Parse(parts[1]), bool.Parse(parts[2]) };
        });
    }

    [TestMethod]
    [DynamicData(nameof(GraniceRadnogVremena), DynamicDataSourceType.Method)]
    public async Task JeTerminDostupanAsync_UlaziIzCsv(int sat, int minuta, bool ocekivano)
    {
        await using var db = TestContextFactory.Create();
        var dostupno = await new TerminServis(db).JeTerminDostupanAsync(1, new DateTime(2027, 1, 4), new TimeSpan(sat, minuta, 0));
        Assert.AreEqual(ocekivano, dostupno);
    }

    [TestMethod]
    public async Task DostupniTermini_GreskaServisa_PropagiraIzuzetak()
    {
        await using var db = TestContextFactory.Create();
        var datum = new DateTime(2027, 1, 4);
        var servis = new Mock<ITerminServis>();
        var greska = new InvalidOperationException("Raspored nije dostupan.");
        servis.Setup(s => s.DohvatiDostupneTermineAsync(2, datum)).ThrowsAsync(greska);
        var controller = new TerminPregledaController(db, Clan3TestContext.Users().Object,
            servis.Object, Mock.Of<IObavijestServis>());

        Assert.AreSame(greska, await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => controller.DostupniTermini(2, datum)));
    }
}
