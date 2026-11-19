using Microsoft.Extensions.Logging;
using Moq;
using OCULIS.Services.Email;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
public class EmailServiceTests
{
    [TestMethod]
    public async Task SendEmailAsync_IspravniPodaci_ZapisujePoruku()
    {
        // Arrange - priprema
        var logger = new Mock<ILogger<EmailService>>();
        var servis = new EmailService(logger.Object);

        string email = "amila@example.com";
        string naslov = "Potvrda";
        string sadrzaj = "Testna poruka";

        // Act - poziv metode
        await servis.SendEmailAsync(email, naslov, sadrzaj);

        // Assert - provjera poziva loggera
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((zapis, tip) =>
                    zapis.ToString()!.Contains(email) &&
                    zapis.ToString()!.Contains(naslov) &&
                    zapis.ToString()!.Contains(sadrzaj)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}