namespace NewFirearms
{
    public partial class RshGun
    {
        internal void ApplyReimport(GunJson definition)
        {
            StopFiring();
            prop = definition;
            if (!prop.fireModes.Contains(fireMode)) fireMode = prop.fireModes[0];
            UpdateSprite();
        }
    }
}
