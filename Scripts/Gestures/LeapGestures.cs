using System.Collections.Generic;
using UnityEngine;

/*
 * Handles hand gestures and their functionality. 
 * Menu and portal positioning, portal and portal markers, etc handled by Gesture scripts.
 * Hold the Leap Motion at a good angle when you first run the scene so the gestures dont get off.
 *   The leap may start with a different origin and idea of which axis is which. 
 *   Some gestures might need to be done at an angle in this case.
 */
namespace Leap.Unity
{
    public class LeapGestures : MonoBehaviour
    {
        // Assign these through the Unity Inspector
        public GameObject planeMarkerRight;
        public GameObject planeMarkerLeft;
        public GameObject attachmentObjectRight;
        public GameObject attachmentObjectLeft;
        public GameObject menuObject;

        GameObject leapRig;
        string leapRigName = "Leap Rig";

        // Grab the current frame from the LeapServiceProvider
        //  This contains information about the hands visible 
        public Frame getCurrentFrame
        {
            get
            {
                return leapRig.transform.GetChild(0).gameObject.GetComponent<LeapXRServiceProvider>().CurrentFrame;
            }
        }

        // Start is called before the first frame update
        private void Start()
        {
            leapRig = GameObject.Find(leapRigName);
        }

        // Go through each of our required pointing fingers
        //   If the current finger is among the required pointing fingers and is pointing, return true
        public bool checkCurrentFingerStatus(List<Finger> handFingers, List<Finger.FingerType> PointingFingers)
        {
            // Loop through all fingers on the current hand
            foreach (Finger finger in handFingers)
            {
                bool match = false;
                // Loop through all required types of fingers to be pointing (index finger, thumb, etc)
                foreach (Finger.FingerType pointer in PointingFingers)
                {
                    // If the current finger is one of the required pointing fingers and its pointing like it should be,
                    //     move on to the next finger to check
                    if (finger.Type == pointer && finger.IsExtended)
                    {
                        match = true;
                        break;
                    } 
                    // If the current finger is a required pointing finger but isnt pointing, point check failed
                    else if (finger.Type == pointer && !finger.IsExtended)
                    {
                        return false;
                    }
                }
                if (!match)
                {
                    // If the current finger was not in the required pointing fingers
                    //      and the current finger is not extended, then this finger is fine.
                    if (!finger.IsExtended)
                        continue;
                    else  // If this finger is pointing when it's not supposed to be, return false.
                        return false;
                }
            }
            // Return true because the correct fingers are pointing
            return true;
        }

        // Returns the finger object in a hand with the provided FingerType
        public Finger getFingerOfType(List<Finger> allFingers, Finger.FingerType fingerType)
        {
            foreach(Finger finger in allFingers)
            {
                if (finger.Type == fingerType)
                    return finger;
            }
            return null;
        }
    }
}