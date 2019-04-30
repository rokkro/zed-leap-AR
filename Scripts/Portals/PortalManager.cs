using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
 * Handles collision trigger events, appearance, and translation of objects between portals. 
 */

public class PortalManager : MonoBehaviour
{
    GameObject[] portalGOs;
    List<GameObject> debugArrows = new List<GameObject>();
    public GameObject arrowObject;
    string portalGOtag = "portal";
    public bool useDebugArrows = false;

    // Use tag to make sure portals dont to try to translate planes and other portals
    string colliderGOtag = "portalCollider";

    // Whether or not objects that pass through the portal have a new rotation 
    //   based on their old rotation combined with the exit portal's rotation.
    // This is best set to false when the portals are aligned for the infinite falling effect.
    public bool matchPortalRotation = true;

    // Limit number of debug arrows spawned
    public int arrowLimit = 8;

    // Used to track each object's portal entry/exit status.
    List<ObjectPortalState> portalObjects = new List<ObjectPortalState>();

    // For calling from the menu UI. Toggles arrows on/off and destroys them.
    public void toggleArrows()
    {
        useDebugArrows = !useDebugArrows;
        destroyAllArrows();
    }

    // For calling from the menu UI. Toggles rotation flag
    public void toggleMatchPortalRotation()
    {
        matchPortalRotation = !matchPortalRotation;
    }

    // Destroys all created arrows in the debugArrows list
    public void destroyAllArrows()
    {
        foreach (GameObject arrow in debugArrows)
            Destroy(arrow);
        debugArrows = new List<GameObject>();
    }

    // Create debugging arrows at a specified position and direction
    void instantiateArrow(Vector3 position, Vector3 direction)
    {
        if (arrowObject != null && debugArrows.Count < arrowLimit)
        {
            GameObject newInstance = Instantiate(arrowObject, position, Quaternion.LookRotation(direction, Vector3.up));
            debugArrows.Add(newInstance);
        }
    }

    // For the translation and rotation of the collision object (obj going throuh portals) between the entry and exit portal
    // This helped with finding the offset: https://answers.unity.com/questions/1240231/what-is-the-correct-way-to-move-objects-relative-t.html
    // Calculated rotation and position will be off slightly.
    private void translateBetweenPortals(GameObject colliderGO, GameObject currentExitPortal, GameObject currentEntryPortal, Vector3 localCollisionPosition)
    {
        // Spawn a debug arrow to see the entry position 
        if (useDebugArrows)
            instantiateArrow(colliderGO.transform.position, colliderGO.transform.forward);

        // Flip the local point x value so that objects come out the correct side. 
        // An object entering the left side of an entry portal should exit the right side of the other.
        localCollisionPosition.x = localCollisionPosition.x * -1;

        // Move the object to the exit portal in world space, taking the relative portal offset into account
        colliderGO.transform.position = currentExitPortal.transform.TransformPoint(localCollisionPosition);

        if (matchPortalRotation)
        {
            // Flip the forward direction coming out of the entry portal 
            //   so that it is pointing in the same general direction the object entering the portal is
            // Get the rotation difference between this direction and the direction of the moving object
            Quaternion rotationDiff = Quaternion.FromToRotation(-currentEntryPortal.transform.forward, colliderGO.GetComponent<Rigidbody>().velocity.normalized);

            // Set the initial rotation to be facing the direction the exit portal is facing.
            colliderGO.transform.rotation = Quaternion.LookRotation(currentExitPortal.transform.forward);

            // Combine the rotation difference and the current rotation of the object
            // Quaternion * operator applies the first rotation, then the second one in order.
            // Inversing the rotation difference makes objects come out in the correct direction.
            colliderGO.transform.rotation = Quaternion.Inverse(rotationDiff) * colliderGO.transform.rotation;
        }
        else
        {
            // Set the object rotation to be the forward rotation of the exit portal
            colliderGO.transform.rotation = Quaternion.LookRotation(currentExitPortal.transform.forward);
        }

        // Update the velocity of the object, maintaining its previous magnitude (speed) and giving it the new direction. 
        colliderGO.GetComponent<Rigidbody>().velocity = colliderGO.GetComponent<Rigidbody>().velocity.magnitude * colliderGO.transform.forward;

        // Spawn a debug arrow to see the exit position 
        if (useDebugArrows)
            instantiateArrow(colliderGO.transform.position, colliderGO.transform.forward);
    }

    // Return another active portal GO that isn't this one
    GameObject getOtherPortal(GameObject currentPortal)
    {
        if (portalGOs.Length <= 1)
        {
            return null;
        }
        foreach (GameObject portalGO in portalGOs)
        {
            if (portalGO.GetInstanceID() != currentPortal.GetInstanceID())
            {
                return portalGO;
            }
        }
        return null;
    }
    
    // Go through list of ObjectPortalState objects and return the one containing the currentCollisionObject with the specified id.
    // If not found, returns null.
    private ObjectPortalState getObjectFromCollisionObjectInstanceID(int id)
    {
        ObjectPortalState objps = portalObjects.Find(x => x.currentCollisionObject.GetInstanceID() == id);
        return objps;
    }

