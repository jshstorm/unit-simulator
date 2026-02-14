#pragma once

#include "CoreMinimal.h"
#include "PathNode.generated.h"

/**
 * A* pathfinding grid node.
 * Ported from Pathfinding/PathNode.cs
 */
USTRUCT(BlueprintType)
struct UNITSIMCORE_API FPathNode
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 X = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Y = 0;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector2D WorldPosition = FVector2D::ZeroVector;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsWalkable = true;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 GCost = TNumericLimits<int32>::Max();

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 HCost = 0;

	/** Index of the node we came from (-1 = none). Using index instead of pointer for USTRUCT compatibility. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 CameFromNodeIndex = -1;

	int32 GetFCost() const { return GCost + HCost; }

	void ResetCosts()
	{
		GCost = TNumericLimits<int32>::Max();
		HCost = 0;
		CameFromNodeIndex = -1;
	}

	FPathNode() = default;

	FPathNode(int32 InX, int32 InY, const FVector2D& InWorldPosition, bool bInIsWalkable = true)
		: X(InX)
		, Y(InY)
		, WorldPosition(InWorldPosition)
		, bIsWalkable(bInIsWalkable)
		, GCost(TNumericLimits<int32>::Max())
		, HCost(0)
		, CameFromNodeIndex(-1)
	{
	}
};
