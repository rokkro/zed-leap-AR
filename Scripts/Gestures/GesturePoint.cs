using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Handles functionality of pointing with an index finger.
 * A pointer line is attached to the end of the pointing finger.
 * If pointing at a surface, a marker will be placed there.
 * A marker can be turned into a portal with a 'thumbs up' gesture.
 * left hand = orange marker/portal, right hand = blue marker/portal.
 * Markers timeout if not turned into a portal soon enough.
 * You can also point at a portal and move it around. Not exactly smooth, but it works.
 */
namespace Leap.Unity
{
    public class GesturePoint : MonoBehaviour
    {
        // Name of spawned line render used for pointing at things
        string lineGOname = "_line";
        string portalTag = "portal";

        // Line pointer GOs
        GameObject leftPointer;
        GameObject rightPointer;

        // Required fingers needed to trigger gesture
        List<Finger.FingerType> indexPointingOnly = new List<Finger.FingerType> { Finger.FingerType.TYPE_INDEX };

        SurfacePlacementManager surfacePlacementManager;
        LeapGestures leapGestures;

        // Coroutines for marker timeouts/despawns
        IEnumerator markerTimeoutLeft;
        IEnumerator markerTimeoutRight;

        // How long the marker is allowed to be active before automatically disabling itself
        float markerTimeoutInterval = 10f;

        // For moving portals around with pointer
        bool portalMove = false;
        GameObject moveablePortal;
        float portalDistance;

        // Need to point at portal long enough to move it
        float pointingAtPortalTime = 2f;
        float pointingAtPortalTimeCurrent = 0f;

        // Need to point long enough to activate the sign
        float pointingTime = .5f;
        float pointingTimeCurrent = 0f;

        // Start is called before the first frame update
        void Start()
        {
            surfacePlacementManager = FindObjectOfType(typeof(SurfacePlacementManager)) as SurfacePlacementManager;
            leapGestures = GetComponent<LeapGestures>();
        }

        // Creates a line renderer gameobject named _line to be attached to the end of a pointing index finger
        // Make sure it shows up in white: https://answers.unity.com/questions/587380/linerenderer-drawing-in-pink.html
        void drawPointer(Vector3 fingerPos, Vector3 fingerDir, ref GameObject pointer)
        {
            LineRenderer lr;
            Material whiteDiffuseMat;

            if (pointer != null)
            {
                lr = pointer.GetComponent<LineRenderer>();
                lr.enabled = true;
                lr.SetPosition(0, fingerPos);
                lr.SetPosition(1, fingerDir * 20 + fingerPos);
                return;
            }

            pointer = new GameObject(lineGOname);
            lr = pointer.AddComponent<LineRenderer>() as LineRenderer;
            lr.enabled = true;
            lr.positionCount = 2;
            lr.startWidth = 0.0005f;
            lr.endWidth = 0.0005f;
            lr.startColor = Color.white;
            lr.endColor = Color.white;
            lr.SetPosition(0, fingerPos);
            lr.SetPosition(1, fingerDir * 20 + fingerPos);
            whiteDiffuseMat = new Material(Shader.Find("Unlit/Texture"));
            lr.material = whiteDiffuseMat;
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
            Finger pointer = leapGestures.getFingerOfType(hand.Fingers, indexPointingOnly[0]);
            Vector3 fingerPos = UnityVectorExtension.ToVector3(pointer.TipPosition);
            Vector3 fingerDir = UnityVectorExtension.ToVector3(pointer.Direction);

            if (hand.IsLeft)
                drawPointer(fingerPos, fingerDir, ref leftPointer);
            else
                drawPointer(fingerPos, fingerDir, ref rightPointer);

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
            if (hit.collider != null && hit.collider.gameObject.tag == portalTag)
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

            if (leapGestures.planeMarkerRight != null && hand.IsRight)
            {
                // Disable any old timers that may be running
                if (markerTimeoutRight != null)
                    StopCoroutine(markerTimeoutRight);
                surfacePlacementManager.GetComponent<SurfacePlacementManager>().attachToPlaneFromWorldSpace(leapGestures.planeMarkerRight, hit);
                markerTimeoutRight = markerTimeout(leapGestures.planeMarkerRight);
                StartCoroutine(markerTimeoutRight);
            }
            else if (leapGestures.planeMarkerLeft != null && hand.IsLeft)
            {
                // Disable any old timers that may be running
                if (markerTimeoutLeft != null)
                    StopCoroutine(markerTimeoutLeft);
                surfacePlacementManager.GetComponent<SurfacePlacementManager>().attachToPlaneFromWorldSpace(leapGestures.planeMarkerLeft, hit);
                markerTimeoutLeft = markerTimeout(leapGestures.planeMarkerLeft);
                StartCoroutine(markerTimeoutLeft);
            }
        }

        // Update is called once per frame
        // Grab the current frame from LeapGestures
        //  This contains information about the hands visible 
        // Test if the required fingers on each hand are extended.
        // If they are, then test their direction and handle actions.
        // Some gestures are easy to accidentally trigger, so a timer is used to trigger it
        void Update()
        {
            Frame currentFrame = leapGestures.getCurrentFrame;

            // Remove the pointer if no hand visible
            if (currentFrame.Hands.Count == 0) { 
                Destroy(leftPointer);
                Destroy(rightPointer);
            }
            foreach (Hand hand in currentFrame.Hands)
            {
                bool pointerResult = leapGestures.checkCurrentFingerStatus(hand.Fingers, indexPointingOnly);

                //// POINTER //// 
                // If the pointer is already active, continue to handle pointer this frame
                if (pointerResult && (leftPointer!=null || rightPointer!=null))
                    handlePointMarker(hand);
                else if (pointerResult)
                {
                    // Handle point gesture if holding gesture for long enough
                    pointingTimeCurrent += Time.deltaTime;
                    if (pointingTimeCurrent >= pointingTime)
                    {
                        handlePointMarker(hand);
                        pointingTimeCurrent = 0;
                    }
                }
                else
                {
                    if (hand.IsLeft)
                        Destroy(leftPointer);
                    else
                        Destroy(rightPointer);

                    // Stop moving the portal if we stop pointing
                    portalMove = false;
                }
            }
        }
    }
}
