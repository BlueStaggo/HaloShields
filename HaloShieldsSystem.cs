using System.Collections.Generic;
using Terraria.Audio;

namespace HaloShields;

public class HaloShieldsSystem : ModSystem
{
    private HaloShieldsUI ui;
    private UserInterface uiInterface;

    public override void Load()
    {
        ui = new();
        ui.Activate();
        uiInterface = new();
        uiInterface.SetState(ui);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        uiInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int resourceBarsIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
        if (resourceBarsIndex != -1)
        {
            layers.Insert(resourceBarsIndex, new LegacyGameInterfaceLayer(
                "Halo Shields: Shield Bar",
                () =>
                {
                    uiInterface?.Draw(Main.spriteBatch, new GameTime());
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }
    }

    public override void OnWorldUnload()
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
