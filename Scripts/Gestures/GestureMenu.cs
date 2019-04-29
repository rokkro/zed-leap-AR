using System.Collections.Generic;
using UnityEngine;

/* Handle functionality of the menu spawn gesture (peace sign).
 * To spawn the menu, hold that gesture for long enough.
 * Once spawned, the menu can be moved around easily with the peace sign gesture.
 * Other menu actions are handled by the individual menu buttons and menuUI.cs.
 */
namespace Leap.Unity
{
    public class GestureMenu : MonoBehaviour
    {
        // This number of seconds with the menu gesture to spawn it
        float menuSpawnTime = 4f;
        float menuSpawnTimeCurrent = 0f;

        // Required fingers to trigger gesture
        List<Finger.FingerType> menuSign = new List<Finger.FingerType> { Finger.FingerType.TYPE_INDEX, Finger.FingerType.TYPE_MIDDLE };

        SurfacePlacementManager surfacePlacementManager;
        LeapGestures leapGestures;

        // Start is called before the first frame update
        void Start()
        {
            surfacePlacementManager = FindObjectOfType(typeof(SurfacePlacementManager)) as SurfacePlacementManager;
            leapGestures = GetComponent<LeapGestures>();
        }

        // Handle menu spawning and attachment
        // Attach the menu to the position the finger is plus an offset so that it doesnt spawn on the hand itself
        // Make the menu face the normal direction of hand's palm instead of finger
        bool handleMenuGesture(Hand hand)
        {
            Finger pointer = leapGestures.getFingerOfType(hand.Fingers, menuSign[0]);
            Vector3 fingerPos = UnityVectorExtension.ToVector3(pointer.TipPosition);
            Vector3 palmNormal = UnityVectorExtension.ToVector3(hand.PalmNormal);

            float offset;
            if (hand.IsLeft)
                offset = .3f;
            else
                offset = -.3f;

            Vector3 positionOffset = new Vector3(fingerPos.x + offset, fingerPos.y, fingerPos.z);
            surfacePlacementManager.GetComponent<SurfacePlacementManager>().attachToSomeSurface(leapGestures.menuObject, positionOffset, palmNormal);
            return true;
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
                bool peaceSignResult = leapGestures.checkCurrentFingerStatus(hand.Fingers, menuSign);
                //// PEACE SIGN (MENU) //// 
                if (leapGestures.menuObject.activeSelf && peaceSignResult)
                {
                    // Allow menu gesture to immediately reposition the menu if it's already spawned
                    handleMenuGesture(hand);
                }
                else if (peaceSignResult)
                {
                    menuSpawnTimeCurrent += Time.deltaTime;
                    if (menuSpawnTimeCurrent >= menuSpawnTime)
                    {
                        handleMenuGesture(hand);
                        menuSpawnTimeCurrent = 0;
                    }
                }
            }
        }
    }
}