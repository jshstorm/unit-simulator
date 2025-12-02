using System;
using System.Numerics;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Fonts;

namespace UnitSimulator;

public class WaveManager
{
    private int _currentWave = 1;
    private readonly Dictionary<int, List<Vector2>> _waveSpawns = new();
    private readonly Func<int> _enemyIdProvider;

    public int CurrentWave => _currentWave;
    public bool HasMoreWaves => _currentWave < Constants.MAX_WAVES;

    public WaveManager(Func<int> enemyIdProvider)
    {
        _enemyIdProvider = enemyIdProvider;
        InitializeWaveSpawns();
    }

    public void SpawnNextWave(List<Unit> enemySquad)
    {
        enemySquad.Clear();
        if (_waveSpawns.TryGetValue(_currentWave, out var spawns))
        {
            foreach (var pos in spawns)
            {
                enemySquad.Add(new Unit(pos, Constants.UNIT_RADIUS, 4.0f, 0.1f, UnitRole.Melee, Constants.ENEMY_HP, _enemyIdProvider(), UnitFaction.Enemy));
            }
        }
    }

    public bool TryAdvanceWave()
    {
        if (_currentWave < Constants.MAX_WAVES)
        {
            _currentWave++;
            Console.WriteLine($"Wave {_currentWave-1} cleared! Spawning wave {_currentWave}...");
            return true;
        }
        return false;
    }

    private void InitializeWaveSpawns()
    {
        _waveSpawns[1] = new List<Vector2>
        {
            new(Constants.IMAGE_WIDTH * 0.6f, Constants.IMAGE_HEIGHT * 0.5f - 60),
            new(Constants.IMAGE_WIDTH * 0.6f, Constants.IMAGE_HEIGHT * 0.5f + 60),
            new(Constants.IMAGE_WIDTH * 0.65f, Constants.IMAGE_HEIGHT * 0.5f - 120),
            new(Constants.IMAGE_WIDTH * 0.65f, Constants.IMAGE_HEIGHT * 0.5f + 120),
            new(Constants.IMAGE_WIDTH * 0.55f, Constants.IMAGE_HEIGHT * 0.5f - 180),
            new(Constants.IMAGE_WIDTH * 0.55f, Constants.IMAGE_HEIGHT * 0.5f + 180),
        };
        
        _waveSpawns[2] = new List<Vector2>
        {
            new(Constants.IMAGE_WIDTH * 0.7f, 150),
            new(Constants.IMAGE_WIDTH * 0.7f, Constants.IMAGE_HEIGHT - 150),
            new(Constants.IMAGE_WIDTH * 0.75f, 250),
            new(Constants.IMAGE_WIDTH * 0.75f, Constants.IMAGE_HEIGHT - 250),
            new(Constants.IMAGE_WIDTH * 0.8f, Constants.IMAGE_HEIGHT/2 - 100),
            new(Constants.IMAGE_WIDTH * 0.8f, Constants.IMAGE_HEIGHT/2 + 100),
            new(Constants.IMAGE_WIDTH * 0.85f, Constants.IMAGE_HEIGHT/2 - 220),
            new(Constants.IMAGE_WIDTH * 0.85f, Constants.IMAGE_HEIGHT/2 + 220),
        };
        
        _waveSpawns[3] = new List<Vector2>
        {
            new(Constants.IMAGE_WIDTH - 250, Constants.IMAGE_HEIGHT/2 - 180),
            new(Constants.IMAGE_WIDTH - 250, Constants.IMAGE_HEIGHT/2 + 180),
            new(Constants.IMAGE_WIDTH - 350, Constants.IMAGE_HEIGHT/2 - 90),
            new(Constants.IMAGE_WIDTH - 350, Constants.IMAGE_HEIGHT/2 + 90),
            new(Constants.IMAGE_WIDTH - 450, Constants.IMAGE_HEIGHT/2 - 180),
            new(Constants.IMAGE_WIDTH - 450, Constants.IMAGE_HEIGHT/2 + 180),
            new(Constants.IMAGE_WIDTH - 550, Constants.IMAGE_HEIGHT/2 - 260),
            new(Constants.IMAGE_WIDTH - 550, Constants.IMAGE_HEIGHT/2 + 260),
        };
    }
}

public class Renderer
{
    private Font? _font;
    private Font? _labelFont;
    private static readonly JsonSerializerOptions DebugJsonOptions = new() { WriteIndented = true };

    public Renderer()
    {
        InitializeFont();
    }

