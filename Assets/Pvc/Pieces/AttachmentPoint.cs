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
            float radius = 0.0001f * (scale.x + scale.y + scale.z) / 3;

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, radius);
        }
    }
}