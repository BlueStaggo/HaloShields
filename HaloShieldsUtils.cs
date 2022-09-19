using MonoMod.Cil;

namespace HaloShields;

public static class Utils
{
    public static Color MultiplyValue(this Color that, double scale)
    {
        return new Color((int)Math.Floor(that.R * scale),
                         (int)Math.Floor(that.G * scale),
                         (int)Math.Floor(that.B * scale),
                         that.A);
    }
}
