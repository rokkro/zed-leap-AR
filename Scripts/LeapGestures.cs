using System.Collections.Generic;
using UnityEngine;
using System.Collections;

/*
 * Handles hand gestures and their functionality. 
 * Menu and portal positioning, portal and portal markers, etc.
 * Hold the Leap Motion at a good angle when you first run the scene so the gestures dont get off.
 *   The leap may start with a different origin and idea of which axis is which. 
 *   Some gestures might need to be done at an angle in this case.
 */
namespace Leap.Unity
{
    public class LeapGestures : MonoBehaviour
    {
        // Name of spawned line render used for pointing at things
        string lineGOname = "_line";

        // Assign these through the Unity Inspector
        public GameObject planeMarkerRight;
        public GameObject planeMarkerLeft;
        public GameObject attachmentObjectRight;
        public GameObject attachmentObjectLeft;
        public GameObject menuObject;

        // Fingers required for each gesture
        List<Finger.FingerType> allExtended = new List<Finger.FingerType> { Finger.FingerType.TYPE_THUMB, Finger.FingerType.TYPE_INDEX, Finger.FingerType.TYPE_MIDDLE, Finger.FingerType.TYPE_RING, Finger.FingerType.TYPE_PINKY };
        List<Finger.FingerType> indexPointingOnly = new List<Finger.FingerType> { Finger.FingerType.TYPE_INDEX };
        List<Finger.FingerType> thumbOnly = new List<Finger.FingerType> { Finger.FingerType.TYPE_THUMB };
        List<Finger.FingerType> menuSign = new List<Finger.FingerType> { Finger.FingerType.TYPE_INDEX, Finger.FingerType.TYPE_MIDDLE};

        // Prevents immediate and constant spawning of objects when doing the palm up gesture.
        bool palmSpawnLock = false;

        // Other objects and scripts from the project
        SurfacePlacementManager surfacePlacementManager;
        CollisionObjectManager collisionObjectManager;
        PortalManager portalManager;
        GameObject leapRig;
        string leapRigName = "Leap Rig";
        string portalTag = "portal";

        // How long the marker is allowed to be active before automatically disabling itself
        float markerTimeoutInterval = 10f;

        // This number of seconds with the menu gesture to spawn it
        float menuSpawnTime = 4f;
        float menuSpawnTimeCurrent = 0f;

        // This number of seconds with the thumbs up gesture to spawn portal
        // Good for preventing accidental portal spawns
        float thumbsUpTime = 1f;
        float thumbsUpTimeCurrent = 0f;

        // This number of seconds with the palm down gesture to clear stuff
        // Good for preventing accidental clears
        float palmDownTime = 1f;
        float palmDownTimeCurrent = 0f;

        // Coroutines for marker timeouts/despawns
        IEnumerator markerTimeoutLeft;
        IEnumerator markerTimeoutRight;

        // For moving portals around with pointer
        bool portalMove = false;
        GameObject moveablePortal;
        float portalDistance;

        // Need to point at portal long enough to move it
        float pointingAtPortalTime = 3f;
        float pointingAtPortalTimeCurrent = 0f;

        // Start is called before the first frame update
        void Start()
        {
            portalManager = FindObjectOfType(typeof(PortalManager)) as PortalManager;
            portalManager = portalManager.GetComponent<PortalManager>();
            collisionObjectManager = FindObjectOfType(typeof(CollisionObjectManager)) as CollisionObjectManager;
            surfacePlacementManager = FindObjectOfType(typeof(SurfacePlacementManager)) as SurfacePlacementManager;
            leapRig = GameObject.Find(leapRigName);
        }

        // Go through each of our required pointing fingers
        //   If the current finger is among the required pointing fingers and is pointing, return true
        bool checkCurrentFingerStatus(List<Finger> handFingers, List<Finger.FingerType> PointingFingers)
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
        Finger getFingerOfType(List<Finger> allFingers, Finger.FingerType fingerType)
        {
            foreach(Finger finger in allFingers)
            {
                if (finger.Type == fingerType)
                    return finger;
            }
            return null;
        }

        // Creates a line renderer gameobject named _line to be attached to the end of a pointing index finger
        // Make sure it shows up in white: https://answers.unity.com/questions/587380/linerenderer-drawing-in-pink.html
        void drawPointer(Vector3 fingerPos, Vector3 fingerDir)
        {
            GameObject go = new GameObject(lineGOname);
            LineRenderer lr = go.AddComponent<LineRenderer>() as LineRenderer;
            lr.enabled = true;
            lr.positionCount = 2;
            lr.startWidth = 0.0005f;
            lr.endWidth = 0.0005f;
            lr.startColor = Color.white;
            lr.endColor = Color.white;
            lr.SetPosition(0, fingerPos);
            lr.SetPosition(1, fingerDir * 20 + fingerPos);
            Material whiteDiffuseMat = new Material(Shader.Find("Unlit/Texture"));
            lr.material = whiteDiffuseMat;
        }

