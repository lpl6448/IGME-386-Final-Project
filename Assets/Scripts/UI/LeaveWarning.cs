using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Enables or disables a UI element based on whether the camera is nearing the edge of the prepared simulation extent
/// </summary>
public class LeaveWarning : MonoBehaviour
{
    [SerializeField] private float warnExtent; // Extent of the "safe" square region where the warning is not active
    [SerializeField] private GameObject container; // Object to activate/deactivate

    private void Update()
    {
        // If either the x- or z-coordinates are outside the warning extent, enable the warning
        float2 camPos = ((float3)Camera.main.transform.position).xz;
        container.SetActive(math.cmax(math.abs(camPos)) > warnExtent);
    }
}
