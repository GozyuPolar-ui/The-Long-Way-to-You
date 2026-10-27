using UnityEngine;

namespace Prince
{
    internal static class ColliderUtility
    {
        public static Vector3 ClosestPoint(Collider collider, Vector3 point)
        {
            if (collider == null)
                return point;

            if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider)
                return collider.ClosestPoint(point);
            if (collider is MeshCollider meshCollider && meshCollider.convex)
                return meshCollider.ClosestPoint(point);

            return collider.bounds.ClosestPoint(point);
        }
    }
}