    public void GenerateFrame(int frameNumber, List<Unit> friendlies, List<Unit> enemies, Vector2 mainTarget, WaveManager waveManager)
    {
        try
        {
            using (var image = new Image<Rgba32>(Constants.IMAGE_WIDTH, Constants.IMAGE_HEIGHT))
            {
                image.Mutate(ctx =>
                {
                    ctx.Fill(Color.DarkSlateGray);
                    DrawUI(ctx, frameNumber, waveManager, enemies);
                    DrawMainTarget(ctx, mainTarget);
                    DrawUnits(ctx, friendlies, enemies);
                });

                string filePath = System.IO.Path.Combine(Constants.OUTPUT_DIRECTORY, $"frame_{frameNumber:D4}.png");
                image.Save(filePath);
            }

            WriteFrameDebugInfo(frameNumber, friendlies, enemies, waveManager);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating frame {frameNumber}: {ex.Message}");
        }
    }

    private void DrawUI(IImageProcessingContext ctx, int frameNumber, WaveManager waveManager, List<Unit> enemies)
    {
        if (_font != null)
        {
            var textOptions = new RichTextOptions(_font) { Origin = new PointF(10, 10) };
            ctx.DrawText(textOptions, $"Frame: {frameNumber:D4} | Wave: {waveManager.CurrentWave} | Enemies Remaining: {enemies.Count(e => !e.IsDead)}", Color.White);
        }
    }

    private void WriteFrameDebugInfo(int frameNumber, List<Unit> friendlies, List<Unit> enemies, WaveManager waveManager)
    {
        try
        {
            var info = new FrameDebugInfo(
                frameNumber,
                waveManager.CurrentWave,
                friendlies.Count(f => !f.IsDead),
                enemies.Count(e => !e.IsDead),
                friendlies.Select(CreateUnitDebug).ToList(),
                enemies.Select(CreateUnitDebug).ToList());

            var debugDir = System.IO.Path.Combine(Constants.OUTPUT_DIRECTORY, Constants.DEBUG_SUBDIRECTORY);
            System.IO.Directory.CreateDirectory(debugDir);
            var debugPath = System.IO.Path.Combine(debugDir, $"frame_{frameNumber:D4}.json");
            var payload = JsonSerializer.Serialize(info, DebugJsonOptions);
            System.IO.File.WriteAllText(debugPath, payload);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write debug info for frame {frameNumber}: {ex.Message}");
        }
    }

    private static UnitDebugInfo CreateUnitDebug(Unit unit)
    {
        var targetLabel = unit.Target is { IsDead: false } target ? target.Label : null;
        bool isMoving = unit.Velocity.LengthSquared() > 0.01f;
        bool inRange = unit.Target != null && !unit.Target.IsDead && Vector2.Distance(unit.Position, unit.Target.Position) <= unit.AttackRange;
        Vec2? avoidance = unit.HasAvoidanceTarget && unit.AvoidanceTarget != Vector2.Zero
            ? new Vec2(unit.AvoidanceTarget.X, unit.AvoidanceTarget.Y)
            : null;

        return new UnitDebugInfo(
            unit.Label,
            unit.Id,
            unit.Role.ToString(),
            unit.Faction.ToString(),
            unit.IsDead,
            unit.HP,
            unit.AttackCooldown,
            unit.TakenSlotIndex,
            targetLabel,
            isMoving,
            inRange,
            new Vec2(unit.Position.X, unit.Position.Y),
            new Vec2(unit.CurrentDestination.X, unit.CurrentDestination.Y),
            avoidance);
    }

    private record FrameDebugInfo(int Frame, int Wave, int LivingFriendlies, int LivingEnemies, List<UnitDebugInfo> Friendlies, List<UnitDebugInfo> Enemies);

    private record UnitDebugInfo(
        string Label,
        int Id,
        string Role,
        string Faction,
        bool Dead,
        int HP,
        float AttackCooldown,
        int SlotIndex,
        string? TargetLabel,
        bool IsMoving,
        bool InAttackRange,
        Vec2 Position,
        Vec2 Destination,
        Vec2? AvoidanceTarget);

    private readonly record struct Vec2(float X, float Y);

    private void DrawMainTarget(IImageProcessingContext ctx, Vector2 mainTarget)
    {
        ctx.Fill(new SolidBrush(Color.Green.WithAlpha(0.5f)), new EllipsePolygon(mainTarget, 10f));
    }

    private void DrawUnits(IImageProcessingContext ctx, List<Unit> friendlies, List<Unit> enemies)
    {
        var labelQueue = new List<(Unit unit, Color color)>();
        DrawFriendlyUnits(ctx, friendlies, labelQueue);
        DrawEnemyUnits(ctx, enemies, labelQueue);
        DrawUnitLabels(ctx, labelQueue);
    }

