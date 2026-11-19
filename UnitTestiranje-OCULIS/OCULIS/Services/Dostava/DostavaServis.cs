namespace OCULIS.Services.Dostava;

public class DostavaServis : IDostavaServis
{
    private const double PragBesplatneDostave = 100d;
    private const double CijenaStandardneDostave = 7d;
    private const int BrojDecimalaValute = 2;

    public double IzracunajCijenuDostave(double iznosNakonPopusta)
    {
        ValidirajIznos(iznosNakonPopusta);

        var iznosZaPoredjenje = Math.Round(
            iznosNakonPopusta, BrojDecimalaValute, MidpointRounding.ToEven);

        return iznosZaPoredjenje >= PragBesplatneDostave
            ? 0d
            : CijenaStandardneDostave;
    }

    private static void ValidirajIznos(double iznosNakonPopusta)
    {
        if (!double.IsFinite(iznosNakonPopusta) || iznosNakonPopusta < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(iznosNakonPopusta), "Iznos nakon popusta ne može biti negativan ili neispravan.");
        }
    }
}
