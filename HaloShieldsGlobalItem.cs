namespace HaloShields;

public class HaloShieldsGlobalItem : GlobalItem
{
    public override void SetDefaults(Item item)
    {
        item.healLife /= 10;
    }
}
