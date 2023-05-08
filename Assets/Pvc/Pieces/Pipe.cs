using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PVC
{
    public class Pipe : Piece
    {
        public const float BASE_LENGTH = 3.048f; //10 feet

        public float Length => BASE_LENGTH * transform.localScale.x;
        public void SetLength(float newLength)
        {
            var ls = transform.localScale;
            ls.x = newLength / BASE_LENGTH;
            transform.localScale = ls;
        }
    }
}
