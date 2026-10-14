namespace GunMinigame
{
    public interface IMinigameGun
    {
        public bool IsRacked();
        public byte CurrentFireMode();
        public void Rack();
        public void ToggleFireMode();
        public bool DragOnto(Item item); // Returns whenever load is success
        public void RemoveMag();
        public string CurrentMag();
    }

    public interface IMinigameMag
    {
        public void DragOnto(Item item);
    }

    public interface ILaser
    {
        public void Toggle();
        public bool IsEnabled();
    }
}
