using UnityEngine;

namespace Liv.Lck.Smoothing
{
    [DefaultExecutionOrder(1000)]
    public class LckStabilizer : MonoBehaviour
    {
        public Transform StabilizationTarget;
        public Transform TargetToFollow;

        public float PositionalSmoothing = 0.1f;
        public float RotationalSmoothing = 0.1f;

        public bool AffectPosition = true;
        public bool AffectRotation = true;

        private KalmanFilterVector3 _positionFilter;
        private KalmanFilterQuaternion _rotationFilter;

        private KalmanFilterVector3 PositionFilter => _positionFilter ??= new KalmanFilterVector3(StabilizationTarget.position);
        private KalmanFilterQuaternion RotationFilter => _rotationFilter ??= new KalmanFilterQuaternion(StabilizationTarget.rotation);

        private void LateUpdate()
        {
            if(AffectPosition)
            {
                StabilizationTarget.position = PositionFilter.Update(TargetToFollow.position, Time.deltaTime, PositionalSmoothing);
            }

            if(AffectRotation)
            {
                StabilizationTarget.rotation = RotationFilter.Update(TargetToFollow.rotation, Time.deltaTime, RotationalSmoothing);
            }
        }

        public void ReachTargetInstantly()
        {
            StabilizationTarget.position = PositionFilter.Update(TargetToFollow.position, Time.deltaTime, 0);
            StabilizationTarget.rotation = RotationFilter.Update(TargetToFollow.rotation, Time.deltaTime, 0);
        }
    }
}
