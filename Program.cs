

using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Fonts;

public enum UnitRole { Melee, Ranged }

public class Unit
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public Vector2 Forward { get; set; }
    public float Radius { get; }
    public float Speed { get; }
    public float TurnSpeed { get; }
    public Unit? Target { get; set; }
    public int HP { get; set; }
    public UnitRole Role { get; }
    public float AttackRange { get; }
    public float AttackCooldown { get; set; }
    public bool IsDead { get; set; }
    public List<Tuple<Unit, int>> RecentAttacks { get; } = new();
    private const int NumAttackSlots = 8;
    public Unit?[] AttackSlots { get; } = new Unit?[NumAttackSlots];
    public int TakenSlotIndex { get; set; } = -1;
    public Vector2 AvoidanceTarget { get; set; } = Vector2.Zero;
    public bool HasAvoidanceTarget { get; set; }

    public Unit(Vector2 position, float radius, float speed, float turnSpeed, UnitRole role, int hp)
    {
        Position = position;
        Radius = radius;
        Speed = speed;
        TurnSpeed = turnSpeed;
        Role = role;
        HP = hp;
        AttackRange = (role == UnitRole.Melee) ? radius * 3 : radius * 6;
        AttackCooldown = 0;
        IsDead = false;
        Velocity = Vector2.Zero;
        Forward = Vector2.UnitX;
        Target = null;
    }

    public Vector2 GetSlotPosition(int slotIndex, float attackerRadius)
    {
        float angle = (2 * MathF.PI / NumAttackSlots) * slotIndex;
        float distance = this.Radius + attackerRadius + 10f;
        return this.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }

    public int TryClaimSlot(Unit attacker)
    {
        for (int i = 0; i < NumAttackSlots; i++)
        {
            if (AttackSlots[i] == null)
            {
                AttackSlots[i] = attacker;
                attacker.TakenSlotIndex = i;
                return i;
            }
        }
        return -1;
    }

    // Assign the closest available slot to the attacker; reclaims if a better slot opens.
    public int ClaimBestSlot(Unit attacker)
    {
        int bestIndex = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < NumAttackSlots; i++)
        {
            var occupant = AttackSlots[i];
            if (occupant != null && occupant != attacker) continue;

            float distance = Vector2.Distance(attacker.Position, GetSlotPosition(i, attacker.Radius));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        if (bestIndex != -1)
        {
            if (attacker.TakenSlotIndex != -1 && attacker.TakenSlotIndex != bestIndex && attacker.TakenSlotIndex < AttackSlots.Length && AttackSlots[attacker.TakenSlotIndex] == attacker)
            {
                AttackSlots[attacker.TakenSlotIndex] = null;
            }
            AttackSlots[bestIndex] = attacker;
            attacker.TakenSlotIndex = bestIndex;
        }
        else
        {
            ReleaseSlot(attacker);
        }

        return bestIndex;
    }

    public void ReleaseSlot(Unit attacker)
    {
        if (attacker.TakenSlotIndex != -1 && attacker.TakenSlotIndex < AttackSlots.Length)
        {
            if (AttackSlots[attacker.TakenSlotIndex] == attacker)
            {
                AttackSlots[attacker.TakenSlotIndex] = null;
            }
            attacker.TakenSlotIndex = -1;
        }
    }
}

public class Program
{
    const int ImageWidth = 2000;
    const int ImageHeight = 1000;
    const int MaxFrames = 3000;
    const string OutputDirectory = "output";
    private static Font? _font;
    private static int _currentWave = 1;
    private static Dictionary<int, List<Vector2>> _waveSpawns = new();
    private const float CollisionRadiusScale = 2f / 3f;

    // --- 분대 행동 제어를 위한 상태 변수 ---
    private static Unit? _squadTarget = null;
    private static Vector2 _rallyPoint = Vector2.Zero;
    private static bool _isReadyToAttack = false;
    // ------------------------------------

