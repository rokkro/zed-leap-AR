using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* For firing and placing objects. Can attach objects to planes or can have them float midair. 
 * Uses the SurfacePlacementManager for positioning objects.
 * This script can also select random prefabs to fire or place from a set of provided directories.
 * Specify directories in either randomPrefabDirectories or in the Unity Inspector GUI.
 * Do not attach this to a collision gameobject itself.
 */
public class CollisionObjectManager : MonoBehaviour
{
    // Assign these to something in the Inspector. 
    // Will be used if useRandomPrefab is false when using the fireKey or placementKey.
    public GameObject objectFire; 
    public GameObject objectPlace; 

    // Whether or not placing or firing objects with the fireKey or placementKey will use a random prefab from one of the directories given
    public bool useRandomPrefab = true; 

    // Force at which it is being fired
    public int force = 10;

    // Allow objects to be placed at camera position when there is no plane to place instead
    public bool allowPlacingInMidair = false;

    // Holds directory paths to prefabs. Should be under Assets/Resources/
    // When trying to select a random prefab, one of these directories will be picked and one of the prefabs within it will be chosen.
    public List<string> randomPrefabDirectories = new List<string>();

    // Keybindings for clearing, firing, and placing objects
    public KeyCode fireKey = KeyCode.Space;
    public KeyCode placementKey = KeyCode.Mouse2;
    public KeyCode clearKey = KeyCode.Escape;

    // Lock preventing rapid successive object creation
    bool creationLock = false;
    public float creationLockInterval = 1f;

    SurfacePlacementManager surfacePlacementManager;
    Camera LeftCamera;

    // List containing all objects instantiated with this script
    List<GameObject> spawnedObjects = new List<GameObject>();

    // Limits number of objects that can be spawned for performance reasons.
    public int spawnLimit = 10;

    // How far objects are allowed to be before they are destroyed
    public float maxDistanceFromCameraAllowed = 5f;

    // deltaTime interval we test the distance an object is from the camera
    float currentDistanceTestTimeInterval = 0f;
    float requiredDistanceTestTimeInterval = 5f;

    string colliderGOtag = "portalCollider";

    // Start is called before the first frame update
    void Start()
    {
        ZEDManager manager = FindObjectOfType(typeof(ZEDManager)) as ZEDManager;
        LeftCamera = manager.GetLeftCameraTransform().gameObject.GetComponent<Camera>();
        surfacePlacementManager = FindObjectOfType(typeof(SurfacePlacementManager)) as SurfacePlacementManager;
    }

    // Locks rapid creation of instances using a timer
    private IEnumerator InstanceCreationLock()
    {
        creationLock = true;
        yield return new WaitForSeconds(creationLockInterval);
        creationLock = false;
    }

    // Tries to get random prefab to fire and passes it on to objFire(GameObject objToFire) to actually fire it
    bool objFire()
    {
        GameObject randomObject = getRandomPrefab();
        if (randomObject == null)
            return false;
        return objFire(randomObject);
    }

    // Instantiates and fires object provided from the direction the camera is facing and adds velocity.
    // Get its rigidbody (physics) component. Add the component if it doesnt have one, then adds force.
    bool objFire(GameObject objToFire)
    {
        if (creationLock)
            return false;
        if (spawnedObjects.Count > spawnLimit)
            return false;

        GameObject newInstance = Instantiate(objToFire, LeftCamera.transform.position, LeftCamera.transform.rotation);

        Rigidbody rb = newInstance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = newInstance.AddComponent<Rigidbody>() as Rigidbody;
            rb.useGravity = false;
        }
        // The direction the camera is facing * force. This becomes its velocity.
        rb.AddForce(LeftCamera.transform.forward.normalized * force);

        spawnedObjects.Add(newInstance);

        // Lock creation of new game objects for a short time period
        if (creationLockInterval > 0 && !creationLock)
            StartCoroutine(InstanceCreationLock());