        // Loop through all gameObjects and destroy the ones with the name "_line"
        void disablePointers()
        {
            foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
                if(go.name == lineGOname)
                    Destroy(go);
        }

        // Disables marker if it's been active for too long
        private IEnumerator markerTimeout(GameObject marker)
        {
            yield return new WaitForSeconds(markerTimeoutInterval);
            if (marker.activeSelf)
            {
                marker.SetActive(false);
            }
        }

        // Handle thumbs up gesture
        // See if it is a thumbs up based on the direction it's pointing
        // If this is a thumbs up from the (right | left) hand and the (blue | orange) marker is on the wall,
        // Move the (blue | orange) portal to the position of the marker and remove the marker
        bool handleThumbsUp(Hand hand)
        {
            Finger thumb = getFingerOfType(hand.Fingers, thumbOnly[0]);
            Vector3 direction = UnityVectorExtension.ToVector3(thumb.Direction);
            Vector3 normalizedDirection = direction.normalized;

            if (normalizedDirection.y >= 0.9 && normalizedDirection.y > normalizedDirection.x && normalizedDirection.y > normalizedDirection.z)
            {
                if (hand.IsRight && planeMarkerRight.activeSelf)
                {
                    attachmentObjectRight.SetActive(true);
                    attachmentObjectRight.transform.position = planeMarkerRight.transform.position;
                    attachmentObjectRight.transform.rotation = planeMarkerRight.transform.rotation;
                    planeMarkerRight.SetActive(false);
                    return true;
                }
                else if (hand.IsLeft && planeMarkerLeft.activeSelf)
                {
                    attachmentObjectLeft.SetActive(true);
                    attachmentObjectLeft.transform.position = planeMarkerLeft.transform.position;
                    attachmentObjectLeft.transform.rotation = planeMarkerLeft.transform.rotation;
                    planeMarkerLeft.SetActive(false);
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
            Finger thumb = getFingerOfType(hand.Fingers, thumbOnly[0]);
            Vector3 direction = UnityVectorExtension.ToVector3(thumb.Direction);
            Vector3 normalizedDirection = direction.normalized;

            if (normalizedDirection.y <= -0.7)
            {
                attachmentObjectRight.SetActive(false);
                planeMarkerRight.SetActive(false);
                attachmentObjectLeft.SetActive(false);
                planeMarkerLeft.SetActive(false);
                return true;
            }
            return false;
        }

        // Reposition the portal based on where we're pointing.
        // Portal rotation faces the tip of the finger. 
        // Portal position retains distance from when it was initially placed.
        // Particle effects may not make it look very smooth while moving
        void movePortal(Vector3 fingerPos, Vector3 fingerDir)
        {
            moveablePortal.transform.position = (fingerPos + fingerDir * portalDistance);
            moveablePortal.transform.rotation = Quaternion.LookRotation(-fingerDir);
        }

        // Handle index finger pointing action
        // First clear and draw a new pointer line
        // Check if you're pointing at a portal. If you are pointing at a portal for long enough, handle moving the portal around.
        // Check if you're pointing at a plane. If you are, create a blue/orange marker on that plane, depending on your right/left hand.
        // Markers will timeout if the portal spawn gesture isnt done soon enough. 
        // Left hand = orange marker for orange portal, right = blue.
        void handlePointMarker(Hand hand)
        {
            Finger pointer = getFingerOfType(hand.Fingers, indexPointingOnly[0]);
            Vector3 fingerPos = UnityVectorExtension.ToVector3(pointer.TipPosition);
            Vector3 fingerDir = UnityVectorExtension.ToVector3(pointer.Direction);

            disablePointers();
            drawPointer(fingerPos, fingerDir);

            // Move portal instead of dealing with markers
            if (portalMove)
            {
                movePortal(fingerPos, fingerDir);
                return;
            }

            // Raycast in advance to see what we're pointing at
            RaycastHit hit;
            int castDistance = 20;
            Physics.Raycast(fingerPos, fingerDir * castDistance, out hit);

            if (hit.collider == null)
                return;

            // If pointing at a portal
            if(hit.collider!=null && hit.collider.gameObject.tag == portalTag)
            {
                pointingAtPortalTimeCurrent += Time.deltaTime;
                if (pointingAtPortalTimeCurrent > pointingAtPortalTime)
                {
                    moveablePortal = hit.collider.gameObject;
                    portalMove = true;
                    portalDistance = hit.distance;
                    pointingAtPortalTimeCurrent = 0f;
                }
                return;
            }

            // If pointing at a ZEDPlane, do marker placing.
            if (!surfacePlacementManager.GetComponent<SurfacePlacementManager>().checkIfHitPlane(hit))
                return;

            if (planeMarkerRight != null && hand.IsRight)
            {
                // Disable any old timers that may be running
                if (markerTimeoutRight != null) 
                    StopCoroutine(markerTimeoutRight); 
                surfacePlacementManager.GetComponent<SurfacePlacementManager>().attachToPlaneFromWorldSpace(planeMarkerRight, hit);
                markerTimeoutRight = markerTimeout(planeMarkerRight);
                StartCoroutine(markerTimeoutRight);
            } 
            else if (planeMarkerLeft != null && hand.IsLeft)
            {
                // Disable any old timers that may be running
                if (markerTimeoutLeft != null) 
                    StopCoroutine(markerTimeoutLeft);
                surfacePlacementManager.GetComponent<SurfacePlacementManager>().attachToPlaneFromWorldSpace(planeMarkerLeft, hit);
                markerTimeoutLeft = markerTimeout(planeMarkerLeft);
                StartCoroutine(markerTimeoutLeft);
            }
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

        // Handle menu spawning and attachment
        // Attach the menu to the position the finger is plus an offset so that it doesnt spawn on the hand itself
        // Make the menu face the normal direction of hand's palm instead of finger
        bool handleMenuGesture(Hand hand)
        {
            Finger pointer = getFingerOfType(hand.Fingers, menuSign[0]);
            Vector3 fingerPos = UnityVectorExtension.ToVector3(pointer.TipPosition);
            Vector3 palmNormal = UnityVectorExtension.ToVector3(hand.PalmNormal);

            float offset;
            if (hand.IsLeft)
                offset = .3f;
            else
                offset = -.3f;

            Vector3 positionOffset = new Vector3(fingerPos.x + offset, fingerPos.y, fingerPos.z);
            surfacePlacementManager.GetComponent<SurfacePlacementManager>().attachToSomeSurface(menuObject, positionOffset, palmNormal);
            return true;
        }

        // Removed this gesture since it's very easy to accidentally trigger.
        bool handlePalmSide(Hand hand)
        {
            Vector3 palmNormal = UnityVectorExtension.ToVector3(hand.PalmNormal);
            Vector3 palmPosition = UnityVectorExtension.ToVector3(hand.PalmPosition);
            Vector3 palmNormalNormalized = palmNormal.normalized;
            if (hand.IsLeft && (palmNormalNormalized.x >= .8 || palmNormalNormalized.z <= -.9 || palmNormalNormalized.z >= .9)){}
            if (hand.IsRight && (palmNormalNormalized.x <= -.8 || palmNormalNormalized.z <= -.9 || palmNormalNormalized.z >= .9)){}
            return false;
        }

        // Update is called once per frame
        // Grab the current frame from the LeapServiceProvider
        //  This contains information about the hands visible 
        // Test if the required fingers on each hand are extended.
        // If they are, then test their direction and handle actions.
        // Some gestures are easy to accidentally trigger, so a timer is used to trigger it
        void Update()
        {
            Frame currentFrame = leapRig.transform.GetChild(0).gameObject.GetComponent<LeapXRServiceProvider>().CurrentFrame;

            // Remove the pointer if no hand visible
            if (currentFrame.Hands.Count == 0)
                disablePointers();

            foreach (Hand hand in currentFrame.Hands)
            {
                bool thumbResult = checkCurrentFingerStatus(hand.Fingers, thumbOnly);
                bool pointerResult = checkCurrentFingerStatus(hand.Fingers, indexPointingOnly);
                bool allExtendResult = checkCurrentFingerStatus(hand.Fingers, allExtended);
                bool peaceSignResult = checkCurrentFingerStatus(hand.Fingers, menuSign);

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

                //// POINTER //// 
                if (pointerResult)
                    handlePointMarker(hand);
                else
                {
                    // If no finger was pointing, then disable the pointer
                    // Stop moving the portal if we stop pointing
                    disablePointers();
                    portalMove = false;
                }

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
                
                //// PEACE SIGN (MENU) //// 
                if (menuObject.activeSelf && peaceSignResult)
                {
                    // Allow menu gesture to immediately reposition the menu if it's already spawned
                    handleMenuGesture(hand);
                }
                else if (peaceSignResult)
                {
                    menuSpawnTimeCurrent += Time.deltaTime;
                    if(menuSpawnTimeCurrent >= menuSpawnTime)
                    {
                        handleMenuGesture(hand);
                        menuSpawnTimeCurrent = 0;
                    }
                }
            }
        }
    }
}