using System.Collections.Generic;
using UnityEngine;

/* Handle functionality of holding out your hand, with all fingers extended.
 * Palm up will spawn a single object in your hand, 
 *  based on a random prefab from the collisionObjectManager.
 * Palm down will despawn all objects, excluding portals + markers.
 */
namespace Leap.Unity
{
    public class GesturePalm : MonoBehaviour
    {
        // This number of seconds with the palm down gesture to clear stuff
        // Good for preventing accidental clears
        float palmDownTime = 1f;
        float palmDownTimeCurrent = 0f;

        // Prevents immediate and constant spawning of objects when doing the palm up gesture.
        bool palmSpawnLock = false;

        // Required fingers to trigger gesture
        List<Finger.FingerType> allExtended = new List<Finger.FingerType> { Finger.FingerType.TYPE_THUMB, Finger.FingerType.TYPE_INDEX, Finger.FingerType.TYPE_MIDDLE, Finger.FingerType.TYPE_RING, Finger.FingerType.TYPE_PINKY };

        CollisionObjectManager collisionObjectManager;
        PortalManager portalManager;
        LeapGestures leapGestures;

        // Start is called before the first frame update
        void Start()
        {
            collisionObjectManager = FindObjectOfType(typeof(CollisionObjectManager)) as CollisionObjectManager;
            portalManager = FindObjectOfType(typeof(PortalManager)) as PortalManager;
            portalManager = portalManager.GetComponent<PortalManager>();
            leapGestures = GetComponent<LeapGestures>();
        }

        // Removed this gesture since it's very easy to accidentally trigger.
        bool handlePalmSide(Hand hand)
        {
            Vector3 palmNormal = UnityVectorExtension.ToVector3(hand.PalmNormal);
            Vector3 palmPosition = UnityVectorExtension.ToVector3(hand.PalmPosition);
            Vector3 palmNormalNormalized = palmNormal.normalized;
            if (hand.IsLeft && (palmNormalNormalized.x >= .8 || palmNormalNormalized.z <= -.9 || palmNormalNormalized.z >= .9)) { }
            if (hand.IsRight && (palmNormalNormalized.x <= -.8 || palmNormalNormalized.z <= -.9 || palmNormalNormalized.z >= .9)) { }
            return false;
        }

        // Handle palm down action
        // Make sure the palm is facing down, then clear all objects and arrows spawned
        bool handlePalmDown(Hand hand)
        {
            Vector3 palmNormal = UnityVectorExtension.ToVector3(hand.PalmNormal);
            Vector3 palmNormalNormalized = palmNormal.normalized;

            if (palmNormalNormalized.y <= -0.8)
            {
                collisionObjectManager.GetComponent<CollisionObjectManager>().objAllClear();
                portalManager.destroyAllArrows();
                return true;
            }
            return false;
        }

        // Handle palm up action
        // Make sure it's facing up and then spawn a random prefab from the CollisionObjectManager in your palm
        // The palmSpawnLock prevents a bunch of objects being spawned immediately when the palm is facing up
        // The palmSpawnLock gets flipped off when you arent doing a palm up/down gesture.
        bool handlePalmUp(Hand hand)
        {
            Vector3 palmNormal = UnityVectorExtension.ToVector3(hand.PalmNormal);
            Vector3 palmPosition = UnityVectorExtension.ToVector3(hand.PalmPosition);
            Vector3 palmNormalNormalized = palmNormal.normalized;

            if (palmNormalNormalized.y >= 0.9)
            {
                if (palmSpawnLock)
                    return false;

                collisionObjectManager.GetComponent<CollisionObjectManager>().objPlacement(palmPosition, palmNormal);
                palmSpawnLock = true;
                return true;
            }
            return false;
        }

        // Update is called once per frame
        // Grab the current frame from LeapGestures
        //  This contains information about the hands visible 
        // Test if the required fingers on each hand are extended.
        // If they are, then test their direction and handle actions.
        // Some gestures are easy to accidentally trigger, so a timer is used to trigger it
        void Update()
        {
            foreach (Hand hand in leapGestures.getCurrentFrame.Hands)
            {
                bool allExtendResult = leapGestures.checkCurrentFingerStatus(hand.Fingers, allExtended);

                //// ALL FINGERS //// 
                if (allExtendResult)
                {
                    if (!handlePalmUp(hand))
                    {
                        // Handle palm down if holding the gesture for long enough
                        palmDownTimeCurrent += Time.deltaTime;
                        if (palmDownTimeCurrent >= palmDownTime)
                        {
                            handlePalmDown(hand);
                            palmDownTimeCurrent = 0;
                        }
                    }
                }
                else
                    palmSpawnLock = false;
            }
        }
    }
}