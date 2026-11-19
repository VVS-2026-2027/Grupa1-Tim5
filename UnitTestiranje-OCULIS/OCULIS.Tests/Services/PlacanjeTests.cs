using System.Text.Json;
using Moq;
using OCULIS.Models;
using OCULIS.Services.Placanje;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan2")]
public class PlacanjeTests
{
    public sealed record KarticniScenarij(string? Kartica, string Status);

    public static IEnumerable<object?[]> Kartice()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "kartice.json");
        var scenariji = JsonSerializer.Deserialize<KarticniScenarij[]>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Nedostaju kartični scenariji.");
        return scenariji.Select(s => new object?[] { s.Kartica, Enum.Parse<StatusPlacanja>(s.Status) });
    }

    [TestMethod]
    [DynamicData(nameof(Kartice), DynamicDataSourceType.Method)]
    public async Task ObradiPlacanjeAsync_KarticniScenariji_VracaStatusAutorizacije(string? kartica, StatusPlacanja status)
    {
        var strategija = new KarticnoPlacanjeStrategija();

        var rezultat = await strategija.ObradiPlacanjeAsync(new Placanje(), kartica);

        Assert.AreEqual(status, rezultat.Status);
        Assert.AreEqual(status == StatusPlacanja.Uspjesno, rezultat.Uspjesno);
        if (rezultat.Uspjesno)
        {
            StringAssert.StartsWith(rezultat.ReferencaTransakcije!, "TXN-");
            StringAssert.Contains(rezultat.Poruka, "****1234");
        }
        else
        {
            Assert.IsNull(rezultat.ReferencaTransakcije);
        }
    }

    [TestMethod]
    public async Task ObradiPlacanjeAsync_Gotovina_CekaPotvrdu()
    {
        var strategija = new GotovinskoPlacanjeStrategija();

        var rezultat = await strategija.ObradiPlacanjeAsync(new Placanje());

        Assert.IsTrue(rezultat.Uspjesno);
        Assert.AreEqual(StatusPlacanja.NaCekanju, rezultat.Status);
        StringAssert.StartsWith(rezultat.ReferencaTransakcije!, "CASH-");
    }

    [TestMethod]
    [DataRow(NacinPlacanja.Gotovina)]
    [DataRow(NacinPlacanja.KreditnaKartica)]
    public void Kreiraj_PodrzanNacin_VracaOdabranuStrategiju(NacinPlacanja nacin)
    {
        var gotovina = new Mock<IPlacanjeStrategija>();
        gotovina.SetupGet(s => s.Nacin).Returns(NacinPlacanja.Gotovina);
        var kartica = new Mock<IPlacanjeStrategija>();
        kartica.SetupGet(s => s.Nacin).Returns(NacinPlacanja.KreditnaKartica);
        var factory = new PlacanjeServisFactory([gotovina.Object, kartica.Object]);

        var rezultat = factory.Kreiraj(nacin);

        Assert.AreSame(nacin == NacinPlacanja.Gotovina ? gotovina.Object : kartica.Object, rezultat);
        gotovina.VerifyGet(s => s.Nacin, Times.Once);
    }

    [TestMethod]
    public void Kreiraj_NemaStrategije_BacaIzuzetak()
    {
        var factory = new PlacanjeServisFactory([]);

        Assert.ThrowsExactly<InvalidOperationException>(() => factory.Kreiraj(NacinPlacanja.Gotovina));
    }
}
