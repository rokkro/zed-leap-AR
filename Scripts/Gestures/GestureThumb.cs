using System.Collections.Generic;
using UnityEngine;

/* Handles thumbs up and thumbs down gestures.
 * Thumbs up will spawn a portal in place of a marker. 
 * If a blue marker is active, a thumbs up from the right hand will place a blue portal there.
 * Orange marker needs a thumbs up from the left hand.
 * Thumbs down despawns all portals and markers.
 * This gesture isn't detected as easily by the Leap Motion for some reason, 
 *  so it may require 'thumbs downing' at a weird down angle.
 */
namespace Leap.Unity {
    public class GestureThumb : MonoBehaviour
    {
        LeapGestures leapGestures;

        // List of required fingers
        List<Finger.FingerType> thumbOnly = new List<Finger.FingerType> { Finger.FingerType.TYPE_THUMB };
        
        // This number of seconds with the thumbs up gesture to spawn portal
        // Good for preventing accidental portal spawns
        float thumbsUpTime = 1f;
        float thumbsUpTimeCurrent = 0f;

        // Start is called before the first frame update
        void Start()
        {
            leapGestures = GetComponent<LeapGestures>();
        }

        // Handle thumbs up gesture
        // See if it is a thumbs up based on the direction it's pointing
        // If this is a thumbs up from the (right | left) hand and the (blue | orange) marker is on the wall,
        // Move the (blue | orange) portal to the position of the marker and remove the marker
        bool handleThumbsUp(Hand hand)
        {
            Finger thumb = leapGestures.getFingerOfType(hand.Fingers, thumbOnly[0]);
            Vector3 direction = UnityVectorExtension.ToVector3(thumb.Direction);
            Vector3 normalizedDirection = direction.normalized;

            if (normalizedDirection.y >= 0.9 && normalizedDirection.y > normalizedDirection.x && normalizedDirection.y > normalizedDirection.z)
            {
                if (hand.IsRight && leapGestures.planeMarkerRight.activeSelf)
                {
                    leapGestures.attachmentObjectRight.SetActive(true);
                    leapGestures.attachmentObjectRight.transform.position = leapGestures.planeMarkerRight.transform.position;
                    leapGestures.attachmentObjectRight.transform.rotation = leapGestures.planeMarkerRight.transform.rotation;
                    leapGestures.planeMarkerRight.SetActive(false);
                    return true;
                }
                else if (hand.IsLeft && leapGestures.planeMarkerLeft.activeSelf)
                {
                    leapGestures.attachmentObjectLeft.SetActive(true);
                    leapGestures.attachmentObjectLeft.transform.position = leapGestures.planeMarkerLeft.transform.position;
                    leapGestures.attachmentObjectLeft.transform.rotation = leapGestures.planeMarkerLeft.transform.rotation;
                    leapGestures.planeMarkerLeft.SetActive(false);
                    return true;
                }
            }
            return false;
        }

        // Handle thumbs down gesture
        // Leap motion seems to have difficulties detecting the thumb's position correctly when it's pointing down,
        //   so the threshold here is a little nicer.
        // Disables the portals and the wall markers.
        bool handleThumbsDown(Hand hand)
        {
            Finger thumb = leapGestures.getFingerOfType(hand.Fingers, thumbOnly[0]);
            Vector3 direction = UnityVectorExtension.ToVector3(thumb.Direction);
            Vector3 normalizedDirection = direction.normalized;

            if (normalizedDirection.y <= -0.7)
            {
                leapGestures.attachmentObjectRight.SetActive(false);
                leapGestures.planeMarkerRight.SetActive(false);
                leapGestures.attachmentObjectLeft.SetActive(false);
                leapGestures.planeMarkerLeft.SetActive(false);
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
                bool thumbResult = leapGestures.checkCurrentFingerStatus(hand.Fingers, thumbOnly);
                //// THUMBS UP/DOWN //// 
                if (thumbResult)
                {
                    // Handle thumbs up if holding the gesture for long enough
                    thumbsUpTimeCurrent += Time.deltaTime;
                    if (thumbsUpTimeCurrent >= thumbsUpTime)
                    {
                        handleThumbsUp(hand);
                        thumbsUpTimeCurrent = 0;
                    }
                    // Hard to accidentally do a thumbs down, so no timer
                    handleThumbsDown(hand);
                }
            }
        }
    }
}
