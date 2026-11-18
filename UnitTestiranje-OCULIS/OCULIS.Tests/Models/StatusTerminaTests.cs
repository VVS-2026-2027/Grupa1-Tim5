using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class StatusTerminaTests
{
    [TestMethod]
    [DataRow(0, StatusTermina.Slobodan)]
    [DataRow(1, StatusTermina.Zakazan)]
    [DataRow(2, StatusTermina.Otkazan)]
    [DataRow(3, StatusTermina.Realizovan)]
    public void StatusTermina_StabilneVrijednostiZaPohranu(int broj, StatusTermina status)
    {
        Assert.IsTrue(Enum.IsDefined(status));
        Assert.AreEqual(broj, (int)status);
    }
}
