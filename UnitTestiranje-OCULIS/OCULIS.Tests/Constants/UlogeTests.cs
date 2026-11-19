using OCULIS.Constants;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
public class UlogeTests
{
    [TestMethod]
    public void Sve_DefinisaneUloge_SadrziTriOcekivaneUloge()
    {
        // Arrange
        string[] ocekivane =
        {
            "Administrator",
            "Zaposlenik",
            "Kupac"
        };

        // Act
        var rezultat = Uloge.Sve;

        // Assert
        CollectionAssert.AreEquivalent(ocekivane, rezultat);
    }
}