        return true;
    }

    // Tries to get a random prefab for placement and passes it to objPlacement(GameObject objToPlace, ..., ...) to actually place it
    public bool objPlacement(Vector3 placementPosition, Vector3 surfaceNormal)
    {
        GameObject randomObject = getRandomPrefab();
        if (randomObject == null)
            return false;
        return objPlacement(randomObject, placementPosition, surfaceNormal);
    }

    // Instantiates and places object at provided position and normal of a surface.
    public bool objPlacement(GameObject objToPlace, Vector3 placementPosition, Vector3 surfaceNormal)
    {
        if (creationLock)
            return false;
        if (spawnedObjects.Count > spawnLimit)
            return false;

        GameObject newInstance = Instantiate(objToPlace, LeftCamera.transform.position, LeftCamera.transform.rotation);   
        spawnedObjects.Add(newInstance);
        surfacePlacementManager.GetComponent<SurfacePlacementManager>().attachToSomeSurface(newInstance, placementPosition, surfaceNormal);

        // Lock creation of new game objects for a short period
        if (creationLockInterval > 0 && !creationLock)
            StartCoroutine(InstanceCreationLock());

        return true;
    }

    // Tries to get a random prefab for placement and passes it to objPlacementFromScreenSpace(GameObject objToPlace, ...) for placing
    bool objPlacementFromScreenSpace(Vector2 screenSpaceCoordinates) {
        GameObject randomObject = getRandomPrefab();
        if (randomObject == null)
            return false;
        return objPlacementFromScreenSpace(randomObject, screenSpaceCoordinates);
    }

    // Place object on ZED plane from screen space coordinates. 
    // For example, you click on where a plane is and it will place the object there.
    // Dont care as much here about the creation lock since it's easier to decide when objects are placed. 
    // Based on clicking instead of gestures or keyboard presses   
    bool objPlacementFromScreenSpace(GameObject objToPlace, Vector2 screenSpaceCoordinates)
    {
        if (spawnedObjects.Count > spawnLimit)
            return false;
        if (objToPlace == null)
            return false;

        GameObject newInstance = Instantiate(objToPlace, LeftCamera.transform.position, LeftCamera.transform.rotation);

        // Attach object to a ZED Plane from screen space
        bool result = surfacePlacementManager.GetComponent<SurfacePlacementManager>().attachToPlaneFromScreenSpace(newInstance, screenSpaceCoordinates);

        // If it couldn't place the object on a ZED Plane, either place it midair where the camera is or destroy it
        // Depends on the allowPlacingInMidair flag
        if (!result && !allowPlacingInMidair)
            Destroy(newInstance);
        else
            spawnedObjects.Add(newInstance);

        return true;
    }

    // Clears all spawned collision objects
    public void objAllClear()
    {
        foreach (GameObject go in spawnedObjects)
        {
            Destroy(go);
        }
        spawnedObjects = new List<GameObject>();
    }

    // Get distance between camera and the object. If the distance is too far, then delete the object. 
    // No point in having objects flying so far away we cant see them
    void destroyDistantObjects()
    {
        for (int i=0;i<spawnedObjects.Count;i++)
        {
            if (Vector3.Distance(spawnedObjects[i].transform.position, LeftCamera.transform.position) > maxDistanceFromCameraAllowed)
            {
                Destroy(spawnedObjects[i]);
            }
        }
    }

    // Remove any destroyed objects from the list
    private void removeOldObjects()
    {
        spawnedObjects.RemoveAll(x => x == null);
    }

    // Update is called once per frame
    // Handle pressing the fire key, the placement key, the clear key,
    //  and object distance checking (on an interval).
    void Update()
    {
        if (Input.GetKeyDown(fireKey))
        {
            if (useRandomPrefab)
                objFire();
            else
                objFire(objectFire);
        }
        else if (Input.GetKeyDown(placementKey))
        {
            if(useRandomPrefab)
                objPlacementFromScreenSpace(Input.mousePosition);
            else
                objPlacementFromScreenSpace(objectPlace,Input.mousePosition);
        }
        else if (Input.GetKeyDown(clearKey))
        {
            objAllClear();
        }

        // Checking for distant objects on a time interval
        currentDistanceTestTimeInterval += Time.deltaTime;
        if (currentDistanceTestTimeInterval >= requiredDistanceTestTimeInterval)
        {
            destroyDistantObjects();
            currentDistanceTestTimeInterval = 0;
        }
        removeOldObjects();
    }

    // Goes through the random prefab directories and tries to get a random prefab
    // Specify directories through the inspector, or manually add to randomPrefabDirectories list
    // If no directories are specified, it will search the entire project for gameObjects, most of which may not be set up for portal collisions
    // If requireColliderTag is true, then the random prefab is checked if it has the colliderGOtag
    GameObject getRandomPrefab(bool requireColliderTag = true)
    {
        while (true)
        {
            string randomDir;
            try
            {
                randomDir = randomPrefabDirectories[Random.Range(0, randomPrefabDirectories.Count)];
            }
            catch
            {
                randomDir = randomPrefabDirectories[0];
            }
            GameObject[] allObjects = Resources.LoadAll<GameObject>(randomDir);
            GameObject selected = allObjects[Random.Range(0, allObjects.Length - 1)];

            // Test if random object has the colliderGOtag. If not, select a new one.
            if (requireColliderTag)
            {
                if (selected.tag == colliderGOtag)
                    return selected;
            }
            else
                return selected;   
        }
    }
}
