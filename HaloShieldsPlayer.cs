using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Utilities;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Achievements;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace HaloShields;

public class HaloShieldsPlayer : ModPlayer
{
    private static double Difficulty
    {
        get
        {
            if (Main.GameModeInfo.IsJourneyMode)
            {
                var power = CreativePowerManager.Instance.GetPower<CreativePowers.DifficultySliderPower>();
                if (power is not null && power.GetIsUnlocked())
                {
                    return power.StrengthMultiplierToGiveNPCs;
                }
            }
            return Main.GameMode;
        }
    }

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

    private int DamageShield(int amount)
    {
        shieldRegenCooldown = 180 - ShieldRegenSpeed + (int)Math.Round(Math.Max(Difficulty - 1, 0) * 30);
        if (ShieldAmount == 0)
        {
            return amount;
        }
        else
        {
            ShieldAmount -= amount / Difficulty * 2;
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
            ShieldRegen = (Player.lifeRegen / 8 + Player.statDefense / 12) * (1.2 - Difficulty * 0.2);
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
            shieldDebuffCooldown = 120 / -(int)ShieldRegen;
            ShieldAmount -= 4;
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
                && (ShieldAmount / ShieldMax2) < 0.35
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
}
