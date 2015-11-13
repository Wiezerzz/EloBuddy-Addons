using EloBuddy.SDK;
using SharpDX;

namespace wzGraves
{
    internal static class Utils
    {
        public static Vector3 ExtendVector3(this Vector3 vector, Vector3 direction, float distance)
        {
            if (vector.To2D().Distance(direction.To2D()) == 0)
            {
                return vector;
            }

            var edge = direction.To2D() - vector.To2D();
            edge.Normalize();

            var v = vector.To2D() + edge * distance;
            return new Vector3(v.X, v.Y, vector.Z);
        }
    }
}
