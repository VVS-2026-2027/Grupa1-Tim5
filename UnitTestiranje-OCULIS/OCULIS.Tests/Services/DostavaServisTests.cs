using OCULIS.Services.Dostava;

namespace OCULIS.Tests;

[TestClass]
public class DostavaServisTests
{
    [TestMethod]
    [DataRow(0d, 7d)]
    [DataRow(99.99d, 7d)]
    [DataRow(100d, 0d)]
    [DataRow(100.01d, 0d)]
    [DataRow(99.994d, 7d)]
    [DataRow(99.995d, 0d)]
    public void IzracunajCijenuDostave_IznosNakonPopusta_VracaOcekivanuDostavu(
        double iznosNakonPopusta, double ocekivanaDostava)
    {
        var servis = new DostavaServis();

        var rezultat = servis.IzracunajCijenuDostave(iznosNakonPopusta);

        Assert.AreEqual(ocekivanaDostava, rezultat, 0.000001d);
    }

    public static IEnumerable<object[]> NeispravniIznosi()
    {
        yield return new object[] { -0.01d };
        yield return new object[] { double.NaN };
        yield return new object[] { double.PositiveInfinity };
        yield return new object[] { double.NegativeInfinity };
    }

    [TestMethod]
    [DynamicData(nameof(NeispravniIznosi), DynamicDataSourceType.Method)]
    public void IzracunajCijenuDostave_NeispravanIznos_BacaArgumentOutOfRangeException(double iznos)
    {
        var servis = new DostavaServis();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => servis.IzracunajCijenuDostave(iznos));
    }
}
