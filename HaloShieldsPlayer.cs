using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Utilities;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace HaloShields;

public class HaloShieldsPlayer : ModPlayer
{
    internal SlotId? sfxShieldRecharging = null;
    internal SlotId? sfxShieldEmpty = null;
    internal SlotId? sfxShieldLow = null;

    private int shieldRegenCooldown;
    private int shieldDebuffCooldown;

    public double ShieldAmount;
    public int ShieldMax;
    public int ShieldMax2;
    public double ShieldRegen;
    public int ShieldRegenSpeed;

    public override void Initialize()
    {
        shieldRegenCooldown = 0;
        shieldDebuffCooldown = 0;
        ShieldAmount = 100;
        ShieldMax = 100;
        ShieldMax2 = 100;
        ShieldRegen = 0;
        ShieldRegenSpeed = 0;
    }

    public override void Load()
    {
        On.Terraria.Player.ItemCheck_UseLifeCrystal += OnItemCheck_UseLifeCrystal;
        On.Terraria.Player.ItemCheck_UseLifeFruit += OnItemCheck_UseLifeFruit;
        IL.Terraria.Player.Hurt += ILHurt;
    }

    private int DamageShield(int amount)
    {
        shieldRegenCooldown = 180 - ShieldRegenSpeed;
        if (ShieldAmount == 0)
        {
            return amount;
        }
        else
        {
            ShieldAmount -= amount * (Main.GameModeInfo.EnemyMaxLifeMultiplier / 2 + 1.5);
            if (ShieldAmount < 0)
                ShieldAmount = 0;
            return 0;
        }
    }

    public override bool PreHurt(bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit,
                                 ref bool customDamage, ref bool playSound, ref bool genGore,
                                 ref PlayerDeathReason damageSource, ref int cooldownCounter)
    {
        customDamage = true;
        damage = DamageShield(damage);
        if (damage != 0)
            damage = (int)Main.CalculateDamagePlayersTake(damage, Player.statDefense);

        return true;
    }

    public override void OnRespawn(Player player)
    {
        shieldRegenCooldown = 0;
        shieldDebuffCooldown = 0;
        ShieldRegen = 0;
        ShieldRegenSpeed = 0;
        ShieldAmount = ShieldMax2;
    }

    public override void ResetEffects()
    {
        ShieldMax2 = ShieldMax;
        Player.statLifeMax = 100;
        Player.statLifeMax2 = 100;
    }

    public override void UpdateDead()
    {
        shieldRegenCooldown = 1;
        shieldDebuffCooldown = 1;
        ShieldRegen = 0;
        ShieldRegenSpeed = 0;
        ShieldMax2 = ShieldMax;
    }

    public override void LoadData(TagCompound tag)
    {
        ShieldAmount = tag.GetDouble("ShieldAmount");
        ShieldMax = tag.GetInt("ShieldMax");
        ShieldMax2 = tag.GetInt("ShieldMax2");
        shieldRegenCooldown = tag.GetInt("shieldRegenCooldown");
        shieldDebuffCooldown = tag.GetInt("shieldDebuffCooldown");
    }

    public override void SaveData(TagCompound tag)
    {
        tag.Set("ShieldAmount", ShieldAmount);
        tag.Set("ShieldMax", ShieldMax);
        tag.Set("ShieldMax2", ShieldMax2);
        tag.Set("shieldRegenCooldown", shieldRegenCooldown);
        tag.Set("shieldDebuffCooldown", shieldDebuffCooldown);
    }

    public override void UpdateLifeRegen()
    {
        if (ShieldAmount > 0)
        {
            ShieldRegen = (Player.lifeRegen + Player.statDefense) / 8;
            ShieldRegenSpeed = Math.Max(Player.lifeRegen * 10 - 10, 0);
        }
        Player.lifeRegen = 0;
    }

    public override void UpdateVisibleAccessories()
    {
        if (ShieldAmount > 0)
            Player.noFallDmg = true;
    }

    public override void PostUpdateBuffs()
    {
        shieldDebuffCooldown -= 1;
        if (ShieldRegen < 0 && shieldDebuffCooldown <= 0)
        {
            shieldRegenCooldown = 180;
            shieldDebuffCooldown = 120 / -(int)ShieldRegen;
            ShieldAmount -= 1;
        }
        else
        {
            shieldRegenCooldown -= 1;
            if (shieldRegenCooldown <= 0)
            {
                shieldRegenCooldown = 0;
                ShieldAmount += Math.Max(ShieldRegen, 1);
                ShieldAmount = Math.Clamp(ShieldAmount, 0, ShieldMax2);

                if (HaloShields.ClientConfig.SoundEffects)
                    sfxShieldRecharging ??= SoundEngine.PlaySound(HaloShieldsSounds.Recharging);
            }
        }

        if (HaloShields.ClientConfig.SoundEffects)
        {
            if (ShieldAmount > 0
                && (ShieldAmount / ShieldMax2) < 0.25
                && sfxShieldRecharging is null)
            {
                sfxShieldLow ??= SoundEngine.PlaySound(HaloShieldsSounds.Low);
            }
            else if (sfxShieldLow is not null
                && SoundEngine.TryGetActiveSound(sfxShieldLow.Value, out var soundShieldLow))
            {
                soundShieldLow.Stop();
                sfxShieldLow = null;
            }

            if (ShieldAmount == 0
                && Player.statLife > 0)
            {
                sfxShieldEmpty ??= SoundEngine.PlaySound(HaloShieldsSounds.Empty);
            }
            else if (sfxShieldEmpty is not null
                && SoundEngine.TryGetActiveSound(sfxShieldEmpty.Value, out var soundShieldEmpty))
            {
                soundShieldEmpty.Stop();
                sfxShieldEmpty = null;
            }

            if (sfxShieldRecharging is not null
                && (ShieldAmount == ShieldMax2 || shieldRegenCooldown > 0)
                && SoundEngine.TryGetActiveSound(sfxShieldRecharging.Value, out var soundShieldRecharging))
            {
                soundShieldRecharging.Stop();
                sfxShieldRecharging = null;
            }
        }

        ShieldAmount = Math.Clamp(ShieldAmount, 0, ShieldMax2);
    }

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

        Console.WriteLine("Injecting into Player.Hurt...");
        var c = new ILCursor(il);
        while (c.TryGotoNext(i => i.MatchLdcR8(1.0))) // ldc.r8 1
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
}
