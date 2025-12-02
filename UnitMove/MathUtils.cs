using System.Numerics;

namespace UnitSimulator;

public static class MathUtils
{
    public static Vector2 SafeNormalize(Vector2 vector)
    {
        return vector.LengthSquared() < 0.0001f ? Vector2.Zero : Vector2.Normalize(vector);
    }

    public static Vector2 Rotate(Vector2 v, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }

    public static Vector2 CalculateSeparationVector(Unit unit, List<Unit> squad, float radius)
    {
        Vector2 separationVector = Vector2.Zero;
        foreach (var member in squad)
        {
            if (member == unit || member.IsDead) continue;
            float distance = Vector2.Distance(unit.Position, member.Position);
            if (distance < radius && distance > 0)
            {
                separationVector += SafeNormalize(unit.Position - member.Position) / distance;
            }
        }
        return separationVector;
    }

    public static Vector2 GetContactPoint(Vector2 posA, Vector2 posB, float radiusA, float radiusB)
    {
        Vector2 delta = posB - posA;
        float distance = delta.Length();
        if (distance < 0.0001f) return posA;

        float t = Math.Clamp(radiusA / (radiusA + radiusB), 0f, 1f);
        return posA + delta * t;
    }

    public static bool TryGetFirstCollision(Unit a, Unit b, out float timeToHit, out Vector2 collisionPoint)
    {
        timeToHit = float.PositiveInfinity;
        collisionPoint = Vector2.Zero;
        if (a == b) return false;

        Vector2 dp = b.Position - a.Position;
        Vector2 dv = b.Velocity - a.Velocity;
        float combinedRadius = (a.Radius + b.Radius) * Constants.COLLISION_RADIUS_SCALE;
        float radiusSq = combinedRadius * combinedRadius;

        float A = Vector2.Dot(dv, dv);
        float B = 2f * Vector2.Dot(dp, dv);
        float C = Vector2.Dot(dp, dp) - radiusSq;

        if (C <= 0f)
        {
            timeToHit = 0f;
            collisionPoint = GetContactPoint(a.Position, b.Position, a.Radius, b.Radius);
            return true;
        }

        if (A < 0.000001f) return false;

        float discriminant = B * B - 4f * A * C;
        if (discriminant < 0f) return false;

        float sqrtDisc = MathF.Sqrt(discriminant);
        float t0 = (-B - sqrtDisc) / (2f * A);
        float t1 = (-B + sqrtDisc) / (2f * A);

        if (t0 < 0f) t0 = t1;
        if (t0 < 0f) return false;

        timeToHit = t0;
        Vector2 posA = a.Position + a.Velocity * timeToHit;
        Vector2 posB = b.Position + b.Velocity * timeToHit;
        collisionPoint = GetContactPoint(posA, posB, a.Radius, b.Radius);
        return true;
    }
}