    // Handles collision exit events sent from either portal. Happens when an object ends contact w/ portal.
    // If the object isnt moving very fast, we want to wait to allow it to be portaled again. 
    // This is because multiple exit trigger events may be fired before it has really ended contact with the exit portal.
    // If we release the lock early, then it could immediately be portaled back to the entry portal, and then ping pong between portals.
    public void handleTriggerExit(GameObject portal, GameObject colliderGO)
    {
        ObjectPortalState currentObject = getObjectFromCollisionObjectInstanceID(colliderGO.GetInstanceID());

        if(currentObject == null || currentObject.currentEntryPortal == null || currentObject.currentExitPortal == null)
            return;

        // If the exit portal sent a triggerExit event, start a coroutine to wait until triggerexit events stop
        // Once they stop, then the object is reset
        if (currentObject.currentExitPortal.GetInstanceID() == portal.GetInstanceID())
        {
            float speedThreshold = 1.3f;

            // Dont bother checking triggers if it's moving fast enough
            Rigidbody rb = currentObject.currentCollisionObject.GetComponent<Rigidbody>();
            if (rb != null && rb.velocity.magnitude >= speedThreshold)
                currentObject.allowPortaling();
            else
            {
                currentObject.portalExitLock = true;
                if (!currentObject.verifyingExitTrigger)
                    StartCoroutine(currentObject.verifyExitTrigger());
            }
        }
    }

    // Handles collision enter events sent from either portal. Happens when an object makes contact w/ portal.
    // Finds the localCollisionPosition:
    //   Tell the collider to fire a ray in its forward direction to get the point of collision at the entry portal.
    //   Also use this to determine whether the object is actually aiming at the portal or not.
    //   Don't translate the object if it grazes the edge of the portal box collider
    // Creates ObjectPortalState object with the current portals and gameObject state,
    //  then calls translateBetweenPortals() to move the object to the exit portal, where handleTriggerExit() should eventually do the rest.
    public void handleTriggerEnter(GameObject entryPortal, GameObject colliderGO)
    {
        ObjectPortalState current = getObjectFromCollisionObjectInstanceID(colliderGO.GetInstanceID());
    
        // Dont start over if there's already an entry portal assigned.
        if (current!= null && current.currentEntryPortal != null)
            return;

        if (colliderGO.tag == colliderGOtag && portalGOs.Length > 1)
        {
            GameObject exitPortal = getOtherPortal(entryPortal);
            if (exitPortal == null) 
                return;

            Vector3 localCollisionPosition = getCollisionPointAtEntryPortal(colliderGO, entryPortal);
            if (localCollisionPosition == Vector3.zero)
                return;

            if (current == null)
                current = new ObjectPortalState();
            current.currentExitPortal = exitPortal;
            current.currentEntryPortal = entryPortal;
            current.currentCollisionObject = colliderGO;
            portalObjects.Add(current);

            translateBetweenPortals(colliderGO, exitPortal, entryPortal, localCollisionPosition);
        }
    }

    // Finds all game objects having the 'portal' tag. This will only work on active GameObjects.
    void findActivePortals()
    {
        portalGOs = GameObject.FindGameObjectsWithTag(portalGOtag);
    }

