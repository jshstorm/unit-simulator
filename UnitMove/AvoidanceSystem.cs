using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace UnitSimulator;

public static class AvoidanceSystem
{
    public static Vector2 PredictiveAvoidanceVector(Unit mover, List<Unit> others, Vector2 desiredDirection, out Vector2 avoidanceTarget, out bool isDetouring, out Unit? avoidanceThreat)
    {
        avoidanceTarget = Vector2.Zero;
        avoidanceThreat = null;
        isDetouring = false;
        float moverRadius = mover.Radius * Constants.COLLISION_RADIUS_SCALE;
        float minSpeed = MathF.Max(mover.Speed, 0.001f);

        Vector2 baseDesiredDir = desiredDirection.LengthSquared() > 0.0001f
            ? MathUtils.SafeNormalize(desiredDirection)
            : (mover.Velocity.LengthSquared() > 0.0001f ? MathUtils.SafeNormalize(mover.Velocity) : mover.Forward);

        var risks = new List<(Vector2 relPos, float distance, float combinedRadius, Unit threat)>();

        foreach (var other in others)
        {
            if (other == mover || other.IsDead) continue;
            float combinedRadius = moverRadius + other.Radius * Constants.COLLISION_RADIUS_SCALE;

            Vector2 relativePos = other.Position - mover.Position;
            Vector2 relativeVel = other.Velocity - mover.Velocity;
            float relativeSpeedSq = relativeVel.LengthSquared();

            if (MathUtils.TryGetFirstCollision(mover, other, out var tCollision, out var _))
            {
                float collisionWindow = MathF.Min((combinedRadius * 2f) / minSpeed, Constants.AVOIDANCE_MAX_LOOKAHEAD);
                if (tCollision <= collisionWindow)
                {
                    Vector2 relAtCollision = (other.Position + other.Velocity * tCollision) - (mover.Position + mover.Velocity * tCollision);
                    float distanceAtCollision = relAtCollision.Length();
                    if (distanceAtCollision > 0.0001f)
                    {
                        risks.Add((relAtCollision, distanceAtCollision, combinedRadius, other));
                        continue;
                    }
                }
            }

            float tClosest = relativeSpeedSq < 0.0001f
                ? 0f
                : Math.Max(-Vector2.Dot(relativePos, relativeVel) / relativeSpeedSq, 0f);

            float timeWindow = MathF.Min((combinedRadius * 2f) / minSpeed, Constants.AVOIDANCE_MAX_LOOKAHEAD);

            Vector2 futureRelPos = relativePos + relativeVel * tClosest;
            float futureDistance = futureRelPos.Length();

            if (futureDistance < combinedRadius && tClosest <= timeWindow && futureDistance > 0.0001f)
            {
                risks.Add((relativePos, relativePos.Length(), combinedRadius, other));
                continue;
            }

            Vector2 heading = baseDesiredDir;
            float projection = Vector2.Dot(relativePos, heading);
            float lookaheadDistance = mover.Speed * Constants.AVOIDANCE_MAX_LOOKAHEAD + combinedRadius;
            if (projection > 0 && projection <= lookaheadDistance)
            {
                Vector2 lateral = relativePos - heading * projection;
                if (lateral.Length() < combinedRadius)
                {
                    risks.Add((relativePos, projection, combinedRadius, other));
                }
            }
        }

        if (!risks.Any())
        {
            mover.ClearAvoidancePath();
            return Vector2.Zero;
        }

        var primaryRisk = risks.OrderBy(r => r.distance).First();
        avoidanceThreat = primaryRisk.threat;
        float minDistance = risks.Min(r => r.distance);
        float desiredWeight = Math.Clamp(minDistance / (moverRadius + 0.001f), 1f, 3f);

        var path = BuildSegmentedAvoidancePath(mover, baseDesiredDir, primaryRisk);
        if (path.Count > 0)
        {
            mover.SetAvoidancePath(path);
            if (mover.TryGetNextAvoidanceWaypoint(out var waypoint))
            {
                avoidanceTarget = waypoint;
                isDetouring = true;
                return MathUtils.SafeNormalize(waypoint - mover.Position) * desiredWeight;
            }
            mover.ClearAvoidancePath();
        }
        else
        {
            mover.ClearAvoidancePath();
        }

        for (int i = 0; i <= Constants.MAX_AVOIDANCE_ITERATIONS; i++)
        {
            var offsets = i == 0 ? new float[] { 0f } : new float[] { Constants.AVOIDANCE_ANGLE_STEP * i, -Constants.AVOIDANCE_ANGLE_STEP * i };
            foreach (var angle in offsets)
            {
                Vector2 candidate = MathUtils.Rotate(baseDesiredDir, angle);
                if (IsDirectionClear(candidate, risks))
                {
                    avoidanceTarget = mover.Position + candidate * MathF.Max(minDistance, moverRadius * 2f);
                    isDetouring = MathF.Abs(angle) > 0.001f;
                    if (!isDetouring)
                    {
                        avoidanceTarget = Vector2.Zero;
                        avoidanceThreat = null;
                    }
                    return candidate * desiredWeight;
                }
            }
        }

        Vector2 away = MathUtils.SafeNormalize(-primaryRisk.relPos);
        avoidanceTarget = mover.Position + away * MathF.Max(primaryRisk.distance, moverRadius * 2f);
        isDetouring = true;
        return away * desiredWeight;
    }

    private static List<Vector2> BuildSegmentedAvoidancePath(Unit mover, Vector2 baseDir, (Vector2 relPos, float distance, float combinedRadius, Unit threat) primaryRisk)
    {
        var path = new List<Vector2>();
        int segmentCount = Constants.AVOIDANCE_SEGMENT_COUNT;
        if (segmentCount <= 0)
        {
            return path;
        }

        Vector2 forward = baseDir.LengthSquared() > 0.0001f ? baseDir : mover.Forward;
        if (forward.LengthSquared() < 0.0001f) forward = Vector2.UnitX;
        forward = MathUtils.SafeNormalize(forward);
        Vector2 lateral = new(-forward.Y, forward.X);
        float side = MathF.Sign(Vector2.Dot(lateral, primaryRisk.relPos));
        if (MathF.Abs(side) < 0.001f) side = 1f;
        lateral *= side;

        float startDistance = MathF.Max(Constants.AVOIDANCE_SEGMENT_START_DISTANCE, mover.Radius);
        float lateralDistance = primaryRisk.combinedRadius + Constants.AVOIDANCE_LATERAL_PADDING;
        float parallelDistance = MathF.Max(primaryRisk.distance, mover.Radius * 2f) * Constants.AVOIDANCE_PARALLEL_DISTANCE_MULTIPLIER;

        Vector2 current = mover.Position + forward * startDistance;
        path.Add(current);

        for (int segment = 0; segment < segmentCount; segment++)
        {
            int phase = segment % 3;
            switch (phase)
            {
                case 0:
                    current += lateral * lateralDistance;
                    break;
                case 1:
                    current += forward * parallelDistance;
                    break;
                default:
                    current -= lateral * lateralDistance;
                    break;
            }
            path.Add(current);
        }

        return path;
    }

    private static bool IsDirectionClear(Vector2 direction, List<(Vector2 relPos, float distance, float combinedRadius, Unit threat)> risks)
    {
        foreach (var (relPos, distance, combinedRadius, _) in risks)
        {
            float projection = Vector2.Dot(relPos, direction);
            if (projection < 0 || projection > distance) continue;

            Vector2 lateral = relPos - direction * projection;
            if (lateral.Length() < combinedRadius) return false;
        }
        return true;
    }
}
