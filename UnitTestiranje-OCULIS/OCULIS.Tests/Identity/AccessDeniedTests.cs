using OCULIS.Areas.Identity.Pages.Account;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
public class AccessDeniedTests
{
    [TestMethod]
    public void OnGet_Poziv_NeDodajeGreskeModelu()
    {
        var stranica = Clan5TestContext.Stranica(new AccessDeniedModel());

        stranica.OnGet();

        Assert.IsTrue(stranica.ModelState.IsValid);
        Assert.AreEqual(0, stranica.ModelState.ErrorCount);
    }
}
