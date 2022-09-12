using System.ComponentModel;
using Terraria.Audio;
using Terraria.ModLoader.Config;

namespace HaloShields;

public class HaloShieldsClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Label("Sound Effects")]
    [Tooltip("Toggle the sound effects from the shield")]
    [DefaultValue(true)]
    public bool SoundEffects { get; set; }

    public override void OnChanged()
    {
        if (!SoundEffects)
        {
            var player = Main.player[Main.myPlayer].GetModPlayer<HaloShieldsPlayer>();
            if (player.sfxShieldEmpty is not null
                && SoundEngine.TryGetActiveSound(player.sfxShieldEmpty.Value, out var soundShieldEmpty))
                soundShieldEmpty.Stop();
            if (player.sfxShieldLow is not null
                && SoundEngine.TryGetActiveSound(player.sfxShieldLow.Value, out var soundShieldLow))
                soundShieldLow.Stop();
            if (player.sfxShieldRecharging is not null
                && SoundEngine.TryGetActiveSound(player.sfxShieldRecharging.Value, out var soundShieldRechargsfxShieldRecharging))
                soundShieldRechargsfxShieldRecharging.Stop();
        }
    }
}
