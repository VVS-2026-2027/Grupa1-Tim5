using OCULIS.Models;

namespace OCULIS.Services.Termin
{
    public interface IOtkazivanjeTerminaServis
    {
        bool MozeOtkazati(TerminPregleda termin);
    }

    public class OtkazivanjeTerminaServis : IOtkazivanjeTerminaServis
    {
        public OtkazivanjeTerminaServis(TimeProvider sat)
        {
        }

        public bool MozeOtkazati(TerminPregleda termin)
        {
            throw new NotImplementedException();
        }
    }
}
