using System.Collections.Generic;
using UnityEngine;

/* Handles the attachment (position and rotation) of objects (portals) to detected planes.
 * 
 * The planes serve as guides to place objects. Once we move an object to the plane's position, we delete the plane.
 * Automatic detection of planes can add a lot of unecessary colliders in our space. So it's best to clear them while we dont need them.
 */

public class SurfacePlacementManager : MonoBehaviour
{
    // ZED Camera/Plane Objects
    ZEDPlaneDetectionManager planeDetectionManager;
    ZEDManager manager;
    sl.ZEDCamera zedCamera;

    public GameObject firstObject;
    public GameObject secondObject;
    public GameObject thirdObject;

    public KeyCode firstObjectKey;
    public KeyCode secondObjectKey;
    public KeyCode thirdObjectKey;
    public KeyCode clearAllObject;

    // Bring in everything from the ZED Plugin
    void Start()
    {
        planeDetectionManager = FindObjectOfType(typeof(ZEDPlaneDetectionManager)) as ZEDPlaneDetectionManager;
        planeDetectionManager = planeDetectionManager.GetComponent<ZEDPlaneDetectionManager>();
        manager = FindObjectOfType(typeof(ZEDManager)) as ZEDManager;
        zedCamera = manager.zedCamera;
    }

    // Update is called once per frame
    // Handles specific keys for spawning portals and clearing them.
    // Portal blue should be left click, orange right click. 
    void Update()
    {
        if (Input.GetKeyDown(firstObjectKey))
            attachToPlaneFromScreenSpace(firstObject, Input.mousePosition);
        else if(Input.GetKeyDown(secondObjectKey))
            attachToPlaneFromScreenSpace(secondObject, Input.mousePosition);
        else if (Input.GetKeyDown(thirdObjectKey))
            attachToPlaneFromScreenSpace(thirdObject, Input.mousePosition);
        else if (Input.GetKeyDown(clearAllObject))
        {
            if(firstObject != null)
                firstObject.SetActive(false);
            if (secondObject != null)
                secondObject.SetActive(false);
            if (thirdObject != null)
                thirdObject.SetActive(false);
        }

    }

    // This is based on rays fired from a given raycast hit in world space.
    // First verify the ray hit a ZED Plane, then attach the object to that plane
    public bool attachToPlaneFromWorldSpace(GameObject attachmentObject, RaycastHit hit)
    {
        if (attachmentObject == null)
            return false;
        if (!checkIfHitPlane(hit))
            return false;
        attachToPlane(hit, attachmentObject);
        return true;
    }

    // This is based on rays fired from given coordinates in world space.
    // Fires the ray then passes it to attachToPlaneFromWorldSpace(..., RaycastHit hit) to attach to a plane
    public bool attachToPlaneFromWorldSpace(GameObject attachmentObject, Vector3 worldSpaceCoordinates,Vector3 direction)
    {
        if (attachmentObject == null)
            return false;
        RaycastHit hit;
        int rayDistance = 20;
        Physics.Raycast(worldSpaceCoordinates, direction * rayDistance, out hit);
        return attachToPlaneFromWorldSpace(attachmentObject, hit);
    }

    // Raycast from given screen position, using the first manager
    public RaycastHit raycastFromScreenSpace(Vector2 screenPosition)
    {
        List<ZEDManager> managers = ZEDManager.GetInstances();
        return raycastFromScreenSpace(screenPosition, managers[0]);
    }

    // Raycast from screen space to world space
    // The GetWorldPositionAtPixel function is innacurate when using ZED_Rig_Mono for some reason. May be a bug with the Unity plugin.
    // Raycast from the camera in the direction of the world space position we determined from the provided screen space position
    public RaycastHit raycastFromScreenSpace(Vector2 screenPosition, ZEDManager manager)
    {
        Camera leftcamera = manager.GetLeftCamera();
        RaycastHit hit;
        int raycastLength = 20;
        Vector3 worldPos;
        ZEDSupportFunctions.GetWorldPositionAtPixel(zedCamera, screenPosition, leftcamera, out worldPos);
        Vector3 direction = (worldPos - leftcamera.transform.position).normalized;
        // Debug.DrawRay(leftcamera.transform.position, direction  * raycastLength, Color.green, 30f);
        Physics.Raycast(leftcamera.transform.position, direction * raycastLength, out hit);
        return hit;
    }

    // Attach an object to a plane in screen space using the first manager.
    public bool attachToPlaneFromScreenSpace(GameObject attachmentObject, Vector2 screenPosition)
    {
        List<ZEDManager> managers = ZEDManager.GetInstances();
        return attachToPlaneFromScreenSpace(attachmentObject, screenPosition, managers[0]);
    }

    // This is based on rays fired from a given coordinate in screen space.
    // First raycast from screen space to world space and see if we hit a ZED Plane.
    // Then attach to that ZED Plane.
    public bool attachToPlaneFromScreenSpace(GameObject attachmentObject,Vector2 screenPosition, ZEDManager manager)
    {
        if (attachmentObject == null)
            return false;
        RaycastHit hit = raycastFromScreenSpace(screenPosition, manager);
        if (!checkIfHitPlane(hit))
            return false;
        attachToPlane(hit, attachmentObject);
        return true;
    }

    // Checks if the raycast hit an object with a ZEDPlaneGameObject component.
    // Only ZED Planes should have this component.
    // Returns true if it is a hit plane, false otherwise.
    public bool checkIfHitPlane(RaycastHit hit)
    {
        if(hit.collider != null)
        {
            ZEDPlaneGameObject zpgo = hit.collider.gameObject.GetComponent<ZEDPlaneGameObject>();
            if (zpgo == null)
            { 
                return false;
            }
            return true;
        }
        return false;
    }

    // Attach the object to some position in world space, using the normal of that spot to determine the object rotation.
    // Sets velocity to zero to keep the object from moving away.
    public void attachToSomeSurface(GameObject attachmentObject, Vector3 worldSpaceCoords, Vector3 surfaceNormal)
    {
        if (attachmentObject == null)
            return;

        attachmentObject.SetActive(true);

        Rigidbody rb = attachmentObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        attachmentObject.transform.position = worldSpaceCoords;
        attachmentObject.transform.rotation = Quaternion.LookRotation(surfaceNormal);
    }

    // Attach the gameObject with the name attachGameObjectName to the ZED Plane our raycast hits.
    // The hit should have been checked if it was a ZED Plane beforehand. 
    // Set velocity to zero, get the normal of the ZED Plane, and make the object rotation match the normal of the plane.
    // Move the object to the position on the plane our ray hit.
    // Lastly, clear all planes. This is to keep things performant and because planes can respawn easily.
    private void attachToPlane(RaycastHit hit, GameObject attachmentObject)
    {
        attachmentObject.SetActive(true);

        Rigidbody rb = attachmentObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        // Move the object to the impact point
        attachmentObject.transform.position = hit.point;

        // Get normal of plane 
        Vector3 normal = hit.collider.gameObject.GetComponent<ZEDPlaneGameObject>().worldNormal;

        // Rotate object around correct axis to match direction normal facing
        attachmentObject.transform.rotation = Quaternion.LookRotation(normal);

        // Clear all planes since we've positioned the attachment object in the right place
        planeDetectionManager.destroyAllPlanes();
    }
}