    public static void Main(string[] args)
    {
        SetupEnvironment();
        var mainTarget = new Vector2(ImageWidth - 100, ImageHeight / 2);
        const float unitRadius = 20f;

        var friendlySquad = new List<Unit>
        {
            new(new Vector2(200, ImageHeight / 2 - 45), unitRadius, 4.5f, 0.08f, UnitRole.Melee, 100),
            new(new Vector2(200, ImageHeight / 2 + 45), unitRadius, 4.5f, 0.08f, UnitRole.Melee, 100),
            new(new Vector2(120, ImageHeight / 2 - 75), unitRadius, 4.5f, 0.08f, UnitRole.Ranged, 100),
            new(new Vector2(120, ImageHeight / 2 + 75), unitRadius, 4.5f, 0.08f, UnitRole.Ranged, 100)
        };
        
        var enemySquad = new List<Unit>();
        SpawnNextWave(enemySquad, unitRadius);

        Console.WriteLine("Running final polished simulation...");

        for (int i = 0; i < MaxFrames; i++)
        {
            if (!enemySquad.Any(e => !e.IsDead))
            {
                if (_currentWave < 3)
                {
                    _currentWave++;
                    Console.WriteLine($"Wave {_currentWave-1} cleared! Spawning wave {_currentWave}...");

                    // --- 상태 초기화 ---
                    _squadTarget = null;
                    _isReadyToAttack = false;
                    // -----------------

                    friendlySquad.ForEach(f => Array.Fill(f.AttackSlots, null));
                    SpawnNextWave(enemySquad, unitRadius);
                }
                else
                {
                     Console.WriteLine($"All enemy waves eliminated at frame {i}.");
                     break;
                }
            }

            UpdateEnemySquad(enemySquad, friendlySquad);
            UpdateFriendlySquad(friendlySquad, enemySquad, mainTarget);
            GenerateFrame(i, friendlySquad, enemySquad, mainTarget);
        }
        Console.WriteLine($"\nFinished generating frames in '{OutputDirectory}'.");
        Console.WriteLine($"ffmpeg -framerate 60 -i {System.IO.Path.Combine(OutputDirectory, "frame_%04d.png")} -c:v libx264 -pix_fmt yuv420p output.mp4");
    }

    private static void SpawnNextWave(List<Unit> enemySquad, float radius)
    {
        enemySquad.Clear();
        var spawns = _waveSpawns[_currentWave];
        foreach(var pos in spawns)
        {
            enemySquad.Add(new Unit(pos, radius, 4.0f, 0.1f, UnitRole.Melee, 5));
        }
    }

    private static void UpdateEnemySquad(List<Unit> enemies, List<Unit> friendlies)
    {
        var livingFriendlies = friendlies.Where(f => !f.IsDead).ToList();
        if (!livingFriendlies.Any()) return;

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead) continue;

            if (enemy.HP <= 0)
            {
                enemy.IsDead = true;
                enemy.Velocity = Vector2.Zero;
                if (enemy.Target != null) enemy.Target.ReleaseSlot(enemy);
                continue;
            }

            if (enemy.Target == null || enemy.Target.IsDead)
            {
                if (enemy.Target != null) enemy.Target.ReleaseSlot(enemy);
                enemy.Target = livingFriendlies.OrderBy(f => Vector2.Distance(enemy.Position, f.Position)).FirstOrDefault();
            }

            if (enemy.Target == null) continue;
            enemy.Target.ClaimBestSlot(enemy);
            int slotIndex = enemy.TakenSlotIndex;

            Vector2 targetPosition;
            if (slotIndex != -1)
            {
                targetPosition = enemy.Target.GetSlotPosition(slotIndex, enemy.Radius);
                if (Vector2.Distance(enemy.Position, targetPosition) < enemy.Radius)
                {
                    enemy.Velocity = Vector2.Zero;
                }
                else
                {
                    Vector2 seekVector = SafeNormalize(targetPosition - enemy.Position);
                    Vector2 enemySeparation = CalculateSeparationVector(enemy, enemies, 120f);
                    Vector2 friendlyAvoidance = PredictiveAvoidanceVector(enemy, livingFriendlies, out var avoidTarget);
                    enemy.HasAvoidanceTarget = friendlyAvoidance != Vector2.Zero;
                    enemy.AvoidanceTarget = avoidTarget;
                    enemy.Velocity = SafeNormalize(seekVector + enemySeparation + friendlyAvoidance) * enemy.Speed;
                }
            }
            else
            {
                Vector2 toTarget = enemy.Target.Position - enemy.Position;
                Vector2 perpendicular = new(-toTarget.Y, toTarget.X);
                targetPosition = enemy.Target.Position + SafeNormalize(perpendicular) * 200f;
                Vector2 seekVector = SafeNormalize(targetPosition - enemy.Position);
                Vector2 separationVector = CalculateSeparationVector(enemy, enemies, 120f);
                Vector2 friendlyAvoidance = PredictiveAvoidanceVector(enemy, livingFriendlies, out var avoidTarget);
                enemy.HasAvoidanceTarget = friendlyAvoidance != Vector2.Zero;
                enemy.AvoidanceTarget = avoidTarget;
                enemy.Velocity = SafeNormalize(seekVector + separationVector + friendlyAvoidance) * enemy.Speed;
            }
            
