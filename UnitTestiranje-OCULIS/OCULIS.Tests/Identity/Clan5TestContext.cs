using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OCULIS.Models;

namespace OCULIS.Tests;

internal static class Clan5TestContext
{
    internal static Mock<UserManager<Korisnik>> Korisnici(
        Korisnik? korisnik = null, IUserStore<Korisnik>? spremiste = null)
    {
        var manager = new Mock<UserManager<Korisnik>>(
            spremiste ?? Mock.Of<IUserEmailStore<Korisnik>>(),
            Options.Create(new IdentityOptions()),
            null!, null!, null!, null!, null!, null!, null!);

        manager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(korisnik);
        manager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(korisnik?.Id ?? "nepoznat");
        manager.SetupGet(m => m.SupportsUserEmail).Returns(true);
        return manager;
    }

    internal static Mock<SignInManager<Korisnik>> Prijava(
        Mock<UserManager<Korisnik>> korisnici)
    {
        var manager = new Mock<SignInManager<Korisnik>>(
            korisnici.Object,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            Mock.Of<IUserClaimsPrincipalFactory<Korisnik>>(),
            Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<Korisnik>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<Korisnik>>());

        manager.Setup(m => m.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(Array.Empty<AuthenticationScheme>());
        return manager;
    }

    internal static T Stranica<T>(T stranica) where T : PageModel
    {
        var autentifikacija = new Mock<IAuthenticationService>();
        autentifikacija.Setup(a => a.SignOutAsync(
                It.IsAny<HttpContext>(), It.IsAny<string>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var servisi = new Mock<IServiceProvider>();
        servisi.Setup(s => s.GetService(typeof(IAuthenticationService)))
            .Returns(autentifikacija.Object);

        var http = new DefaultHttpContext
        {
            RequestServices = servisi.Object,
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "korisnik-1") }, "Test"))
        };
        http.Request.Scheme = "https";

        stranica.PageContext = new PageContext
        {
            HttpContext = http,
            RouteData = new RouteData(),
            ActionDescriptor = new CompiledPageActionDescriptor()
        };
        stranica.RouteData.Values["page"] = "/Account/Login";

        var url = new Mock<IUrlHelper>();
        url.SetupGet(u => u.ActionContext).Returns(stranica.PageContext);
        url.Setup(u => u.Content(It.IsAny<string>())).Returns("/");
        url.Setup(u => u.RouteUrl(
                It.IsAny<Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext>()))
            .Returns("https://example.test/potvrda?code=test&userId=korisnik-1");
        stranica.Url = url.Object;
        return stranica;
    }
}
