using OCULIS.Models;
using OCULIS.Services.Placanje;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan2")]
public class PlacanjeStrategyEdgeTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("123456789012")]
    public async Task KarticnoPlacanje_NedostajeKarticaIliJePrekratka_Odbija(string? card)
    {
        var result = await new KarticnoPlacanjeStrategija().ObradiPlacanjeAsync(new Placanje(), card);
        Assert.IsFalse(result.Uspjesno);
        Assert.AreEqual(StatusPlacanja.Odbijeno, result.Status);
        Assert.IsNull(result.ReferencaTransakcije);
    }

    [TestMethod]
    public async Task KarticnoPlacanje_KarticaSaNedovoljnimSredstvima_Odbija()
    {
        var result = await new KarticnoPlacanjeStrategija().ObradiPlacanjeAsync(new Placanje(), "1234567890000");
        Assert.AreEqual(StatusPlacanja.Odbijeno, result.Status);
        Assert.IsFalse(result.Uspjesno);
        Assert.IsNull(result.ReferencaTransakcije);
    }

    [TestMethod]
    public async Task KarticnoPlacanje_IspravnaKartica_VracaReferencuIMaskiranBroj()
    {
        var result = await new KarticnoPlacanjeStrategija().ObradiPlacanjeAsync(new Placanje(), "1234567891234");
        Assert.IsTrue(result.Uspjesno);
        Assert.AreEqual(StatusPlacanja.Uspjesno, result.Status);
        StringAssert.StartsWith(result.ReferencaTransakcije!, "TXN-");
        StringAssert.Contains(result.Poruka, "****1234");
    }

    [TestMethod]
    public async Task GotovinskoPlacanje_NacinJeGotovinaINeZavrsavaUspjesnoNaplatom()
    {
        var strategy = new GotovinskoPlacanjeStrategija();
        var result = await strategy.ObradiPlacanjeAsync(new Placanje(), "ignored");
        Assert.AreEqual(NacinPlacanja.Gotovina, strategy.Nacin);
        Assert.AreEqual(StatusPlacanja.NaCekanju, result.Status);
        Assert.IsTrue(result.Uspjesno);
    }
}
