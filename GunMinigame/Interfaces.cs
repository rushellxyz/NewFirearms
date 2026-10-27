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
        public bool DragOnto(Item item); // Returns whether the load succeeded or was requested from the host.
    }

    // ну, ок, ладно
    // Optional capability: adapters without persistent jams continue to work.
    public interface IMinigameJammedGun
    {
        bool TryGetJam(out UnityEngine.Sprite sprite, out bool liveRound);
        void ClearJam();
    }

    public interface ILaser
    {
        public void Toggle();
        public bool IsEnabled();
    }
}
