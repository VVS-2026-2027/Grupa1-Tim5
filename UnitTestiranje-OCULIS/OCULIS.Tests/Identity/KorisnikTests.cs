using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OCULIS.Controllers;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
public class KorisnikTests
{
    [TestMethod]
    [DataRow("aktivan", true)]
    [DataRow("nepoznat", false)]
    public async Task Details_PostojanjeKorisnika_VracaProfilIliNotFound(string id, bool postoji)
    {
        var korisnik = new Korisnik { Id = "aktivan", Ime = "Ada" };
        var users = new Mock<UserManager<Korisnik>>(Mock.Of<IUserStore<Korisnik>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        users.Setup(u => u.FindByIdAsync(id)).ReturnsAsync(postoji ? korisnik : null);
        users.Setup(u => u.GetRolesAsync(korisnik)).ReturnsAsync(new List<string> { "Kupac" });
        var controller = new KorisnikController(users.Object, null!);

        var rezultat = await controller.Details(id);

        if (postoji)
        {
            Assert.AreSame(korisnik, ((ViewResult)rezultat).Model);
            CollectionAssert.AreEqual(new[] { "Kupac" }, ((IList<string>)controller.ViewBag.Uloge).ToArray());
        }
        else
        {
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        users.Verify(u => u.FindByIdAsync(id), Times.Once);
        users.Verify(u => u.GetRolesAsync(korisnik), postoji ? Times.Once() : Times.Never());
    }
}
