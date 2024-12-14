using Unity.Mathematics;
using UnityEngine;

public class Compass : MonoBehaviour
{
    [SerializeField] private RectTransform[] rotate;
    [SerializeField] private RectTransform[] counterRotate;
    
    private void LateUpdate()
    {
        float azimuth = math.degrees(math.atan2(Camera.main.transform.forward.x, Camera.main.transform.forward.z));
        foreach (RectTransform rt in rotate)
            rt.localEulerAngles = new Vector3(0, 0, azimuth);
        foreach (RectTransform rt in counterRotate)
            rt.localEulerAngles = new Vector3(0, 0, -azimuth);
    }
}