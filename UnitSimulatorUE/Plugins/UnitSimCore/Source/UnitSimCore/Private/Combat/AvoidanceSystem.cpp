#include "Combat/AvoidanceSystem.h"
#include "Units/Unit.h"

FVector2D AvoidanceSystem::SafeNormalize(const FVector2D& V)
{
	const float LenSq = V.SizeSquared();
	if (LenSq < KINDA_SMALL_NUMBER)
	{
		return FVector2D::ZeroVector;
	}
	return V / FMath::Sqrt(LenSq);
}

FVector2D AvoidanceSystem::Rotate(const FVector2D& V, float Angle)
{
	const float Cos = FMath::Cos(Angle);
	const float Sin = FMath::Sin(Angle);
	return FVector2D(V.X * Cos - V.Y * Sin, V.X * Sin + V.Y * Cos);
}

bool AvoidanceSystem::TryGetFirstCollision(const FUnit& A, const FUnit& B, float& OutT, float& OutDistance)
{
	const float CombinedRadius = A.Radius * UnitSimConstants::COLLISION_RADIUS_SCALE
		+ B.Radius * UnitSimConstants::COLLISION_RADIUS_SCALE;

	const FVector2D RelPos = B.Position - A.Position;
	const FVector2D RelVel = B.Velocity - A.Velocity;
	const float RelSpeedSq = RelVel.SizeSquared();

	if (RelSpeedSq < KINDA_SMALL_NUMBER)
	{
		OutT = 0.f;
		OutDistance = RelPos.Size();
		return OutDistance < CombinedRadius;
	}

	const float A2 = RelSpeedSq;
	const float B2 = 2.f * FVector2D::DotProduct(RelPos, RelVel);
	const float C2 = RelPos.SizeSquared() - CombinedRadius * CombinedRadius;

	const float Discriminant = B2 * B2 - 4.f * A2 * C2;
	if (Discriminant < 0.f)
	{
		OutT = 0.f;
		OutDistance = 0.f;
		return false;
	}

	const float SqrtD = FMath::Sqrt(Discriminant);
	const float T1 = (-B2 - SqrtD) / (2.f * A2);
	const float T2 = (-B2 + SqrtD) / (2.f * A2);

	OutT = T1 >= 0.f ? T1 : T2;
	if (OutT < 0.f)
	{
		OutT = 0.f;
		OutDistance = 0.f;
		return false;
	}

	OutDistance = (RelPos + RelVel * OutT).Size();
	return true;
}

