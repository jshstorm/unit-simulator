using System.Numerics;
using System.Text.Json;
using UnitSimulator.Server.Messages;
using UnitSimulator.Skills;

namespace UnitSimulator.Server.Handlers;

public static class TowerSkillHandler
{
    private static readonly Dictionary<string, TowerSkillSystem> _sessionSkillSystems = new();
    private static readonly object _lock = new();

    public static TowerSkillSystem GetSkillSystem(SimulationSession session)
    {
        lock (_lock)
        {
            if (!_sessionSkillSystems.TryGetValue(session.SessionId, out var skillSystem))
            {
                skillSystem = new TowerSkillSystem();
                _sessionSkillSystems[session.SessionId] = skillSystem;
                InitializeDefaultSkills(session, skillSystem);
            }
            return skillSystem;
        }
    }

    public static void RemoveSkillSystem(string sessionId)
    {
        lock (_lock)
        {
            _sessionSkillSystems.Remove(sessionId);
        }
    }

    public static async Task HandleActivateSkillAsync(
        SessionClient client,
        SimulationSession session,
        JsonElement commandData)
    {
        if (!commandData.TryGetProperty("towerId", out var towerIdElement))
        {
            await session.SendToClientAsync(client, "error", new
            {
                message = "Missing towerId for activate_skill command",
                code = SkillErrorCodes.InvalidRequest
            });
            return;
        }

        if (!commandData.TryGetProperty("skillId", out var skillIdElement))
        {
            await session.SendToClientAsync(client, "error", new
            {
                message = "Missing skillId for activate_skill command",
                code = SkillErrorCodes.InvalidRequest
            });
            return;
        }

        var towerId = towerIdElement.GetInt32();
        var skillId = skillIdElement.GetString();

        if (string.IsNullOrEmpty(skillId))
        {
            await session.SendToClientAsync(client, "error", new
            {
                message = "Invalid skillId",
                code = SkillErrorCodes.InvalidRequest
            });
            return;
        }

        Vector2? targetPosition = null;
        if (commandData.TryGetProperty("targetPosition", out var posElement))
        {
            if (posElement.TryGetProperty("x", out var xElem) &&
                posElement.TryGetProperty("y", out var yElem))
            {
                targetPosition = new Vector2(xElem.GetSingle(), yElem.GetSingle());
            }
        }

        int? targetUnitId = null;
        if (commandData.TryGetProperty("targetUnitId", out var unitIdElement))
        {
            targetUnitId = unitIdElement.GetInt32();
        }

        var tower = session.Simulator.GameSession.GetAllTowers()
            .FirstOrDefault(t => t.Id == towerId);

        List<Unit> enemies;
        if (tower != null)
        {
            enemies = tower.Faction == UnitFaction.Friendly
                ? session.Simulator.EnemyUnits.ToList()
                : session.Simulator.FriendlyUnits.ToList();
        }
        else
        {
            enemies = new List<Unit>();
        }

        var skillSystem = GetSkillSystem(session);
        var result = skillSystem.ActivateSkill(
            towerId,
            skillId,
            tower,
            enemies,
            targetPosition,
            targetUnitId);

        var response = ActivateTowerSkillResponse.FromResult(result);

        await session.SendToClientAsync(client, "skill_response", response);

        if (result.Success)
        {
            var skillEvent = new TowerSkillActivatedEvent
            {
                TowerId = towerId,
                SkillId = skillId,
                Effects = response.Effects ?? new List<SkillEffectDto>(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.BroadcastAsync("skill_activated", skillEvent);
            await session.BroadcastAsync("frame", session.Simulator.GetCurrentFrameData());

            Console.WriteLine($"[TowerSkillHandler] Skill '{skillId}' activated on tower {towerId}. Effects: {result.Effects?.Count ?? 0}");
        }
        else
        {
            Console.WriteLine($"[TowerSkillHandler] Skill '{skillId}' activation failed: {result.ErrorMessage}");
        }
    }

    public static async Task HandleGetSkillsAsync(
        SessionClient client,
        SimulationSession session,
        JsonElement commandData)
    {
        if (!commandData.TryGetProperty("towerId", out var towerIdElement))
        {
            await session.SendToClientAsync(client, "error", new
            {
                message = "Missing towerId for get_tower_skills command",
                code = SkillErrorCodes.InvalidRequest
            });
            return;
        }

        var towerId = towerIdElement.GetInt32();
        var skillSystem = GetSkillSystem(session);
        var skills = skillSystem.GetSkills(towerId);

        var skillDtos = skills.Select(s => new TowerSkillDto
        {
            Id = s.Id,
            Name = s.Name,
            EffectType = s.EffectType.ToString(),
            CooldownMs = s.CooldownMs,
            RemainingCooldownMs = s.RemainingCooldownMs,
            IsOnCooldown = s.IsOnCooldown,
            Damage = s.Damage > 0 ? s.Damage : null,
            Range = s.Range > 0 ? s.Range : null
        }).ToList();

        var response = new GetTowerSkillsResponse
        {
            TowerId = towerId,
            Skills = skillDtos
        };

        await session.SendToClientAsync(client, "tower_skills", response);
    }

    private static void InitializeDefaultSkills(SimulationSession session, TowerSkillSystem skillSystem)
    {
        if (!session.Simulator.IsInitialized)
        {
            return;
        }

        var allTowers = session.Simulator.GameSession.GetAllTowers();

        foreach (var tower in allTowers)
        {
            if (tower.Type == TowerType.King)
            {
                skillSystem.RegisterSkill(tower.Id, new TowerSkill
                {
                    Id = "king_blast",
                    Name = "Royal Blast",
                    EffectType = SkillEffectType.AreaOfEffect,
                    TargetType = SkillTargetType.Position,
                    Damage = 200,
                    Range = 150f,
                    CooldownMs = 15000
                });
            }
            else if (tower.Type == TowerType.Princess)
            {
                skillSystem.RegisterSkill(tower.Id, new TowerSkill
                {
                    Id = "arrow_volley",
                    Name = "Arrow Volley",
                    EffectType = SkillEffectType.TargetedDamage,
                    TargetType = SkillTargetType.SingleUnit,
                    Damage = 150,
                    Range = 200f,
                    CooldownMs = 10000
                });

                skillSystem.RegisterSkill(tower.Id, new TowerSkill
                {
                    Id = "spread_shot",
                    Name = "Spread Shot",
                    EffectType = SkillEffectType.AreaOfEffect,
                    TargetType = SkillTargetType.None,
                    Damage = 75,
                    Range = 100f,
                    CooldownMs = 8000
                });
            }
        }

        Console.WriteLine($"[TowerSkillHandler] Initialized default skills for {allTowers.Count()} towers in session {session.SessionId[..8]}");
    }
}
