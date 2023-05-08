using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PVC
{
    public abstract class Piece : MonoBehaviour
    {
        public IReadOnlyList<AttachmentPoint> Points => points;
        private AttachmentPoint[] points;

        protected virtual void Start()
        {
            points = GetComponentsInChildren<AttachmentPoint>();
        }

        //TODO: Detect collisions with other pieces.
    }
}