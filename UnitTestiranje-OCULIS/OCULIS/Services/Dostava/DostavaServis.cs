namespace OCULIS.Services.Dostava;

public class DostavaServis : IDostavaServis
{
    public double IzracunajCijenuDostave(double iznosNakonPopusta)
    {
        if (double.IsNaN(iznosNakonPopusta)
            || double.IsInfinity(iznosNakonPopusta)
            || iznosNakonPopusta < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(iznosNakonPopusta), "Iznos nakon popusta ne može biti negativan ili neispravan.");
        }

        var zaokruzeniIznos = Math.Round(iznosNakonPopusta, 2);
        return zaokruzeniIznos >= 100d ? 0d : 7d;
    }
}
