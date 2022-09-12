using Terraria.GameContent.UI.Elements;

namespace HaloShields;

public class HaloShieldsUI : UIState
{
    private static readonly Color shieldColor = new(120, 160, 255);
    private static readonly Color panicColor = new(255, 0, 0);
    private UIPanel background, shield;

    public override void OnInitialize()
    {
        background = new(
            ModContent.Request<Texture2D>($"{HaloShields.AssetPath}ShieldBarEmpty"),
            ModContent.Request<Texture2D>($"{HaloShields.AssetPath}ShieldBarBorder")
        )
        {
            Left = new(-300, 0),
            Top = new(12, 0),
            Width = new(240, 0),
            Height = new(24, 0),

            HAlign = 1.0f,

            PaddingTop = 0,
            PaddingBottom = 0,
            PaddingLeft = 0,
            PaddingRight = 0,

            BackgroundColor = shieldColor.MultiplyValue(0.5),
            BorderColor = shieldColor.MultiplyValue(0.25)
        };
        Append(background);

        shield = new(
            ModContent.Request<Texture2D>($"{HaloShields.AssetPath}ShieldBarFull"),
            ModContent.Request<Texture2D>($"{HaloShields.AssetPath}ShieldBarBorder")
        )
        {
            Left = new(0, 0),
            Top = new(0, 0),
            Width = new(0, 1),
            Height = new(0, 1),

            BackgroundColor = shieldColor,
            BorderColor = shieldColor.MultiplyValue(0.5)
        };
        background.Append(shield);
    }

    public override void Update(GameTime gameTime)
    {
        var player = Main.player[Main.myPlayer].GetModPlayer<HaloShieldsPlayer>();
        var shieldAmount = (float)player.ShieldAmount / player.ShieldMax;
        shield.Width = new(0, shieldAmount);
        if (shield.Width.GetValue(background.Width.Pixels) < 16)
            shield.Width.Set(16, 0);

        if (shieldAmount == 0)
        {
            background.BackgroundColor = panicColor.MultiplyValue(
                Math.Abs(Math.Sin(Main.timeForVisualEffects / 4)) * 0.5);
            background.BorderColor = panicColor.MultiplyValue(
                Math.Abs(Math.Sin(Main.timeForVisualEffects / 4)) * 0.25);

            shield.BackgroundColor = Color.Transparent;
            shield.BorderColor = Color.Transparent;
        }
        else
        {
            background.BackgroundColor = shieldColor.MultiplyValue(0.5);
            background.BorderColor = shieldColor.MultiplyValue(0.25);

            shield.BackgroundColor = shieldColor;
            shield.BorderColor = shieldColor.MultiplyValue(0.5);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        var player = Main.player[Main.myPlayer].GetModPlayer<HaloShieldsPlayer>();
        if (shield.IsMouseHovering || background.IsMouseHovering)
            Main.instance.MouseText($"{Math.Floor(player.ShieldAmount)} / {player.ShieldMax2}");
    }
}
