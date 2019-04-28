using UnityEngine;

// Store normal of the object here.
// Used for debugging and viewing normal from the Unity Inspector (GUI).
public class ObjNormals : MonoBehaviour
{
    public Vector3 normal;
    void Update()
    {
        normal = gameObject.transform.forward;
    }
}
