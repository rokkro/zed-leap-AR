using UnityEngine;

/* Used for debugging purposes. 
 * Attach this script to an object to see data from the camera in the inspector */
public class CameraData : MonoBehaviour
{
    // ZED Camera/Plane Objects
    ZEDManager manager;
    Camera LeftCamera;

    void Awake()
    {
        manager = FindObjectOfType(typeof(ZEDManager)) as ZEDManager;
        LeftCamera = manager.GetLeftCameraTransform().gameObject.GetComponent<Camera>();
    }

    sl.ZEDCamera zedcam;
    sl.IMUData imudata;

    public Vector3 worldPosition;
    public Quaternion worldRotation;

    public Vector3 cameraPosition = Vector3.zero;
    public Quaternion cameraRotation = Quaternion.identity;

    public Vector3 linearAcceleration;
    public Quaternion fusedOrientation;
    public Vector3 angularVelocity;


    // Start is called before the first frame update
    void Start()
    {
        zedcam = manager.zedCamera;
    }

    // Update is called once per frame
    void Update()
    {
        // Get pos/rotation in world space
        worldPosition = LeftCamera.transform.position;
        worldRotation = LeftCamera.transform.rotation;

        // Get data from IMU sensor (Inertial Measurement Unit)
        zedcam.GetInternalIMUData(ref imudata, sl.TIME_REFERENCE.CURRENT);

        // Accelerometer raw data in m/s².
        linearAcceleration = imudata.linearAcceleration;

        // Orientation from gyro/accelerator fusion.
        fusedOrientation = imudata.fusedOrientation;

        // Gyroscope raw data in degrees/second.
        angularVelocity = imudata.angularVelocity;

        // Get pos/rotation in camera space
        zedcam.GetPosition(ref cameraRotation, ref cameraPosition, sl.REFERENCE_FRAME.CAMERA);
    }
}
