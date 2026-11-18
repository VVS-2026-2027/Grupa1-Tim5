using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCULIS.Models;
using OCULIS.Areas.Identity.Pages.Account;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class ForgotPasswordTests
{
    [TestMethod]
    [DataRow("invalid")]
    [DataRow("missing@example.test")]
    [DataRow("unconfirmed@example.test")]
    [DataRow("confirmed@example.test")]
    public async Task OnPost_ResetSaljeSamoZaPotvrdjenuAdresu(string email)
    {
        var user = new Korisnik();
        var users = Clan3TestContext.Users();
        users.Setup(m => m.FindByEmailAsync("unconfirmed@example.test")).ReturnsAsync(user);
        users.Setup(m => m.FindByEmailAsync("confirmed@example.test")).ReturnsAsync(user);
        users.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(email == "confirmed@example.test");
        users.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("test-token");
        var sender = new Mock<IEmailSender>();
        var page = Clan3TestContext.Page(new ForgotPasswordModel(users.Object, sender.Object)
            { Input = new ForgotPasswordModel.InputModel { Email = email } });
        if (email == "invalid")
            page.ModelState.AddModelError("Input.Email", "Neispravna adresa");

        var result = await page.OnPostAsync();
        if (email == "invalid")
        {
            Assert.IsInstanceOfType<PageResult>(result);
            users.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        }
        else
            Assert.AreEqual("./ForgotPasswordConfirmation", ((RedirectToPageResult)result).PageName);

        if (email == "confirmed@example.test")
            sender.Verify(m => m.SendEmailAsync(email, "Reset Password", It.Is<string>(s => s.Contains("a=1&amp;b=2"))), Times.Once);
        else
            sender.VerifyNoOtherCalls();
    }
}
