#pragma once

#include "CoreMinimal.h"

/**
 * Base interface for all simulation commands.
 * Commands are serializable and support deterministic replay.
 * Ported from Commands/ISimulationCommand.cs
 */
class UNITSIMCORE_API ISimulationCommand
{
public:
	virtual ~ISimulationCommand() = default;

	/**
	 * The frame number when this command should be executed.
	 * Commands are processed at the start of the specified frame.
	 */
	virtual int32 GetFrameNumber() const = 0;
};
