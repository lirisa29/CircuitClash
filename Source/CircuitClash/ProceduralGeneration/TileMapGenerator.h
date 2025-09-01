#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "TileMapGenerator.generated.h"

// Forward
class UStaticMesh;

// Tile Category for the TileSet data asset
UENUM(BlueprintType)
enum class ETileCategory : uint8
{
	Empty,
	Trace,
	Socket,
	Switch,
	InputPort,
	CPU
};

USTRUCT(BlueprintType)
struct FTileVariant
{
	GENERATED_BODY()

	// 4-bit mask: bit0 = North, bit1 = East, bit2 = South, bit3 = West
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Tile")
	uint8 ConnectMask = 0;

	// Weight for WFC selection
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Tile")
	float Weight = 1.f;

	// Visual mesh (optional)
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Tile")
	UStaticMesh* Mesh = nullptr;

	// If the tile should spawn a special actor (socket, switch, input, cpu) - optional
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Tile")
	TSubclassOf<AActor> SpawnActorClass = nullptr;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Tile")
	ETileCategory Category = ETileCategory::Trace;

	// Allow rotation (0..3)
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Tile")
	bool bAllowRotation = true;
};

// DataAsset to hold tile variants
UCLASS(BlueprintType)
class UTileSetDataAsset : public UDataAsset
{
	GENERATED_BODY()

public:
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="Tiles")
	TArray<FTileVariant> Variants;

	// Number of switches/sockets can be tuned here
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="Tiles")
	int32 MaxSwitches = 6;

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="Tiles")
	int32 MinSockets = 8;
};

// Generates a grid using a simplified Wave Function Collapse approach,
// seeds CPU + Input Ports, ensures at least 3 paths, spawns tile meshes and builds splines
UCLASS()
class CIRCUITCLASH_API ATileMapGenerator : public AActor
{
	GENERATED_BODY()
	
public:	
	ATileMapGenerator();

protected:
	virtual void BeginPlay() override;

public:	
	// === Editable params ===
	UPROPERTY(EditAnywhere, Category="Generation")
	int32 GridW = 24;

	UPROPERTY(EditAnywhere, Category="Generation")
	int32 GridH = 18;
	
	// World size per cell
	UPROPERTY(EditAnywhere, Category="Generation")
	float CellSize = 300.f;

	// Pointer to TileSet Data Asset
	UPROPERTY(EditAnywhere, Category="Generation")
	UTileSetDataAsset* TileSet = nullptr;

	// Whether to auto-retry generation until valid (useful when WFC fails)
	UPROPERTY(EditAnywhere, Category="Generation")
	int32 MaxGenerationRetries = 12;

	// Base tile actor to spawn for debug / placeholder
	UPROPERTY(EditAnywhere, Category="Spawning")
	TSubclassOf<AActor> DefaultTileActorClass;

	// Instanced mesh for performance
	UPROPERTY(EditAnywhere, Category="Spawning")
	UStaticMesh* DefaultTileMesh;

	// Spline Path actor class to spawn when building splines
	UPROPERTY(EditAnywhere, Category="Spawning")
	TSubclassOf<AActor> PathSplineClass;

	// Z-offset for spawning meshes so they sit above the plane
	UPROPERTY(EditAnywhere, Category="Spawning")
	float SpawnZ = 0.f;

	// === Runtime data ===

	// Struct representing a WFC option (variant + rotation)
	struct FWfcOption
	{
		int32 VariantIndex = -1;
		uint8 RotSteps = 0;
		uint8 RotatedMask = 0;
		float Weight = 1.f;
	};

	struct FWfcCell
	{
		TArray<FWfcOption> Options;
		bool bCollapsed = false;
		FWfcOption Final;
	};

protected:
	// Grid of cells
	TArray<FWfcCell> Grid;

	// Helper
	FORCEINLINE int32 Idx(int32 X, int32 Y) const { return Y * GridW + X; }
	FORCEINLINE bool InBounds(int32 X, int32 Y) const { return X >= 0 && X < GridW && Y >= 0 && Y < GridH; }

	// Core steps
	void InitGrid();
	bool RunWfcWithRetries(int32& OutSeed);
	bool RunWfcOnce(FRandomStream& Rng, bool& bOutFailedConstraint);
	bool CollapseStep(FRandomStream& Rng, bool& bOutFailedConstraint);
	void Propagate(TArray<int32>& Queue);
	bool AreNeighboursCompatible(uint8 A, int SideA, uint8 B);

	// Post-processing
	bool ExtractPathsAndValidate(TArray<TArray<FIntPoint>>& OutPaths);

	// World building
	void SpawnTilesAndSpecials();
	void SpawnTileMeshAt(int32 X, int32 Y, const FWfcOption& Opt);
	void BuildSplinesFromPaths(const TArray<TArray<FIntPoint>>& Paths);

	// Utility
	static uint8 RotateMask(uint8 Mask, uint8 RotSteps);

	// Stored valid paths
	TArray<TArray<FIntPoint>> ValidPaths;
};