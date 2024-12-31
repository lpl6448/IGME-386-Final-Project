using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Rotates a series of UI elements to face north using the main camera
/// </summary>
public class Compass : MonoBehaviour
{
    // Rotate toward north
    [SerializeField] private RectTransform[] rotate;

    // Rotate the opposite direction, used to nest elements that do not rotate within the compass, like direction text
    [SerializeField] private RectTransform[] counterRotate;

    private void LateUpdate()
    {
        // Get the azimuth from the main camera's forward direction
        float azimuth = math.degrees(math.atan2(Camera.main.transform.forward.x, Camera.main.transform.forward.z));
        foreach (RectTransform rt in rotate)
            rt.localEulerAngles = new Vector3(0, 0, azimuth);
        foreach (RectTransform rt in counterRotate)
            rt.localEulerAngles = new Vector3(0, 0, -azimuth);
    }
}