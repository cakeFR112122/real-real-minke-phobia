using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Liv.Lck.Smoothing
{
    public static class SmoothingUtils 
    {
        public static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector3 currentVelocity, float smoothTime)
        {
            if (Time.deltaTime == 0) return current;
            if (smoothTime == 0) return target;

            Vector3 c = current.eulerAngles;
            Vector3 t = target.eulerAngles;
            return Quaternion.Euler(
                    Mathf.SmoothDampAngle(c.x, t.x, ref currentVelocity.x, smoothTime),
                    Mathf.SmoothDampAngle(c.y, t.y, ref currentVelocity.y, smoothTime),
                    Mathf.SmoothDampAngle(c.z, t.z, ref currentVelocity.z, smoothTime)
                    );
        }
    }
}
