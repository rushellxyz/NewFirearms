using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;


namespace NewFirearms
{
    public class FirearmHandlingCourse : TutorialCourse
    {
        public const int LEFT_PAW = 14;

        public bool shouldPoint;
        public bool usedAutoPump;

        public override IEnumerator Sequence()
        {
            tutorial.forcePlayerAlive = false;
            usedAutoPump = false;
            body.skills.AddExp(2, 1000f);
            body.forceWalk = false;
            StartCoroutine(MedicalThingy());

            tutorial.Speak("Welcome to the firearm handling course");
            yield return new WaitUntil(() => tutorial.finishedTalking);

            tutorial.objectToCreate = "newfirearms.m1911";
            yield return new WaitUntil(() => tutorial.grabInfo.grabbed);
            tutorial.handPos.y -= 15f;
            yield return new WaitUntil(() => tutorial.handPos == tutorial.handPosCurrent);
            tutorial.handPos = body.transform.position + Vector3.up * 4f;
            yield return new WaitUntil(() => tutorial.handPos == tutorial.handPosCurrent);

            yield return new WaitForSeconds(1f);

            RshGun m1911 = tutorial.lastSpawnedObject.GetComponent<RshGun>();
            m1911.Rack(manual: true);
            shouldPoint = true;
            StartCoroutine(PointAt(m1911.transform, body.limbs[LEFT_PAW].transform));
            body.forceWalk = true;
            tutorial.Speak("Starting off from treating firearms related injuries");
            yield return new WaitUntil(() => tutorial.finishedTalking);

            yield return new WaitForSeconds(3f);
            Sound.Play(m1911.prop.shootAudio, m1911.transform.position);
            ShootManager.DrawVisuals(new ShootVisuals
            {
                start = m1911.transform.position,
                ends = new List<Vector2>() {
                    body.limbs[LEFT_PAW].transform.position,
                },
                hitNumbers = new List<(Vector2, ushort)>(),
            });
            PlayerDamage m1911damage = m1911.prop.ammoTypes[0].playerDamage;
            ShootManager.InduceDamage(body.limbs[LEFT_PAW], m1911damage, 1f);
            body.limbs[LEFT_PAW].MendBone();
            body.limbs[LEFT_PAW].shrapnel = 5;

            m1911.Rack(manual: false);
            yield return new WaitForSeconds(1f);

            tutorial.Speak("TODO");
            yield return new WaitForSeconds(10f);

            GlobalDark.main.Darken();
            yield return new WaitUntil(() => !GlobalDark.main.IsDarkening());
            SceneManager.LoadScene("PreGen");
        }

        public IEnumerator MedicalThingy()
        {
            while (true)
            {
                body.hunger = Mathf.Max(75.4f, body.hunger);
                body.averagePain = Mathf.Min(body.averagePain, 98f);
                body.bloodOxygen = Mathf.Max(body.bloodOxygen, 61f);
                body.shock = 0f;
                body.immunity = 199.4f;
                if (!usedAutoPump && !body.conscious && 30f > body.bloodPressure)
                {
                    body.WearWearable(Utils.Create("autopump", body.transform.position, 0f).GetComponent<Item>());
                    usedAutoPump = true;
                }
                yield return null;
            }
        }

        public IEnumerator PointAt(Transform original, Transform target)
        {
            while (true)
            {
                original.rotation = Quaternion.AngleAxis(Mathf.Atan2(target.position.y - original.position.y, target.position.x - original.position.x) * Mathf.Rad2Deg, Vector3.forward);
                if (shouldPoint)
                    yield return null;
           else     yield break;
            }
        }

        public override string LocaleName()
        {
            return "Firearm handling course";
        }
    }
}