    // Update the particle effects of the portals when there is one portal instead of 2. 
    // For the background and smoke effects.
    // Enabling and disabling of particle effects thanks to: https://answers.unity.com/questions/1141480/enabling-and-disabling-child-particles.html
    void updateParticleEffectVisibility()
    {
        string smokeEffectName = "Smoke";
        string backdropEffectName = "Backdrop";
        foreach (GameObject portal in portalGOs)
        {
            ParticleSystem[] allParticleSystems = portal.GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem childPS in allParticleSystems)
            {
                if (childPS.name == smokeEffectName)
                {
                    ParticleSystem.EmissionModule childPSEmissionModule = childPS.emission;
                    // If another portal exists, enable the smoke
                    if (portalGOs.Length > 1)
                        childPSEmissionModule.enabled = true;
                    else
                        childPSEmissionModule.enabled = false;
                }
                else if (childPS.name == backdropEffectName)
                {
                    ParticleSystem.EmissionModule childPSEmissionModule = childPS.emission;
                    // If another portal exists, disable the backdrop
                    if (portalGOs.Length > 1)
                        childPSEmissionModule.enabled = false;
                    else
                        childPSEmissionModule.enabled = true;
                }
            }
        }
    }

    // Make the collider object fire a ray in the direction it's moving to try to hit a portal and return the collision position relative to the portal
    // Used to make objects hit a portal at a certain position and exit a portal at a similar position.
    // Sometimes only a small part of the object will trigger a portal collision event when the rest of the object isnt near the portal collider all.
    //   This happens when the object grazes the box collider edge of the portal. Returns Vector3.zero when this happens.
    public Vector3 getCollisionPointAtEntryPortal(GameObject colliderGO, GameObject currentEntryPortal)
    {
        Vector3 localPosition = Vector3.zero;
        Vector3 pointOfContact = Vector3.zero;
        float raycastLength = 20f;

        // Debug.DrawRay(colliderGO.transform.position, colliderGO.GetComponent<Rigidbody>().velocity.normalized * 1000, Color.white, 100.0f);

        // Get all objects the raycast hit. This is for when there are a bunch of objects entering a portal at the same time.
        RaycastHit[] hits;
        hits = Physics.RaycastAll(colliderGO.transform.position, colliderGO.GetComponent<Rigidbody>().velocity.normalized * raycastLength);

        if (hits.Length == 0)
            return Vector3.zero;

        foreach(RaycastHit hit in hits)
        {
            // Make sure it collides with a portal.
            if (hit.collider.gameObject.tag == portalGOtag)
            {
                pointOfContact = hit.point;

                // Transform the collision point from world space into space local to that of the portal
                localPosition = currentEntryPortal.transform.InverseTransformPoint(hit.point);

                return localPosition;
            }
            
        }
        return Vector3.zero;
    }

    // Align portalB to face portalA while retaining its distance.
    // Get absval of the vector distance between the portals.
    // Make portalB face portalA. Move portalB on top of PortalA.
    // Move portalB away from portalA based on its distance in world space.
    // This is good for creating the infinite falling effect.
    public void alignPortals()
    {
        if (portalGOs == null || portalGOs.Length < 2)
            return;
        GameObject portalA = portalGOs[0];
        GameObject portalB = portalGOs[1];
        float distance = Vector3.Distance(portalA.transform.position, portalB.transform.position);
        distance = Mathf.Abs(distance);
        portalB.transform.rotation = Quaternion.LookRotation(-portalA.transform.forward);
        portalB.transform.position = portalA.transform.position;
        portalB.transform.Translate(portalA.transform.forward * distance, Space.World);
    }

    // Make all portals have the same rotation of the first portal. For debugging.
    private void giveAllPortalsRotation()
    {
        if (portalGOs == null || portalGOs.Length < 1)
            return;
        Quaternion rot = portalGOs[0].transform.rotation;
        giveAllPortalsRotation(rot);
    }

    // Make all portals have the same rotation. For debugging.
    private void giveAllPortalsRotation(Quaternion rot)
    {
        if (portalGOs == null || portalGOs.Length < 1)
            return;
        foreach (GameObject portal in portalGOs)
        {
            portal.transform.rotation = rot;
        }
    }

    // Remove objects from the list if they no longer exist in the scene
    private void removeOldObjects()
    {
        portalObjects.RemoveAll(x => x.currentCollisionObject == null);
    }

    // Update is called once per frame
    void Update()
    {
        findActivePortals();
        updateParticleEffectVisibility();
        removeOldObjects();
    }
}

/* Stores information for every object that enters the portal.
 * Used to identify what the object's entry/exit portals are and when the object has left the exit portal.
 */
class ObjectPortalState{
    public GameObject currentEntryPortal { get; set; }
    public GameObject currentExitPortal { get; set; }
    public GameObject currentCollisionObject { get; set; }
    public bool portalExitLock { get; set; }
    public bool verifyingExitTrigger { get; private set; }

    private float resetTimerThreshold = 10f;
    private float currentResetTimer = 0f;

    // Reset values here, allowing it to enter any portal again.
    public void allowPortaling()
    {
        verifyingExitTrigger = false;
        currentEntryPortal = null;
        currentExitPortal = null;
        currentResetTimer = 0f;
    }
    // For handling multiple exit triggers that are fired almost at the same time
    // Doesnt unlock the portalExitLock until an exit trigger hasnt been fired within the checkInterval time period
    // Once we verify it has actually exited the portal, then reset the object, allowing it to enter/exit portals again.
    public IEnumerator verifyExitTrigger()
    {
        float checkInterval = .5f;

        // Lock making sure only one coroutine of this is running for this object
        verifyingExitTrigger = true;
        while (true)
        {
            // If an exit trigger hasnt been fired recently, unlock everything and exit the coroutine
            if (!portalExitLock)
            {
                allowPortaling();
                yield break;
            }
            else
            {
                portalExitLock = false;
                // Sleep and then try to see if the flag was flipped by an OnTriggerExit call during this period
                yield return new WaitForSeconds(checkInterval);
            }
        }
    }

    void Update()
    {
        // Reset the object if it has held a enter/exit portal state for too long
        // Might happen if an onTriggerExit event didnt get fired from the exit portal.
        currentResetTimer += Time.deltaTime;
        if(currentResetTimer >= resetTimerThreshold)
        {
            currentEntryPortal = null;
            currentExitPortal = null;
            currentResetTimer = 0f;

            // If velocity gets stuck at zero, add force
            Rigidbody rb = currentCollisionObject.GetComponent<Rigidbody>();
            if (rb != null && rb.velocity == Vector3.zero)
                rb.AddForce(currentCollisionObject.transform.forward * 10);
        }
    }
}
