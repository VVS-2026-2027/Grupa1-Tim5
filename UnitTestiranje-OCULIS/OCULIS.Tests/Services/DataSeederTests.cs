using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using OCULIS.Data;
using OCULIS.Models;
using OCULIS.Services.Seed;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
public class DataSeederTests
{
    private ApplicationDbContext db = null!;
    private Mock<RoleManager<IdentityRole>> uloge = null!;
    private Mock<UserManager<Korisnik>> korisnici = null!;
    private IConfiguration konfiguracija = null!;
    private ServiceProvider servisi = null!;

    [TestInitialize]
    public void Priprema()
    {
        db = TestContextFactory.Create();

        uloge = new Mock<RoleManager<IdentityRole>>(
            Mock.Of<IRoleStore<IdentityRole>>(),
            null!, null!, null!, null!);

        korisnici = new Mock<UserManager<Korisnik>>(
            Mock.Of<IUserStore<Korisnik>>(),
            Options.Create(new IdentityOptions()),
            null!, null!, null!, null!, null!, null!, null!);

        uloge.Setup(m => m.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        uloge.Setup(m => m.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Success);

        korisnici.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((Korisnik?)null);

        korisnici.Setup(m => m.CreateAsync(
                It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        korisnici.Setup(m => m.AddToRoleAsync(
                It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        konfiguracija = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:Password"] = "Test123!"
            })
            .Build();

        servisi = new ServiceCollection()
            .AddSingleton(db)
            .AddSingleton(uloge.Object)
            .AddSingleton(korisnici.Object)
            .AddSingleton<IConfiguration>(konfiguracija)
            .BuildServiceProvider();
    }

    [TestCleanup]
    public void Zavrsetak()
    {
        servisi.Dispose();
    }

    [TestMethod]
    public async Task SeedAsync_PraznaBaza_DodajePocetnePodatke()
    {
        await DataSeeder.SeedAsync(servisi);

        Assert.AreEqual(3, await db.Poslovnica.CountAsync());
        Assert.AreEqual(6, await db.Proizvod.CountAsync());
        Assert.AreEqual(2, await db.Akcija.CountAsync());

        uloge.Verify(m => m.CreateAsync(It.Is<IdentityRole>(
            r => r.Name == "Administrator")), Times.Once);

        uloge.Verify(m => m.CreateAsync(It.Is<IdentityRole>(
            r => r.Name == "Zaposlenik")), Times.Once);

        uloge.Verify(m => m.CreateAsync(It.Is<IdentityRole>(
            r => r.Name == "Kupac")), Times.Once);

        korisnici.Verify(m => m.CreateAsync(
            It.IsAny<Korisnik>(), "Test123!"), Times.Exactly(3));

        korisnici.Verify(m => m.AddToRoleAsync(It.Is<Korisnik>(
            k => k.Email == "admin@oculis.ba"),
            "Administrator"), Times.Once);

        korisnici.Verify(m => m.AddToRoleAsync(It.Is<Korisnik>(
            k => k.Email == "zaposlenik@oculis.ba"),
            "Zaposlenik"), Times.Once);

        korisnici.Verify(m => m.AddToRoleAsync(It.Is<Korisnik>(
            k => k.Email == "kupac@oculis.ba"),
            "Kupac"), Times.Once);
    }

    [TestMethod]
    public async Task SeedAsync_PostojeciPodaci_NeDupliraZapise()
    {
        await DataSeeder.SeedAsync(servisi);

        uloge.Setup(m => m.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        korisnici.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new Korisnik());

        await DataSeeder.SeedAsync(servisi);

        Assert.AreEqual(3, await db.Poslovnica.CountAsync());
        Assert.AreEqual(6, await db.Proizvod.CountAsync());
        Assert.AreEqual(2, await db.Akcija.CountAsync());

        uloge.Verify(m => m.CreateAsync(
            It.IsAny<IdentityRole>()), Times.Exactly(3));

        korisnici.Verify(m => m.CreateAsync(
            It.IsAny<Korisnik>(), It.IsAny<string>()),
            Times.Exactly(3));

        korisnici.Verify(m => m.AddToRoleAsync(
            It.IsAny<Korisnik>(), It.IsAny<string>()),
            Times.Exactly(3));
    }

    [TestMethod]
    public async Task SeedAsync_BezLozinke_NeKreiraKorisnike()
    {
        konfiguracija["Seed:Password"] = null;

        await DataSeeder.SeedAsync(servisi);

        korisnici.Verify(m => m.CreateAsync(
            It.IsAny<Korisnik>(), It.IsAny<string>()),
            Times.Never);

        Assert.AreEqual(3, await db.Poslovnica.CountAsync());
        Assert.AreEqual(6, await db.Proizvod.CountAsync());
        Assert.AreEqual(2, await db.Akcija.CountAsync());
    }

    [TestMethod]
    public async Task SeedAsync_KreiranjeKorisnikaNeUspije_NeDodjeljujeUlogu()
    {
        korisnici.Setup(m => m.CreateAsync(
                It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "TestGreska",
                Description = "Kreiranje korisnika nije uspjelo."
            }));

        await DataSeeder.SeedAsync(servisi);

        korisnici.Verify(m => m.CreateAsync(
            It.IsAny<Korisnik>(), It.IsAny<string>()),
            Times.Exactly(3));

        korisnici.Verify(m => m.AddToRoleAsync(
            It.IsAny<Korisnik>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task SeedAsync_KreiranjeBaciIzuzetak_ProsljedjujeIzuzetak()
    {
        korisnici.Setup(m => m.CreateAsync(
                It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ThrowsAsync(
                new InvalidOperationException("Testni izuzetak."));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => DataSeeder.SeedAsync(servisi));

        korisnici.Verify(m => m.AddToRoleAsync(
            It.IsAny<Korisnik>(), It.IsAny<string>()),
            Times.Never);
    }
}