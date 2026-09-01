using KrokoshaCasualtiesMP;

namespace NewFirearms
{
    // patch to register recivers
    public class NetworkMsgRegister
    {
        public static void Postfix()
        { // What was wrong with uint? :dune_stare:
            Net.RegisterClientReceiver(31802, RshGun.ClientSync);
            Net.RegisterServerReceiver(31803, RshGun.ServerReceiver);
            Net.RegisterClientReceiver(31804, RshGun.ReciveShoot);
            Net.RegisterClientReceiver(31805, RshMag.ClientSync);
            Net.RegisterServerReceiver(31806, RshMag.ServerReceiver);
            Net.RegisterServerReceiver(31807, YOU_SHOULD_KILL_YOURSELF_NOOOWPatch.ReciveGunSuicideRequest);
        }
    }
}