    private void DrawFriendlyUnits(IImageProcessingContext ctx, List<Unit> friendlies, List<(Unit unit, Color color)> labelQueue)
    {
        foreach (var friendly in friendlies)
        {
            ctx.Fill(new SolidBrush(Color.Cyan), new EllipsePolygon(friendly.Position, friendly.Radius));
            ctx.DrawLine(new SolidPen(Color.White, 3f), friendly.Position, friendly.Position + friendly.Forward * (friendly.Radius + 5));

            DrawMovementDebug(ctx, friendly);
            DrawAttackSlots(ctx, friendly);
            DrawRecentAttacks(ctx, friendly);
            labelQueue.Add((friendly, Color.White));
        }
    }

    private void DrawAttackSlots(IImageProcessingContext ctx, Unit unit)
    {
        for (int i = 0; i < unit.AttackSlots.Length; i++)
        {
            var slotTaker = unit.AttackSlots[i];
            var slotPos = unit.GetSlotPosition(i, 30f);
            var color = slotTaker != null ? Color.Red.WithAlpha(0.8f) : Color.Gray.WithAlpha(0.3f);
            float radius = slotTaker != null ? 6f : 3f;
            ctx.Fill(color, new EllipsePolygon(slotPos, radius));
        }
    }

    private void DrawRecentAttacks(IImageProcessingContext ctx, Unit unit)
    {
        for (int i = unit.RecentAttacks.Count - 1; i >= 0; i--)
        {
            var (target, timer) = unit.RecentAttacks[i];
            if (timer > 0)
            {
                ctx.DrawLine(new SolidPen(Color.White, 2f), unit.Position, target.Position);
                unit.RecentAttacks[i] = new Tuple<Unit, int>(target, timer - 1);
            }
            else unit.RecentAttacks.RemoveAt(i);
        }
    }

    private void DrawEnemyUnits(IImageProcessingContext ctx, List<Unit> enemies, List<(Unit unit, Color color)> labelQueue)
    {
        foreach (var enemy in enemies)
        {
            var color = enemy.IsDead ? Color.Gray : Color.Red;
            ctx.Fill(new SolidBrush(color), new EllipsePolygon(enemy.Position, enemy.Radius));
            if (!enemy.IsDead)
            {
                ctx.DrawLine(new SolidPen(Color.OrangeRed, 2f), enemy.Position, enemy.Position + enemy.Forward * (enemy.Radius + 5));
            }

            DrawMovementDebug(ctx, enemy);
            labelQueue.Add((enemy, Color.White));
        }
    }

    private void DrawMovementDebug(IImageProcessingContext ctx, Unit unit)
    {
        if (Vector2.Distance(unit.Position, unit.CurrentDestination) > 1f)
        {
            var pathColor = unit.Faction == UnitFaction.Friendly ? Color.LightSkyBlue : Color.LightSalmon;
            ctx.DrawLine(new SolidPen(pathColor, 1.5f), unit.Position, unit.CurrentDestination);
        }

        if (unit.HasAvoidanceTarget && unit.AvoidanceTarget != Vector2.Zero)
        {
            ctx.Draw(new SolidPen(Color.Gold, 2f), new EllipsePolygon(unit.AvoidanceTarget, 6f));
            ctx.DrawLine(new SolidPen(Color.Gold, 1.5f), unit.Position, unit.AvoidanceTarget);

            if (unit.AvoidanceThreat != null)
            {
                ctx.DrawLine(new SolidPen(Color.Goldenrod, 1f), unit.Position, unit.AvoidanceThreat.Position);
                if (_labelFont != null)
                {
                    var info = $"Avoid {unit.AvoidanceThreat.Label}";
                    var textOptions = new RichTextOptions(_labelFont)
                    {
                        Origin = new PointF(unit.AvoidanceTarget.X + 6, unit.AvoidanceTarget.Y + 6)
                    };
                    ctx.DrawText(textOptions, info, Color.Gold);
                }
            }
        }
    }

    private void DrawUnitLabels(IImageProcessingContext ctx, List<(Unit unit, Color color)> labelQueue)
    {
        foreach (var (unit, color) in labelQueue)
        {
            DrawUnitLabel(ctx, unit, color);
        }
    }

    private void DrawUnitLabel(IImageProcessingContext ctx, Unit unit, Color color)
    {
        if (_labelFont == null) return;

        var textOptions = new RichTextOptions(_labelFont)
        {
            Origin = new PointF(unit.Position.X, unit.Position.Y - unit.Radius - 18),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        ctx.DrawText(textOptions, unit.Label, color);
    }

    private void InitializeFont()
    {
        var fontCollection = new FontCollection();
        try
        {
            _font = fontCollection.Add("Arial").CreateFont(16, FontStyle.Regular);
        }
        catch
        {
            try
            {
                if (SystemFonts.TryGet("Verdana", out var f))
                    _font = f.CreateFont(16);
            }
            catch
            {
                try
                {
                    _font = SystemFonts.Collection.Families.First().CreateFont(16);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not find system font. {ex.Message}");
                }
            }
        }

        _labelFont = _font;
    }
}
