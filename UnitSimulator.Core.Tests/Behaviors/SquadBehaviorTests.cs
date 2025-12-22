using System.Numerics;
using Xunit;

namespace UnitSimulator.Core.Tests.Behaviors;

public class SquadBehaviorTests
{
    [Fact]
    public void UpdateFriendlySquad_NoEnemies_MovesLeaderTowardMainTarget()
    {
        var behavior = new SquadBehavior();
        var leader = new Unit(new Vector2(0, 0), 20f, 4.5f, 0.08f, UnitRole.Melee, 100, 1, UnitFaction.Friendly);
        var follower = new Unit(new Vector2(0, 50), 20f, 4.5f, 0.08f, UnitRole.Ranged, 100, 2, UnitFaction.Friendly);
        var friendlies = new List<Unit> { leader, follower };
        var enemies = new List<Unit>();

        behavior.UpdateFriendlySquad(friendlies, enemies, new Vector2(300, 300));

        Assert.Equal(new Vector2(300, 300), leader.CurrentDestination);
    }
}
