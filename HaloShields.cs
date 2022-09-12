namespace HaloShields;

public class HaloShields : Mod
{
    public const string AssetPath = "HaloShields/Assets/";

    public static HaloShields Instance { get; private set; }
    public static HaloShieldsClientConfig ClientConfig { get; private set; }

    public override void Load()
    {
        Instance = this;
        ClientConfig = ModContent.GetInstance<HaloShieldsClientConfig>();
    }

    public override void Unload()
    {
        Instance = null;
        ClientConfig = null;
    }
}
