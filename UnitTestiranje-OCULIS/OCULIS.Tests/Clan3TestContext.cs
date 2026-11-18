using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Moq;
using OCULIS.Models;

namespace OCULIS.Tests;

internal static class Clan3TestContext
{
    internal static Mock<UserManager<Korisnik>> Users(Korisnik? user = null)
    {
        var mock = new Mock<UserManager<Korisnik>>(Mock.Of<IUserStore<Korisnik>>(),
            Options.Create(new IdentityOptions()), null!, null!, null!, null!, null!, null!, null!);
        mock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user!);
        return mock;
    }

    internal static T Controller<T>(T controller, string role) where T : Controller
    {
        var http = Http(role);
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return controller;
    }

    internal static T Page<T>(T page) where T : PageModel
    {
        page.PageContext = new PageContext
        {
            HttpContext = Http("Kupac"),
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
            ActionDescriptor = new CompiledPageActionDescriptor()
        };
        var url = new Mock<IUrlHelper>();
        url.SetupGet(u => u.ActionContext).Returns(page.PageContext);
        url.Setup(u => u.RouteUrl(It.IsAny<Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext>()))
            .Returns("https://example.test/confirmation?a=1&b=2");
        page.Url = url.Object;
        return page;
    }

    private static DefaultHttpContext Http(string role)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Test"))
        };
        http.Request.Scheme = "https";
        return http;
    }
}