FVector2D AvoidanceSystem::PredictiveAvoidanceVector(
	FUnit& Mover,
	int32 MoverIndex,
	const FUnit* Others,
	int32 OtherCount,
	const FVector2D& DesiredDirection,
	FVector2D& OutAvoidanceTarget,
	bool& bOutIsDetouring,
	int32& OutThreatIndex)
{
	OutAvoidanceTarget = FVector2D::ZeroVector;
	OutThreatIndex = -1;
	bOutIsDetouring = false;

	const float MoverRadius = Mover.Radius * UnitSimConstants::COLLISION_RADIUS_SCALE;
	const float MinSpeed = FMath::Max(Mover.Speed, 0.001f);

	FVector2D BaseDesiredDir = DesiredDirection.SizeSquared() > 0.0001f
		? SafeNormalize(DesiredDirection)
		: (Mover.Velocity.SizeSquared() > 0.0001f ? SafeNormalize(Mover.Velocity) : Mover.Forward);

	TArray<FAvoidanceRisk> Risks;

	for (int32 i = 0; i < OtherCount; ++i)
	{
		if (i == MoverIndex) continue;
		const FUnit& Other = Others[i];
		if (Other.bIsDead) continue;
		if (!Mover.IsSameLayer(Other)) continue;

		const float CombinedRadius = MoverRadius + Other.Radius * UnitSimConstants::COLLISION_RADIUS_SCALE;
		const FVector2D RelativePos = Other.Position - Mover.Position;
		const FVector2D RelativeVel = Other.Velocity - Mover.Velocity;
		const float RelativeSpeedSq = RelativeVel.SizeSquared();

		// Check predicted collision
		float TCollision, CollisionDist;
		if (TryGetFirstCollision(Mover, Other, TCollision, CollisionDist))
		{
			const float CollisionWindow = FMath::Min((CombinedRadius * 2.f) / MinSpeed, UnitSimConstants::AVOIDANCE_MAX_LOOKAHEAD);
			if (TCollision <= CollisionWindow)
			{
				const FVector2D RelAtCollision = (Other.Position + Other.Velocity * TCollision)
					- (Mover.Position + Mover.Velocity * TCollision);
				const float DistanceAtCollision = RelAtCollision.Size();
				if (DistanceAtCollision > 0.0001f)
				{
					FAvoidanceRisk Risk;
					Risk.RelPos = RelAtCollision;
					Risk.Distance = DistanceAtCollision;
					Risk.CombinedRadius = CombinedRadius;
					Risk.ThreatIndex = i;
					Risks.Add(Risk);
					continue;
				}
			}
		}

		// Closest approach
		const float TClosest = RelativeSpeedSq < 0.0001f
			? 0.f
			: FMath::Max(-FVector2D::DotProduct(RelativePos, RelativeVel) / RelativeSpeedSq, 0.f);
		const float TimeWindow = FMath::Min((CombinedRadius * 2.f) / MinSpeed, UnitSimConstants::AVOIDANCE_MAX_LOOKAHEAD);
		const FVector2D FutureRelPos = RelativePos + RelativeVel * TClosest;
		const float FutureDistance = FutureRelPos.Size();

		if (FutureDistance < CombinedRadius && TClosest <= TimeWindow && FutureDistance > 0.0001f)
		{
			FAvoidanceRisk Risk;
			Risk.RelPos = RelativePos;
			Risk.Distance = RelativePos.Size();
			Risk.CombinedRadius = CombinedRadius;
			Risk.ThreatIndex = i;
			Risks.Add(Risk);
			continue;
		}

		// Forward cone check
		const float Projection = FVector2D::DotProduct(RelativePos, BaseDesiredDir);
		const float LookaheadDistance = Mover.Speed * UnitSimConstants::AVOIDANCE_MAX_LOOKAHEAD + CombinedRadius;
		if (Projection > 0 && Projection <= LookaheadDistance)
		{
			const FVector2D Lateral = RelativePos - BaseDesiredDir * Projection;
			if (Lateral.Size() < CombinedRadius)
			{
				FAvoidanceRisk Risk;
				Risk.RelPos = RelativePos;
				Risk.Distance = Projection;
				Risk.CombinedRadius = CombinedRadius;
				Risk.ThreatIndex = i;
				Risks.Add(Risk);
			}
		}
	}

	if (Risks.Num() == 0)
	{
		Mover.ClearAvoidancePath();
		return FVector2D::ZeroVector;
	}

	// Find primary risk (nearest)
	int32 PrimaryIdx = 0;
	float MinDist = Risks[0].Distance;
	for (int32 i = 1; i < Risks.Num(); ++i)
	{
		if (Risks[i].Distance < MinDist)
		{
			MinDist = Risks[i].Distance;
			PrimaryIdx = i;
		}
	}
	const FAvoidanceRisk& PrimaryRisk = Risks[PrimaryIdx];
	OutThreatIndex = PrimaryRisk.ThreatIndex;

	float MinDistance = MinDist;
	const float DesiredWeight = FMath::Clamp(MinDistance / (MoverRadius + 0.001f), 1.f, 3.f);

	// Try segmented avoidance path
	TArray<FVector2D> Path = BuildSegmentedAvoidancePath(Mover, BaseDesiredDir, PrimaryRisk);
	if (Path.Num() > 0)
	{
		Mover.SetAvoidancePath(Path);
		FVector2D Waypoint;
		if (Mover.TryGetNextAvoidanceWaypoint(Waypoint))
		{
			OutAvoidanceTarget = Waypoint;
			bOutIsDetouring = true;
			return SafeNormalize(Waypoint - Mover.Position) * DesiredWeight;
		}
		Mover.ClearAvoidancePath();
	}
	else
	{
		Mover.ClearAvoidancePath();
	}

	// Fallback: try rotated directions
	for (int32 i = 0; i <= UnitSimConstants::MAX_AVOIDANCE_ITERATIONS; ++i)
	{
		TArray<float, TInlineAllocator<2>> Offsets;
		if (i == 0)
		{
			Offsets.Add(0.f);
		}
		else
		{
			Offsets.Add(UnitSimConstants::AVOIDANCE_ANGLE_STEP * i);
			Offsets.Add(-UnitSimConstants::AVOIDANCE_ANGLE_STEP * i);
		}

		for (const float Angle : Offsets)
		{
			const FVector2D Candidate = Rotate(BaseDesiredDir, Angle);
			if (IsDirectionClear(Candidate, Risks))
			{
				OutAvoidanceTarget = Mover.Position + Candidate * FMath::Max(MinDistance, MoverRadius * 2.f);
				bOutIsDetouring = FMath::Abs(Angle) > 0.001f;
				if (!bOutIsDetouring)
				{
					OutAvoidanceTarget = FVector2D::ZeroVector;
					OutThreatIndex = -1;
				}
				return Candidate * DesiredWeight;
			}
		}
	}

	// Last resort: move away from primary risk
	const FVector2D Away = SafeNormalize(-PrimaryRisk.RelPos);
	OutAvoidanceTarget = Mover.Position + Away * FMath::Max(PrimaryRisk.Distance, MoverRadius * 2.f);
	bOutIsDetouring = true;
	return Away * DesiredWeight;
}

