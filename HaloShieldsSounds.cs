using Terraria.Audio;

namespace HaloShields;

public static class HaloShieldsSounds
{
    public static SoundStyle Low => new($"{HaloShields.AssetPath}ShieldLow")
    {
        IsLooped = true,
        SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
        MaxInstances = 1
    };
    public static SoundStyle Empty => new($"{HaloShields.AssetPath}ShieldEmpty")
    {
        IsLooped = true,
        SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
        MaxInstances = 1
    };
    public static SoundStyle Recharging => new($"{HaloShields.AssetPath}ShieldRecharging")
    {
        IsLooped = true,
        SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
        MaxInstances = 1
    };
}
