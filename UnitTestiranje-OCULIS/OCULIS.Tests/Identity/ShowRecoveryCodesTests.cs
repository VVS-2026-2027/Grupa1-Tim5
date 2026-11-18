using OCULIS.Areas.Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class ShowRecoveryCodesTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("test-code")]
    public void OnGet_PrikazujeSamoPostojeceKodove(string? kod)
    {
        var page = new ShowRecoveryCodesModel
        {
            StatusMessage = "Kodovi su generisani.",
            RecoveryCodes = kod is null ? null! : kod.Length == 0 ? [] : [kod]
        };
        var result = page.OnGet();
        if (!string.IsNullOrEmpty(kod))
        {
            Assert.IsInstanceOfType<PageResult>(result);
            Assert.AreEqual(kod, page.RecoveryCodes.Single());
            Assert.AreEqual("Kodovi su generisani.", page.StatusMessage);
        }
        else
            Assert.AreEqual("./TwoFactorAuthentication", ((RedirectToPageResult)result).PageName);
    }
}
