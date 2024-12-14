using Unity.Mathematics;
using UnityEngine;

public class LeaveWarning : MonoBehaviour
{
    [SerializeField] private float warnExtent;
    [SerializeField] private GameObject container;

    private void Update()
    {
        float2 camPos = ((float3)Camera.main.transform.position).xz;
        container.SetActive(math.cmax(math.abs(camPos)) > warnExtent);
    }
}
