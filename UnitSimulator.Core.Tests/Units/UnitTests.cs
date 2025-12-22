using System.Numerics;
using Xunit;

namespace UnitSimulator.Core.Tests.Units;

public class UnitTests
{
    [Fact]
    public void TakeDamage_KillsUnitAtZeroHp()
    {
        var unit = new Unit(new Vector2(0, 0), 20f, 4.5f, 0.08f, UnitRole.Melee, 5, 1, UnitFaction.Friendly);

        unit.TakeDamage(5);

        Assert.Equal(0, unit.HP);
        Assert.True(unit.IsDead);
    }

    [Fact]
    public void AttackSlot_ClaimAndRelease()
    {
        var target = new Unit(new Vector2(0, 0), 20f, 4.5f, 0.08f, UnitRole.Melee, 10, 1, UnitFaction.Friendly);
        var attacker = new Unit(new Vector2(10, 0), 20f, 4.5f, 0.08f, UnitRole.Melee, 10, 2, UnitFaction.Enemy);

        var slotIndex = target.TryClaimSlot(attacker);

        Assert.Equal(0, slotIndex);
        Assert.Equal(0, attacker.TakenSlotIndex);
        Assert.Equal(attacker, target.AttackSlots[0]);

        target.ReleaseSlot(attacker);

        Assert.Equal(-1, attacker.TakenSlotIndex);
        Assert.Null(target.AttackSlots[0]);
    }
}
