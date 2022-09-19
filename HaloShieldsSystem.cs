using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.Audio;
using Terraria.GameContent.Achievements;
using Terraria.ID;

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

        On.Terraria.Player.ItemCheck_UseLifeCrystal += OnItemCheck_UseLifeCrystal;
        On.Terraria.Player.ItemCheck_UseLifeFruit += OnItemCheck_UseLifeFruit;
        IL.Terraria.Player.Hurt += ILHurt;
        IL.Terraria.Player.PickupItem += ILPickupItem;

        IL.Terraria.Chest.SetupShop += ILUseShieldMax;
        IL.Terraria.Main.CanStartInvasion += ILUseShieldMax;
        IL.Terraria.Main.HelpText += ILUseShieldMax;
        IL.Terraria.Main.StartInvasion += ILUseShieldMax;
        IL.Terraria.Main.UpdateTime += ILUseShieldMax;
        IL.Terraria.Main.UpdateTime_SpawnTownNPCs += ILUseShieldMax;
        IL.Terraria.Main.UpdateTime_StartNight += ILUseShieldMax;
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

    #region IL Injection

    private static void OnItemCheck_UseLifeCrystal(On.Terraria.Player.orig_ItemCheck_UseLifeCrystal orig,
                                                   Player self, Item item)
    {
        var modPlayer = self.GetModPlayer<HaloShieldsPlayer>();
        if (item.type == ItemID.LifeCrystal
            && self.itemAnimation > 0
            && modPlayer.ShieldMax < 400
            && self.ItemTimeIsZero)
        {
            self.ApplyItemTime(item);
            modPlayer.ShieldMax += 20;
            modPlayer.ShieldMax2 += 20;
            modPlayer.ShieldAmount += 20;
            self.HealEffect(20);
            AchievementsHelper.HandleSpecialEvent(self, 0);
        }
    }

    private static void OnItemCheck_UseLifeFruit(On.Terraria.Player.orig_ItemCheck_UseLifeFruit orig,
                                                 Player self, Item item)
    {
        var modPlayer = self.GetModPlayer<HaloShieldsPlayer>();
        if (item.type == ItemID.LifeFruit
            && self.itemAnimation > 0
            && modPlayer.ShieldMax >= 400
            && modPlayer.ShieldMax < 500
            && self.ItemTimeIsZero)
        {
            self.ApplyItemTime(item);
            modPlayer.ShieldMax += 5;
            modPlayer.ShieldMax2 += 5;
            modPlayer.ShieldAmount += 5;
            self.HealEffect(5);
            AchievementsHelper.HandleSpecialEvent(self, 0);
        }
    }

    private static void ILHurt(ILContext il)
    {
        bool num2ge1 = false;
        bool num2lt1 = false;

        var c = new ILCursor(il);
        while (c.TryGotoNext(i => i.MatchLdcR8(1.0)))
        {
            if (c.Previous is null
                || c.Next?.Next is null
                || !(c.Previous?.Match(OpCodes.Ldloc_S) ?? false))
                continue;

            if (!num2ge1 && c.Next.Next.Match(OpCodes.Blt_Un))
            {
                c.Remove();
                c.Emit(OpCodes.Ldc_R8, 0.0);
                num2ge1 = true;
            }

            if (!num2lt1 && c.Next.Next.Match(OpCodes.Bge_Un_S))
            {
                c.Remove();
                c.Emit(OpCodes.Ldc_R8, 0.0);
                num2lt1 = true;
            }

            if (num2ge1 && num2lt1)
                return;
        }

        throw new Exception("Failed to inject IL into method Terraria.Player.Hurt");
    }

    private static void ILPickupItem(ILContext il)
    {
        var c = new ILCursor(il);
        if (!c.TryGotoNext(i => i.MatchLdcI4(20) && i.Next.MatchCall<Player>("Heal")))
            throw new Exception("Failed to inject IL into method Terraria.Player.PickupItem");

        c.Remove();
        c.Emit(OpCodes.Ldc_I4, 5);
    }

    private static void ILUseShieldMax(ILContext il)
    {
        var c = new ILCursor(il);
        while (c.TryGotoNext(i =>
            i.MatchLdfld<Player>(nameof(Player.statLifeMax))
            || i.MatchLdfld<Player>(nameof(Player.statLifeMax2))))
        {
            c.Remove();
            c.Emit(OpCodes.Callvirt, typeof(Player).GetMethod("GetModPlayer", Array.Empty<Type>())
                .MakeGenericMethod(typeof(HaloShieldsPlayer)));
            c.Emit(OpCodes.Ldfld, typeof(HaloShieldsPlayer).GetField(nameof(HaloShieldsPlayer.ShieldMax)));
            c.Emit(OpCodes.Conv_I4);
        }
    }

    #endregion
}
