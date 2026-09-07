namespace GunMinigame
{
    public interface IMinigameGun
    {
        public bool IsRacked();
        public void Rack();
        public bool DragOnto(Item item); // Returns whenever load is success
        public void RemoveMag();
        public string CurrentMag();
    }

    public interface IMinigameMag
    {
        public void DragOnto(Item item);
    }
}
