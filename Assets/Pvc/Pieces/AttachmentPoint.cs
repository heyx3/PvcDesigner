using System;
using System.Collections.Generic;
using UnityEngine;

namespace PVC
{
    public class AttachmentPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            var scale = transform.lossyScale;
            float radius = 0.006f * (scale.x + scale.y + scale.z) / 3;

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, radius);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position,
                            transform.position + (transform.forward * radius * 2.0f));
        }
    }
}