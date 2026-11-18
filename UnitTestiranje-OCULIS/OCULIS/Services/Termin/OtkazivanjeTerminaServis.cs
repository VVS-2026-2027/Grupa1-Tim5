using OCULIS.Models;

namespace OCULIS.Services.Termin
{
    public interface IOtkazivanjeTerminaServis
    {
        bool MozeOtkazati(TerminPregleda termin);
    }

    public class OtkazivanjeTerminaServis : IOtkazivanjeTerminaServis
    {
        private static readonly TimeSpan MinimalnoVrijemeDoTermina = TimeSpan.FromHours(24);
        private readonly TimeProvider _sat;

        public OtkazivanjeTerminaServis(TimeProvider sat)
        {
            _sat = sat;
        }

        public bool MozeOtkazati(TerminPregleda termin)
        {
            ArgumentNullException.ThrowIfNull(termin);

            if (termin.Status != StatusTermina.Zakazan)
                return false;

            var pocetakTermina = termin.Datum.Date.Add(termin.Vrijeme);
            var sada = _sat.GetLocalNow().DateTime;
            return pocetakTermina - sada >= MinimalnoVrijemeDoTermina;
        }
    }
}
