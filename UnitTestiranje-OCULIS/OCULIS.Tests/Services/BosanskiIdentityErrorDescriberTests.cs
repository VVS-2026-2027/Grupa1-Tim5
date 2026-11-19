using System.Text.Json;
using OCULIS.Services.Identity;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
public class BosanskiIdentityErrorDescriberTests
{
    public static IEnumerable<object[]> PodaciZaLozinku()
    {
        var putanja = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "PorukeLozinke.json");

        using var dokument = JsonDocument.Parse(
            File.ReadAllText(putanja));

        foreach (var zapis in dokument.RootElement.EnumerateArray())
        {
            yield return new object[]
            {
                zapis.GetProperty("MinimalnaDuzina").GetInt32(),
                zapis.GetProperty("OcekivanaPoruka").GetString()!
            };
        }
    }

    [TestMethod]
    [DynamicData(nameof(PodaciZaLozinku), DynamicDataSourceType.Method)]
    public void PasswordTooShort_JsonPodaci_VracaOcekivanuPoruku(
        int minimalnaDuzina, string ocekivanaPoruka)
    {
        // Arrange - priprema
        var opis = new BosanskiIdentityErrorDescriber();

        // Act - poziv metode
        var rezultat = opis.PasswordTooShort(minimalnaDuzina);

        // Assert - provjera rezultata
        Assert.AreEqual("PasswordTooShort", rezultat.Code);
        Assert.AreEqual(ocekivanaPoruka, rezultat.Description);
    }

    [TestMethod]
    public void PasswordMismatch_Poziv_VracaPorukuPogresneLozinke()
    {
        // Arrange - priprema
        var opis = new BosanskiIdentityErrorDescriber();

        // Act - poziv metode
        var rezultat = opis.PasswordMismatch();

        // Assert - provjera rezultata
        Assert.AreEqual("PasswordMismatch", rezultat.Code);
        Assert.AreEqual("Pogrešna lozinka.", rezultat.Description);
    }

    [TestMethod]
    public void InvalidToken_Poziv_VracaPorukuNevazecegTokena()
    {
        // Arrange - priprema
        var opis = new BosanskiIdentityErrorDescriber();

        // Act - poziv metode
        var rezultat = opis.InvalidToken();

        // Assert - provjera rezultata
        Assert.AreEqual("InvalidToken", rezultat.Code);
        Assert.AreEqual("Nevažeći token.", rezultat.Description);
    }

    [TestMethod]
    public void DuplicateEmail_ZadanaAdresa_UkljucujeAdresuUPoruci()
    {
        // Arrange - priprema
        var opis = new BosanskiIdentityErrorDescriber();
        string email = "amila@example.com";

        // Act - poziv metode
        var rezultat = opis.DuplicateEmail(email);

        // Assert - provjera rezultata
        Assert.AreEqual("DuplicateEmail", rezultat.Code);
        Assert.AreEqual(
            "E-mail 'amila@example.com' je već zauzet.",
            rezultat.Description);
    }
    [TestMethod]
    public void InvalidUserName_NeispravnoIme_VracaOcekivanuPoruku()
    {
        // Arrange
        var opis = new BosanskiIdentityErrorDescriber();

        // Act
        var rezultat = opis.InvalidUserName("amila!");

        // Assert
        Assert.AreEqual("InvalidUserName", rezultat.Code);
        Assert.AreEqual(
            "Korisničko ime 'amila!' je nevažeće, može sadržavati samo slova i cifre.",
            rezultat.Description);
    }

    [TestMethod]
    public void InvalidEmail_NeispravnaAdresa_VracaOcekivanuPoruku()
    {
        // Arrange
        var opis = new BosanskiIdentityErrorDescriber();

        // Act
        var rezultat = opis.InvalidEmail("pogresna-adresa");

        // Assert
        Assert.AreEqual("InvalidEmail", rezultat.Code);
        Assert.AreEqual(
            "E-mail 'pogresna-adresa' je nevažeći.",
            rezultat.Description);
    }

    [TestMethod]
    public void InvalidRoleName_NeispravanNaziv_VracaOcekivanuPoruku()
    {
        // Arrange
        var opis = new BosanskiIdentityErrorDescriber();

        // Act
        var rezultat = opis.InvalidRoleName("uloga!");

        // Assert
        Assert.AreEqual("InvalidRoleName", rezultat.Code);
        Assert.AreEqual(
            "Naziv uloge 'uloga!' je nevažeći.",
            rezultat.Description);
    }

    [TestMethod]
    public void DefaultError_Poziv_VracaPorukuNepoznateGreske()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.DefaultError();

        Assert.AreEqual("DefaultError", rezultat.Code);
        Assert.AreEqual(
            "Došlo je do nepoznate greške.",
            rezultat.Description);
    }

    [TestMethod]
    public void ConcurrencyFailure_Poziv_VracaPorukuIzmjeneObjekta()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.ConcurrencyFailure();

        Assert.AreEqual("ConcurrencyFailure", rezultat.Code);
        Assert.AreEqual(
            "Optimistična konkurencija nije uspjela, objekt je izmijenjen.",
            rezultat.Description);
    }

    [TestMethod]
    public void LoginAlreadyAssociated_Poziv_VracaOcekivanuPoruku()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.LoginAlreadyAssociated();

        Assert.AreEqual("LoginAlreadyAssociated", rezultat.Code);
        Assert.AreEqual(
            "Korisnik s tim pristupom je već povezan.",
            rezultat.Description);
    }

    [TestMethod]
    public void DuplicateUserName_ZadanoIme_UkljucujeImeUPoruci()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.DuplicateUserName("amila");

        Assert.AreEqual("DuplicateUserName", rezultat.Code);
        Assert.AreEqual(
            "Korisničko ime 'amila' je već zauzeto.",
            rezultat.Description);
    }

    [TestMethod]
    public void DuplicateRoleName_ZadanaUloga_UkljucujeUloguUPoruci()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.DuplicateRoleName("Kupac");

        Assert.AreEqual("DuplicateRoleName", rezultat.Code);
        Assert.AreEqual(
            "Uloga 'Kupac' već postoji.",
            rezultat.Description);
    }
    [TestMethod]
    public void UserAlreadyHasPassword_Poziv_VracaOcekivanuPoruku()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.UserAlreadyHasPassword();

        Assert.AreEqual("UserAlreadyHasPassword", rezultat.Code);
        Assert.AreEqual(
            "Korisnik već ima postavljenu lozinku.",
            rezultat.Description);
    }

    [TestMethod]
    public void UserLockoutNotEnabled_Poziv_VracaOcekivanuPoruku()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.UserLockoutNotEnabled();

        Assert.AreEqual("UserLockoutNotEnabled", rezultat.Code);
        Assert.AreEqual(
            "Zaključavanje nije omogućeno za ovog korisnika.",
            rezultat.Description);
    }

    [TestMethod]
    public void UserAlreadyInRole_ZadanaUloga_UkljucujeUloguUPoruci()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.UserAlreadyInRole("Kupac");

        Assert.AreEqual("UserAlreadyInRole", rezultat.Code);
        Assert.AreEqual(
            "Korisnik je već u ulozi 'Kupac'.",
            rezultat.Description);
    }

    [TestMethod]
    public void UserNotInRole_ZadanaUloga_UkljucujeUloguUPoruci()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.UserNotInRole("Kupac");

        Assert.AreEqual("UserNotInRole", rezultat.Code);
        Assert.AreEqual(
            "Korisnik nije u ulozi 'Kupac'.",
            rezultat.Description);
    }

    [TestMethod]
    public void PasswordRequiresNonAlphanumeric_Poziv_VracaOcekivanuPoruku()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.PasswordRequiresNonAlphanumeric();

        Assert.AreEqual("PasswordRequiresNonAlphanumeric", rezultat.Code);
        Assert.AreEqual(
            "Lozinka mora sadržavati barem jedan znak koji nije slovo ili cifra.",
            rezultat.Description);
    }

    [TestMethod]
    public void PasswordRequiresDigit_Poziv_VracaOcekivanuPoruku()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.PasswordRequiresDigit();

        Assert.AreEqual("PasswordRequiresDigit", rezultat.Code);
        Assert.AreEqual(
            "Lozinka mora sadržavati barem jednu cifru ('0'-'9').",
            rezultat.Description);
    }

    [TestMethod]
    public void PasswordRequiresLower_Poziv_VracaOcekivanuPoruku()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.PasswordRequiresLower();

        Assert.AreEqual("PasswordRequiresLower", rezultat.Code);
        Assert.AreEqual(
            "Lozinka mora sadržavati barem jedno malo slovo ('a'-'z').",
            rezultat.Description);
    }

    [TestMethod]
    public void PasswordRequiresUpper_Poziv_VracaOcekivanuPoruku()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.PasswordRequiresUpper();

        Assert.AreEqual("PasswordRequiresUpper", rezultat.Code);
        Assert.AreEqual(
            "Lozinka mora sadržavati barem jedno veliko slovo ('A'-'Z').",
            rezultat.Description);
    }

    [TestMethod]
    public void PasswordRequiresUniqueChars_ZadaniBroj_UkljucujeBrojUPoruci()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.PasswordRequiresUniqueChars(3);

        Assert.AreEqual("PasswordRequiresUniqueChars", rezultat.Code);
        Assert.AreEqual(
            "Lozinka mora koristiti najmanje 3 različitih znakova.",
            rezultat.Description);
    }

    [TestMethod]
    public void RecoveryCodeRedemptionFailed_Poziv_VracaOcekivanuPoruku()
    {
        var opis = new BosanskiIdentityErrorDescriber();

        var rezultat = opis.RecoveryCodeRedemptionFailed();

        Assert.AreEqual("RecoveryCodeRedemptionFailed", rezultat.Code);
        Assert.AreEqual(
            "Kod za oporavak nije ispravan.",
            rezultat.Description);
    }
}