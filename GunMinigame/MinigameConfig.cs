using System.Collections.Generic;

namespace GunMinigame
{
    public class MinigameConfig : MinigameInfo
    {
        public string mainExperimentPath;
        public string mainMilkyTriggerOnPath;
        public string mainMilkyTriggerOffPath;
        public string mainDunePath;
        public string sliderFrontPath;
        public string grabTrigger;
        public string sliderBackPath;
        public string backLayerPath;
        public string magazineBackLayerPath;
        public string barrelPath;
        public string rackedOnlyPath;
        public string unrackedOnlyPath;
        public string[] ammosInHandPaths;
        public string[] ammosInPtrPaths;
        public string receiverPath;
        public string magReleasePath;
        public string rackTriggerPath;
        public Dictionary<string, string> magazinePaths;
        public Dictionary<string, string[]> ammosInMagazinePaths;
        public string magazineDragPath;
        public string[] casingsPath;
        public string laserTogglePath;
        public string flashlightTogglePath;
        public string laserPath;
        public string fireTogglePath;
        public string[] fireModePaths;
        public string[] coverAnimationPaths;
    }
}
