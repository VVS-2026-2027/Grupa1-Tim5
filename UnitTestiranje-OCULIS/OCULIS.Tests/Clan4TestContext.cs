using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;
using OCULIS.Models;

namespace OCULIS.Tests;

internal static class Clan4TestContext
{
    internal static Mock<UserManager<Korisnik>> CreateUserManager()
    {
        return new Mock<UserManager<Korisnik>>(
            Mock.Of<IUserStore<Korisnik>>(),
            Options.Create(new IdentityOptions()), null!, null!, null!, null!, null!, null!, null!);
    }

    internal static Mock<RoleManager<IdentityRole>> CreateRoleManager()
    {
        return new Mock<RoleManager<IdentityRole>>(
            Mock.Of<IRoleStore<IdentityRole>>(), Array.Empty<IRoleValidator<IdentityRole>>(),
            Mock.Of<ILookupNormalizer>(), new IdentityErrorDescriber(), Mock.Of<Microsoft.Extensions.Logging.ILogger<RoleManager<IdentityRole>>>());
    }

    internal static Mock<SignInManager<Korisnik>> CreateSignInManager(UserManager<Korisnik> userManager)
    {
        return new Mock<SignInManager<Korisnik>>(
            userManager, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<Korisnik>>(),
            Options.Create(new IdentityOptions()), Mock.Of<Microsoft.Extensions.Logging.ILogger<SignInManager<Korisnik>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(), Mock.Of<IUserConfirmation<Korisnik>>());
    }

    internal static T Controller<T>(T controller) where T : Controller
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "Test"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return controller;
    }

    internal static T Page<T>(T page, string? role = null) where T : PageModel
    {
        var identity = role == null
            ? new ClaimsIdentity("Test")
            : new ClaimsIdentity([new Claim(ClaimTypes.Role, role), new Claim(ClaimTypes.NameIdentifier, "user-1")], "Test");
        page.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
            RouteData = new RouteData(),
            ActionDescriptor = new CompiledPageActionDescriptor()
        };
        var url = new Mock<IUrlHelper>();
        url.SetupGet(x => x.ActionContext).Returns(page.PageContext);
        url.Setup(x => x.Content(It.IsAny<string>())).Returns<string>(x => x);
        url.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://example.test/confirm");
        page.Url = url.Object;
        page.TempData = new TempDataDictionary(page.PageContext.HttpContext, Mock.Of<ITempDataProvider>());
        return page;
    }
}