            enemy.Position += enemy.Velocity;
            UpdateUnitRotation(enemy);
        }
    }

    private static void UpdateFriendlySquad(List<Unit> friendlies, List<Unit> enemies, Vector2 mainTarget)
    {
        const float attackCooldown = 30;
        var livingEnemies = enemies.Where(e => !e.IsDead).ToList();

        if (livingEnemies.Any())
        {
            // --- 목표 및 집결지 설정 (최초 1회) ---
            if (_squadTarget == null)
            {
                var leader = friendlies[0];
                _squadTarget = livingEnemies.OrderBy(e => Vector2.Distance(leader.Position, e.Position)).FirstOrDefault();
                if (_squadTarget != null)
                {
                    // 목표로부터 300 유닛 떨어진 곳을 집결지로 설정
                    Vector2 directionToTarget = Vector2.Normalize(_squadTarget.Position - leader.Position);
                    _rallyPoint = _squadTarget.Position - directionToTarget * 300f;
                }
            }

            // 목표가 죽으면 새로운 목표를 탐색
            if (_squadTarget != null && _squadTarget.IsDead)
            {
                _squadTarget = null; // 다음 프레임에 새 타겟 설정
                _isReadyToAttack = false; // 재집결
            }

            // --- 공격 개시 조건 확인 ---
            var squadLeader = friendlies[0];
            if (!_isReadyToAttack && Vector2.Distance(squadLeader.Position, _rallyPoint) < 20f)
            {
                _isReadyToAttack = true;
                Console.WriteLine("Squad has reached the rally point. Engaging!");
            }

            // --- 집결지로 이동 (대형 유지) ---
            var formationOffsets = new List<Vector2> { new(0, 0), new(0, 90), new(-80, -45), new(-80, 135) };

            Vector2 leaderTargetPosition = _isReadyToAttack ? squadLeader.Position : _rallyPoint; // 집결 완료 전에는 집결지로

            Vector2 toMain = leaderTargetPosition - squadLeader.Position;
            squadLeader.Velocity = toMain.Length() < 5f ? Vector2.Zero : SafeNormalize(toMain) * squadLeader.Speed;
            squadLeader.Position += squadLeader.Velocity;
            UpdateUnitRotation(squadLeader);

            for (int i = 1; i < friendlies.Count; i++)
            {
                var follower = friendlies[i];
                var rotation = Matrix3x2.CreateRotation(MathF.Atan2(squadLeader.Forward.Y, squadLeader.Forward.X));
                var rotatedOffset = Vector2.Transform(formationOffsets[i], rotation);
                var formationTarget = squadLeader.Position + rotatedOffset;

                Vector2 toFormation = formationTarget - follower.Position;
                float distanceToSlot = toFormation.Length();
                float rampedSpeed = follower.Speed * Math.Clamp(distanceToSlot / 150f, 0.1f, 1.0f);

                follower.Velocity = distanceToSlot < 3f ? Vector2.Zero : SafeNormalize(toFormation) * rampedSpeed;
                follower.Position += follower.Velocity;
                UpdateUnitRotation(follower);
            }

            // --- 개별 유닛 공격 로직 (집결 완료 시) ---
            if (_isReadyToAttack)
            {
                foreach(var friendly in friendlies)
                {
                    if (friendly.Target != null && friendly.Target.IsDead)
                    {
                        friendly.Target.ReleaseSlot(friendly);
                        friendly.Target = null;
                    }

                    friendly.AttackCooldown = Math.Max(0, friendly.AttackCooldown - 1);
                    var previousTarget = friendly.Target;
                    friendly.Target = livingEnemies.OrderBy(e => Vector2.Distance(friendly.Position, e.Position)).FirstOrDefault();
                    if (previousTarget != null && previousTarget != friendly.Target) previousTarget.ReleaseSlot(friendly);
                    if (friendly.Target != null) friendly.Target.ClaimBestSlot(friendly);
                    int slotIndex = friendly.TakenSlotIndex;

                    if (friendly.Target != null)
                    {
                        Vector2 attackPosition = slotIndex != -1
                            ? friendly.Target.GetSlotPosition(slotIndex, friendly.Radius)
                            : friendly.Target.Position;

                        float distanceToAttack = Vector2.Distance(friendly.Position, attackPosition);
                        if (distanceToAttack <= friendly.AttackRange)
                        {
                            friendly.Velocity = Vector2.Zero; // 공격 위치에 도달하면 정지
                            if (friendly.AttackCooldown <= 0)
                            {
                                friendly.Target.HP--;
                                friendly.AttackCooldown = attackCooldown;
                                friendly.RecentAttacks.Add(new Tuple<Unit, int>(friendly.Target, 5));
                            }
                        }
                        else
                        {
                            // 공격 위치로 이동
                            Vector2 seekVector = SafeNormalize(attackPosition - friendly.Position);
                            Vector2 separationVector = CalculateSeparationVector(friendly, friendlies, 80f);
                            Vector2 avoidance = PredictiveAvoidanceVector(friendly, livingEnemies.Cast<Unit>().Concat(friendlies).ToList(), out var avoidTarget);
                            friendly.HasAvoidanceTarget = avoidance != Vector2.Zero;
                            friendly.AvoidanceTarget = avoidTarget;
                            friendly.Velocity = SafeNormalize(seekVector + separationVector + avoidance) * friendly.Speed;
                        }
                    }
                    // 이동 로직은 대형 유지 파트에서 이미 처리되었으므로 여기서는 속도만 결정
                    friendly.Position += friendly.Velocity;
                    UpdateUnitRotation(friendly);
                }
            }
        }
        else
        {
            // --- 상태 초기화 ---
            _squadTarget = null;
            _isReadyToAttack = false;
            // -----------------

            var formationOffsets = new List<Vector2> { new(0, 0), new(0, 90), new(-80, -45), new(-80, 135) };
            var leader = friendlies[0];

            foreach (var f in friendlies) { if (f.Target != null) f.Target.ReleaseSlot(f); f.TakenSlotIndex = -1; f.Target = null; f.HasAvoidanceTarget = false; }

            Vector2 toMain = mainTarget - leader.Position;
            leader.Velocity = toMain.Length() < 5f ? Vector2.Zero : SafeNormalize(toMain) * leader.Speed;
            leader.Position += leader.Velocity;
            UpdateUnitRotation(leader);

            for (int i = 1; i < friendlies.Count; i++)
            {
                var follower = friendlies[i];
                var rotation = Matrix3x2.CreateRotation(MathF.Atan2(leader.Forward.Y, leader.Forward.X));
                var rotatedOffset = Vector2.Transform(formationOffsets[i], rotation);
                var formationTarget = leader.Position + rotatedOffset;

                Vector2 toFormation = formationTarget - follower.Position;
                float distanceToSlot = toFormation.Length();
                float rampedSpeed = follower.Speed * Math.Clamp(distanceToSlot / 150f, 0.1f, 1.0f);

                follower.Velocity = distanceToSlot < 3f ? Vector2.Zero : SafeNormalize(toFormation) * rampedSpeed;
                follower.Position += follower.Velocity;
                UpdateUnitRotation(follower);
            }
        }
    }
    
    private static Vector2 SafeNormalize(Vector2 vector)
    {
        return vector.LengthSquared() < 0.0001f ? Vector2.Zero : Vector2.Normalize(vector);
    }

    private static void UpdateUnitRotation(Unit unit)
    {
        if (unit.Velocity.LengthSquared() < 0.001f) return;
        float targetAngle = MathF.Atan2(unit.Velocity.Y, unit.Velocity.X);
        float currentAngle = MathF.Atan2(unit.Forward.Y, unit.Forward.X);
        float angleDiff = targetAngle - currentAngle;
        while (angleDiff > MathF.PI) angleDiff -= 2 * MathF.PI;
        while (angleDiff < -MathF.PI) angleDiff += 2 * MathF.PI;
        float rotation = Math.Clamp(angleDiff, -unit.TurnSpeed, unit.TurnSpeed);
        unit.Forward = Vector2.Transform(unit.Forward, Matrix3x2.CreateRotation(rotation));
    }

    private static Vector2 CalculateSeparationVector(Unit unit, List<Unit> squad, float radius)
    {
        Vector2 separationVector = Vector2.Zero;
        foreach(var member in squad)
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

    private static Vector2 PredictiveAvoidanceVector(Unit mover, List<Unit> others, out Vector2 avoidanceTarget)
    {
        avoidanceTarget = Vector2.Zero;
        float moverRadius = mover.Radius * CollisionRadiusScale;
        float minSpeed = MathF.Max(mover.Speed, 0.001f);

        var risks = new List<(Vector2 relPos, float distance, float combinedRadius)>();

        foreach (var other in others)
        {
            if (other == mover || other.IsDead) continue;
            float combinedRadius = moverRadius + other.Radius * CollisionRadiusScale;

            Vector2 relativePos = other.Position - mover.Position;
            Vector2 relativeVel = other.Velocity - mover.Velocity;
            float relativeSpeedSq = relativeVel.LengthSquared();

            if (TryGetFirstCollision(mover, other, out var tCollision, out var _))
            {
                float collisionWindow = (combinedRadius * 2f) / minSpeed; // how far ahead we care
                if (tCollision <= collisionWindow)
                {
                    Vector2 relAtCollision = (other.Position + other.Velocity * tCollision) - (mover.Position + mover.Velocity * tCollision);
                    float distanceAtCollision = relAtCollision.Length();
                    if (distanceAtCollision > 0.0001f)
                    {
                        risks.Add((relAtCollision, distanceAtCollision, combinedRadius));
                        continue;
                    }
                }
            }

            float tClosest = relativeSpeedSq < 0.0001f
                ? 0f
                : Math.Max(-Vector2.Dot(relativePos, relativeVel) / relativeSpeedSq, 0f);

            float timeWindow = (combinedRadius * 2f) / minSpeed; // time to travel one combined diameter

            Vector2 futureRelPos = relativePos + relativeVel * tClosest;
            float futureDistance = futureRelPos.Length();

            if (futureDistance < combinedRadius && tClosest <= timeWindow && futureDistance > 0.0001f)
            {
                risks.Add((relativePos, relativePos.Length(), combinedRadius));
                continue;
            }

            // Ray-style check along current heading (guards stationary/parallel movement cases)
            Vector2 heading = mover.Velocity.LengthSquared() > 0.0001f ? SafeNormalize(mover.Velocity) : mover.Forward;
            float projection = Vector2.Dot(relativePos, heading);
            if (projection > 0)
            {
                Vector2 lateral = relativePos - heading * projection;
                if (lateral.Length() < combinedRadius)
                {
                    risks.Add((relativePos, projection, combinedRadius));
                }
            }
        }

        if (!risks.Any()) return Vector2.Zero;

        Vector2 baseDir = mover.Velocity.LengthSquared() > 0.0001f ? SafeNormalize(mover.Velocity) : mover.Forward;
        float step = MathF.PI / 8f; // 22.5 degrees
        float minDistance = risks.Min(r => r.distance);
        float desiredWeight = Math.Clamp(minDistance / (moverRadius + 0.001f), 1f, 3f);

        // Search left/right offsets for the first clear direction
        for (int i = 0; i <= 8; i++)
        {
            var offsets = i == 0 ? new float[] { 0f } : new float[] { step * i, -step * i };
            foreach (var angle in offsets)
            {
                Vector2 candidate = Rotate(baseDir, angle);
                if (IsDirectionClear(candidate, risks))
                {
                    avoidanceTarget = mover.Position + candidate * MathF.Max(minDistance, moverRadius * 2f);
                    return candidate * desiredWeight;
                }
            }
        }

        // Fallback: push directly away from the closest risk
        var nearest = risks.OrderBy(r => r.distance).First();
        Vector2 away = SafeNormalize(-nearest.relPos);
        avoidanceTarget = mover.Position + away * MathF.Max(nearest.distance, moverRadius * 2f);
        return away * desiredWeight;
    }

    private static bool IsDirectionClear(Vector2 direction, List<(Vector2 relPos, float distance, float combinedRadius)> risks)
    {
        foreach (var (relPos, distance, combinedRadius) in risks)
        {
            float projection = Vector2.Dot(relPos, direction);
            if (projection < 0 || projection > distance) continue;

            Vector2 lateral = relPos - direction * projection;
            if (lateral.Length() < combinedRadius) return false;
        }
        return true;
    }

    private static Vector2 Rotate(Vector2 v, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }

    // Returns true if two moving circles will touch; outputs first collision time (>=0) and contact point.
    private static bool TryGetFirstCollision(Unit a, Unit b, out float timeToHit, out Vector2 collisionPoint)
    {
        timeToHit = float.PositiveInfinity;
        collisionPoint = Vector2.Zero;
        if (a == b) return false;

        Vector2 dp = b.Position - a.Position; // relative position
        Vector2 dv = b.Velocity - a.Velocity; // relative velocity
        float combinedRadius = (a.Radius + b.Radius) * CollisionRadiusScale;
        float radiusSq = combinedRadius * combinedRadius;

        float A = Vector2.Dot(dv, dv);
        float B = 2f * Vector2.Dot(dp, dv);
        float C = Vector2.Dot(dp, dp) - radiusSq;

        // Already intersecting
        if (C <= 0f)
        {
            timeToHit = 0f;
            collisionPoint = GetContactPoint(a.Position, b.Position, a.Radius, b.Radius);
            return true;
        }

        // No relative movement
        if (A < 0.000001f) return false;

        float discriminant = B * B - 4f * A * C;
        if (discriminant < 0f) return false;

        float sqrtDisc = MathF.Sqrt(discriminant);
        float t0 = (-B - sqrtDisc) / (2f * A);
        float t1 = (-B + sqrtDisc) / (2f * A);

        if (t0 < 0f) t0 = t1; // pick the first non-negative root
        if (t0 < 0f) return false;

        timeToHit = t0;
        Vector2 posA = a.Position + a.Velocity * timeToHit;
        Vector2 posB = b.Position + b.Velocity * timeToHit;
        collisionPoint = GetContactPoint(posA, posB, a.Radius, b.Radius);
        return true;
    }

    private static Vector2 GetContactPoint(Vector2 posA, Vector2 posB, float radiusA, float radiusB)
    {
        Vector2 delta = posB - posA;
        float distance = delta.Length();
        if (distance < 0.0001f) return posA; // overlapping centers

        float t = Math.Clamp(radiusA / (radiusA + radiusB), 0f, 1f);
        return posA + delta * t;
    }

    private static void GenerateFrame(int frameNumber, List<Unit> friendlies, List<Unit> enemies, Vector2 mainTarget)
    {
        using (var image = new Image<Rgba32>(ImageWidth, ImageHeight))
        {
            image.Mutate(ctx =>
            {
                ctx.Fill(Color.DarkSlateGray);
                if (_font != null)
                {
                    var textOptions = new RichTextOptions(_font) { Origin = new PointF(10, 10) };
                    ctx.DrawText(textOptions, $"Wave: {_currentWave} | Enemies Remaining: {enemies.Count(e => !e.IsDead)}", Color.White);
                }

                ctx.Fill(new SolidBrush(Color.Green.WithAlpha(0.5f)), new EllipsePolygon(mainTarget, 10f));

                foreach (var friendly in friendlies)
                {
                    ctx.Fill(new SolidBrush(Color.Cyan), new EllipsePolygon(friendly.Position, friendly.Radius));
                    ctx.DrawLine(new SolidPen(Color.White, 3f), friendly.Position, friendly.Position + friendly.Forward * (friendly.Radius + 5));

                    for(int i=0; i<friendly.AttackSlots.Length; i++)
                    {
                        var slotTaker = friendly.AttackSlots[i];
                        var slotPos = friendly.GetSlotPosition(i, 30f);
                        var color = slotTaker != null ? Color.Red.WithAlpha(0.8f) : Color.Gray.WithAlpha(0.3f);
                        float radius = slotTaker != null ? 6f : 3f;
                        ctx.Fill(color, new EllipsePolygon(slotPos, radius));
                    }

                    for (int i = friendly.RecentAttacks.Count - 1; i >= 0; i--)
                    {
                        var (target, timer) = friendly.RecentAttacks[i];
                        if (timer > 0)
                        {
                            ctx.DrawLine(new SolidPen(Color.White, 2f), friendly.Position, target.Position);
                            friendly.RecentAttacks[i] = new Tuple<Unit, int>(target, timer - 1);
                        }
                        else friendly.RecentAttacks.RemoveAt(i);
                    }
                }

                foreach (var enemy in enemies)
                {
                    var color = enemy.IsDead ? Color.Gray : Color.Red;
                    ctx.Fill(new SolidBrush(color), new EllipsePolygon(enemy.Position, enemy.Radius));
                    if (!enemy.IsDead)
                        ctx.DrawLine(new SolidPen(Color.OrangeRed, 2f), enemy.Position, enemy.Position + enemy.Forward * (enemy.Radius + 5));
                    if (!enemy.IsDead && enemy.HasAvoidanceTarget)
                    {
                        ctx.Draw(new SolidPen(Color.Gold, 2f), new EllipsePolygon(enemy.AvoidanceTarget, 6f));
                        ctx.DrawLine(new SolidPen(Color.Gold, 1.5f), enemy.Position, enemy.AvoidanceTarget);
                    }
                }
                foreach (var friendly in friendlies)
                {
                    if (!friendly.IsDead && friendly.HasAvoidanceTarget)
                    {
                        ctx.Draw(new SolidPen(Color.YellowGreen, 2f), new EllipsePolygon(friendly.AvoidanceTarget, 6f));
                        ctx.DrawLine(new SolidPen(Color.YellowGreen, 1.5f), friendly.Position, friendly.AvoidanceTarget);
                    }
                }
            });

            string filePath = System.IO.Path.Combine(OutputDirectory, $"frame_{frameNumber:D4}.png");
            image.Save(filePath);
        }
    }

    private static void SetupEnvironment()
    {
        var dirInfo = new DirectoryInfo(OutputDirectory);
        if (dirInfo.Exists) dirInfo.Delete(true);
        dirInfo.Create();
        
        _waveSpawns[1] = new List<Vector2>
        {
            new(ImageWidth * 0.6f, ImageHeight * 0.5f - 60), new(ImageWidth * 0.6f, ImageHeight * 0.5f + 60),
            new(ImageWidth * 0.65f, ImageHeight * 0.5f - 120), new(ImageWidth * 0.65f, ImageHeight * 0.5f + 120),
            new(ImageWidth * 0.55f, ImageHeight * 0.5f - 180), new(ImageWidth * 0.55f, ImageHeight * 0.5f + 180),
        };
        _waveSpawns[2] = new List<Vector2>
        {
            new(ImageWidth * 0.7f, 150), new(ImageWidth * 0.7f, ImageHeight - 150),
            new(ImageWidth * 0.75f, 250), new(ImageWidth * 0.75f, ImageHeight - 250),
            new(ImageWidth * 0.8f, ImageHeight/2 - 100), new(ImageWidth * 0.8f, ImageHeight/2 + 100),
            new(ImageWidth * 0.85f, ImageHeight/2 - 220), new(ImageWidth * 0.85f, ImageHeight/2 + 220),
        };
        _waveSpawns[3] = new List<Vector2>
        {
            new(ImageWidth - 250, ImageHeight/2 - 180), new(ImageWidth - 250, ImageHeight/2 + 180),
            new(ImageWidth - 350, ImageHeight/2 - 90), new(ImageWidth - 350, ImageHeight/2 + 90),
            new(ImageWidth - 450, ImageHeight/2 - 180), new(ImageWidth - 450, ImageHeight/2 + 180),
            new(ImageWidth - 550, ImageHeight/2 - 260), new(ImageWidth - 550, ImageHeight/2 + 260),
        };

        var fontCollection = new FontCollection();
        try { _font = fontCollection.Add("Arial").CreateFont(16, FontStyle.Regular); }
        catch {
            try { if (SystemFonts.TryGet("Verdana", out var f)) _font = f.CreateFont(16); }
            catch {
                try { _font = SystemFonts.Collection.Families.First().CreateFont(16); }
                catch (Exception ex) { Console.WriteLine($"Warning: Could not find system font. {ex.Message}"); }
            }
        }
    }
}
