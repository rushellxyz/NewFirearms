namespace NewFirearms
{
    public partial class RshGun
    {
        private bool halfRecoil;

        public void SetHalfRecoil(bool active) => halfRecoil = active;

        private float GetMuzzleRise(Body holder, float muzzleRise)
            => (holder == PlayerCamera.main?.body ? halfRecoil : IsClientHalfRecoilActive(holder))
                ? muzzleRise * 0.5f : muzzleRise;
    }
}
