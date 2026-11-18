using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCULIS.Models;
using OCULIS.Areas.Identity.Pages.Account.Manage;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class EmailTests
{
    private const string StaraAdresa = "old@example.test";
    private const string NovaAdresa = "new@example.test";

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Get_UcitavaAdresuIStatusPotvrde(bool potvrdjena)
    {
        var user = new Korisnik();
        var users = Clan3TestContext.Users(user);
        users.Setup(m => m.GetEmailAsync(user)).ReturnsAsync(StaraAdresa);
        users.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(potvrdjena);
        var page = Clan3TestContext.Page(new EmailModel(users.Object, null!, Mock.Of<IEmailSender>()));

        Assert.IsInstanceOfType<PageResult>(await page.OnGetAsync());
        Assert.AreEqual(StaraAdresa, page.Email);
        Assert.AreEqual(StaraAdresa, page.Input.NewEmail);
        Assert.AreEqual(potvrdjena, page.IsEmailConfirmed);
    }

    [TestMethod]
    public async Task Handleri_NepostojeciKorisnik_VracajuNotFound()
    {
        var sender = new Mock<IEmailSender>(MockBehavior.Strict);
        var page = Clan3TestContext.Page(new EmailModel(Clan3TestContext.Users().Object, null!, sender.Object));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnGetAsync());
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnPostChangeEmailAsync());
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnPostSendVerificationEmailAsync());
    }

    [TestMethod]
    [DataRow(false, "invalid", false)]
    [DataRow(true, "invalid", false)]
    [DataRow(false, StaraAdresa, true)]
    [DataRow(false, NovaAdresa, true)]
    [DataRow(true, StaraAdresa, true)]
    public async Task Post_ProvjeravaUnosISaljeOdgovarajucuPotvrdu(bool verifikacija, string email, bool validno)
    {
        var user = new Korisnik();
        var users = Clan3TestContext.Users(user);
        users.Setup(m => m.GetEmailAsync(user)).ReturnsAsync(StaraAdresa);
        users.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(user.Id);
        users.Setup(m => m.GenerateChangeEmailTokenAsync(user, NovaAdresa)).ReturnsAsync("change-token");
        users.Setup(m => m.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("confirm-token");
        var sender = new Mock<IEmailSender>();
        var page = Clan3TestContext.Page(new EmailModel(users.Object, null!, sender.Object)
            { Input = new EmailModel.InputModel { NewEmail = email } });
        if (!validno)
            page.ModelState.AddModelError("Input.NewEmail", "Neispravna adresa");

        var result = verifikacija ? await page.OnPostSendVerificationEmailAsync() : await page.OnPostChangeEmailAsync();
        if (validno)
            Assert.IsInstanceOfType<RedirectToPageResult>(result);
        else
        {
            Assert.IsInstanceOfType<PageResult>(result);
            Assert.AreEqual(StaraAdresa, page.Input.NewEmail);
        }

        if (validno && (verifikacija || email == NovaAdresa))
        {
            sender.Verify(m => m.SendEmailAsync(email, "Confirm your email", It.Is<string>(s => s.Contains("a=1&amp;b=2"))), Times.Once);
            if (verifikacija)
                users.Verify(m => m.GenerateEmailConfirmationTokenAsync(user), Times.Once);
            else
                users.Verify(m => m.GenerateChangeEmailTokenAsync(user, NovaAdresa), Times.Once);
        }
        else
            sender.VerifyNoOtherCalls();
    }
}