TArray<FVector2D> AvoidanceSystem::BuildSegmentedAvoidancePath(
	const FUnit& Mover,
	const FVector2D& BaseDir,
	const FAvoidanceRisk& PrimaryRisk)
{
	TArray<FVector2D> Path;
	const int32 SegmentCount = UnitSimConstants::AVOIDANCE_SEGMENT_COUNT;
	if (SegmentCount <= 0) return Path;

	FVector2D Forward = BaseDir.SizeSquared() > 0.0001f ? BaseDir : Mover.Forward;
	if (Forward.SizeSquared() < 0.0001f) Forward = FVector2D(1.0, 0.0);
	Forward = SafeNormalize(Forward);

	FVector2D Lateral(-Forward.Y, Forward.X);
	float Side = FMath::Sign(FVector2D::DotProduct(Lateral, PrimaryRisk.RelPos));
	if (FMath::Abs(Side) < 0.001f) Side = 1.f;
	Lateral *= Side;

	const float StartDistance = FMath::Max(UnitSimConstants::AVOIDANCE_SEGMENT_START_DISTANCE, Mover.Radius);
	const float LateralDistance = PrimaryRisk.CombinedRadius + UnitSimConstants::AVOIDANCE_LATERAL_PADDING;
	const float ParallelDistance = FMath::Max(PrimaryRisk.Distance, Mover.Radius * 2.f) * UnitSimConstants::AVOIDANCE_PARALLEL_DISTANCE_MULTIPLIER;

	FVector2D Current = Mover.Position + Forward * StartDistance;
	Path.Add(Current);

	for (int32 Segment = 0; Segment < SegmentCount; ++Segment)
	{
		const int32 Phase = Segment % 3;
		switch (Phase)
		{
		case 0:
			Current += Lateral * LateralDistance;
			break;
		case 1:
			Current += Forward * ParallelDistance;
			break;
		default:
			Current -= Lateral * LateralDistance;
			break;
		}
		Path.Add(Current);
	}

	return Path;
}

bool AvoidanceSystem::IsDirectionClear(
	const FVector2D& Direction,
	const TArray<FAvoidanceRisk>& Risks)
{
	for (const FAvoidanceRisk& Risk : Risks)
	{
		const float Projection = FVector2D::DotProduct(Risk.RelPos, Direction);
		if (Projection < 0 || Projection > Risk.Distance) continue;

		const FVector2D LateralVec = Risk.RelPos - Direction * Projection;
		if (LateralVec.Size() < Risk.CombinedRadius) return false;
	}
	return true;
}
