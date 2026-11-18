using OCULIS.Areas.Identity.Pages.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class ResetPasswordConfirmationTests
{
    [TestMethod]
    public void OnGet_PotvrdaJeDostupnaAnonimnomKorisniku()
    {
        Assert.IsTrue(Attribute.IsDefined(typeof(ResetPasswordConfirmationModel),
            typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute)));
        new ResetPasswordConfirmationModel().OnGet();
    }
}
