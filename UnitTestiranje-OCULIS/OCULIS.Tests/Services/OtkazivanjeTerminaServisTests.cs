using OCULIS.Models;
using OCULIS.Services.Termin;

namespace OCULIS.Tests;

[TestClass]
public class OtkazivanjeTerminaServisTests
{
    [TestMethod]
    [DataRow(25, 0, StatusTermina.Zakazan, true)]
    [DataRow(24, 0, StatusTermina.Zakazan, true)]
    [DataRow(23, 59, StatusTermina.Zakazan, false)]
    [DataRow(-1, 0, StatusTermina.Zakazan, false)]
    [DataRow(0, 0, StatusTermina.Zakazan, false)]
    [DataRow(25, 0, StatusTermina.Otkazan, false)]
    [DataRow(25, 0, StatusTermina.Realizovan, false)]
    [DataRow(25, 0, StatusTermina.Slobodan, false)]
    public void MozeOtkazati_VremenskaGranicaIStatus_VracaOcekivanuOdluku(
        int sati, int minute, StatusTermina status, bool ocekivano)
    {
        var sada = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.FromHours(1));
        var pocetak = sada.DateTime.AddHours(sati).AddMinutes(minute);
        var termin = new TerminPregleda
        {
            Datum = pocetak.Date,
            Vrijeme = pocetak.TimeOfDay,
            Status = status
        };
        var servis = new OtkazivanjeTerminaServis(new FiksniSat(sada));

        var rezultat = servis.MozeOtkazati(termin);

        Assert.AreEqual(ocekivano, rezultat);
        Assert.AreEqual(status, termin.Status);
    }

    [TestMethod]
    public void MozeOtkazati_NullTermin_BacaArgumentNullException()
    {
        var sat = new FiksniSat(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));
        var servis = new OtkazivanjeTerminaServis(sat);

        Assert.ThrowsExactly<ArgumentNullException>(() => servis.MozeOtkazati(null!));
    }

    private sealed class FiksniSat : TimeProvider
    {
        private readonly DateTimeOffset _sada;

        public FiksniSat(DateTimeOffset sada)
        {
            _sada = sada;
        }

        public override DateTimeOffset GetUtcNow() => _sada.ToUniversalTime();

        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.CreateCustomTimeZone("TestnaZona", _sada.Offset, "Testna zona", "Testna zona");
    }
}
