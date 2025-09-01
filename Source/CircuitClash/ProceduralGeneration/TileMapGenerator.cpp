#include "CircuitClash/ProceduralGeneration/TileMapGenerator.h"
#include "Engine/World.h"
#include "Engine/StaticMesh.h"
#include "Components/InstancedStaticMeshComponent.h"
#include "UObject/ConstructorHelpers.h"
#include "Kismet/GameplayStatics.h"
#include "GameFramework/Actor.h"
#include "Math/UnrealMathUtility.h"
#include "Engine/Engine.h"

class APathSpline;

ATileMapGenerator::ATileMapGenerator()
{
	PrimaryActorTick.bCanEverTick = false;

	// Default values
	GridW = 24;
	GridH = 18;
	CellSize = 300.f;
	MaxGenerationRetries = 12;
	SpawnZ = 0.f;
}

void ATileMapGenerator::BeginPlay()
{
	Super::BeginPlay();

	if (!TileSet)
	{
		UE_LOG(LogTemp, Error, TEXT("TileMapGenerator: No TileSet assigned! Create a TileSetDataAsset and assign it."));
		return;
	}

	int32 Seed = 0;
	bool bSuccess = RunWfcWithRetries(Seed);
	if (!bSuccess)
	{
		UE_LOG(LogTemp, Error, TEXT("TileMapGenerator: Generation failed after retries."));
		return;
	}

	// After successful WFC, ValidPaths should be filled
	if (ValidPaths.Num() == 0)
	{
		UE_LOG(LogTemp, Warning, TEXT("TileMapGenerator: No valid paths found after generation."));
	}
	else
	{
		SpawnTilesAndSpecials();
		BuildSplinesFromPaths(ValidPaths);
		UE_LOG(LogTemp, Log, TEXT("TileMapGenerator: Generation succeeded with %d paths."), ValidPaths.Num());
	}
}

// Utility: rotate 4-bit mask
uint8 ATileMapGenerator::RotateMask(uint8 Mask, uint8 RotSteps)
{
	RotSteps &= 3;
	if (RotSteps == 0) return Mask;
	for (uint8 r = 0; r < RotSteps; r++)
	{
		uint8 N = (Mask >> 0) & 1;
		uint8 E = (Mask >> 1) & 1;
		uint8 S = (Mask >> 2) & 1;
		uint8 W = (Mask >> 3) & 1;

		// Clockwise: W->N, N->E, E->S, S->W
		Mask = (W << 0) | (N << 1) | (E << 2) | (S << 3);
	}
	return Mask;
}

// Initialise Grid Options
void ATileMapGenerator::InitGrid()
{
	Grid.Empty();
	Grid.SetNum(GridW * GridH);

	// Precompute all options (variants x rotations)
	TArray<FWfcOption> AllOptions;
	for (int32 i = 0; i < TileSet->Variants.Num(); i++)
	{
		const FTileVariant& V = TileSet->Variants[i];
		if (V.bAllowRotation)
		{
			for (uint8 r = 0; r < 4; r++)
			{
				FWfcOption O;
				O.VariantIndex = i;
				O.RotSteps = r;
				O.RotatedMask = RotateMask(V.ConnectMask, r);
				O.Weight = FMath::Max(0.0001f, V.Weight);
				AllOptions.Add(O);
			}
		}
		else
		{
			FWfcOption O;
			O.VariantIndex = i;
			O.RotSteps = 0;
			O.RotatedMask = V.ConnectMask;
			O.Weight = FMath::Max(0.0001f, V.Weight);
			AllOptions.Add(O);
		}
	}

	for (int32 i = 0; i < Grid.Num(); i++)
	{
		Grid[i].Options = AllOptions;
		Grid[i].bCollapsed = false;
	}

	// Seed CPU near center (collapsed)
	const int32 CX = GridW / 2;
	const int32 CY = GridH / 2;

	// Find first CPU variant
	int32 CpuVarIndex = -1;
	for (int32 i = 0; i < TileSet->Variants.Num(); i++)
	{
		if (TileSet->Variants[i].Category == ETileCategory::CPU)
		{
			CpuVarIndex = i;
			break;
		}
	}
	if (CpuVarIndex != -1)
	{
		// Collapse center cell to a CPU variant (no rotation)
		FWfcOption CPUOpt;
		CPUOpt.VariantIndex = CpuVarIndex;
		CPUOpt.RotSteps = 0;
		CPUOpt.RotatedMask = TileSet->Variants[CpuVarIndex].ConnectMask;
		CPUOpt.Weight = TileSet->Variants[CpuVarIndex].Weight;
		int32 Id = Idx(CX, CY);
		Grid[Id].Options = { CPUOpt };
		Grid[Id].Final = CPUOpt;
		Grid[Id].bCollapsed = true;
	}

	// Seed 3 input ports on edges, pointing inward
	TArray<FIntPoint> CandidatePorts = { {0, CY}, {GridW - 1, CY}, {CX, GridH - 1} };
	int32 PortFound = -1;
	int32 InputVarIndex = -1;
	for (int32 i = 0; i < TileSet->Variants.Num(); i++)
	{
		if (TileSet->Variants[i].Category == ETileCategory::InputPort)
		{
			InputVarIndex = i;
			break;
		}
	}
	if (InputVarIndex != -1)
	{
		for (int32 n = 0; n < 3 && CandidatePorts.Num(); n++)
		{
			FIntPoint P = CandidatePorts[n];
			FWfcOption PortOpt;
			PortOpt.VariantIndex = InputVarIndex;
			PortOpt.Weight = TileSet->Variants[InputVarIndex].Weight;
			PortOpt.RotSteps = 0;
			PortOpt.RotatedMask = TileSet->Variants[InputVarIndex].ConnectMask;

			// Rotate to face inward
			if (P.X == 0) PortOpt.RotatedMask = RotateMask(PortOpt.RotatedMask, 1); // Face east
			else if (P.X == GridW - 1) PortOpt.RotatedMask = RotateMask(PortOpt.RotatedMask, 3); // Face west
			else if (P.Y == 0) PortOpt.RotatedMask = RotateMask(PortOpt.RotatedMask, 2); // Face south
			// Bottom case default faces north

			int32 Id = Idx(P.X, P.Y);
			Grid[Id].Options = { PortOpt };
			Grid[Id].Final = PortOpt;
			Grid[Id].bCollapsed = true;
		}
	}

	// RunWfcOnce handles propagation on collapse steps
}

// WFC run + retries
bool ATileMapGenerator::RunWfcWithRetries(int32& OutSeed)
{
	for (int32 Attempt = 0; Attempt < MaxGenerationRetries; Attempt++)
	{
		InitGrid();
		FRandomStream Rng(FDateTime::Now().GetTicks() + Attempt * 1337);
		bool bFailedConstraint = false;
		bool bOk = RunWfcOnce(Rng, bFailedConstraint);
		if (bOk && !bFailedConstraint)
		{
			// Validate paths
			if (ExtractPathsAndValidate(ValidPaths))
			{
				OutSeed = Rng.GetInitialSeed();
				return true;
			}
			else
			{
				UE_LOG(LogTemp, Verbose, TEXT("WFC: Valid path count < required, retrying..."));
			}
		}
		else
		{
			UE_LOG(LogTemp, Verbose, TEXT("WFC: Constraint failure or collapse failure, retrying..."));
		}
	}
	return false;
}

// Run WFC once (collapse loop)
bool ATileMapGenerator::RunWfcOnce(FRandomStream& Rng, bool& bOutFailedConstraint)
{
	bOutFailedConstraint = false;

	// Quick initial propagation from any pre-collapsed cells
	TArray<int32> StartQueue;
	for (int32 y = 0; y < GridH; y++)
	{
		for (int32 x = 0; x < GridW; x++)
		{
			int32 id = Idx(x, y);
			if (Grid[id].bCollapsed)
				StartQueue.Add(id);
		}
	}
	if (StartQueue.Num())
		Propagate(StartQueue);

	// Collapse loop
	while (true)
	{
		bool bDid = CollapseStep(Rng, bOutFailedConstraint);
		if (bOutFailedConstraint) return false;
		if (!bDid) break; // finished
	}

	// Check all collapsed
	for (const FWfcCell& C : Grid)
	{
		if (!C.bCollapsed) return false;
	}
	return true;
}

// Collapse a single cell (lowest entropy)
bool ATileMapGenerator::CollapseStep(FRandomStream& Rng, bool& bOutFailedConstraint)
{
	bOutFailedConstraint = false;

	int32 BestIdx = -1;
	int32 BestCount = TNumericLimits<int32>::Max();

	for (int32 i = 0; i < Grid.Num(); i++)
	{
		if (Grid[i].bCollapsed) continue;
		int32 Count = Grid[i].Options.Num();
		if (Count == 0)
		{
			bOutFailedConstraint = true;
			return false;
		}
		if (Count < BestCount)
		{
			BestCount = Count;
			BestIdx = i;
		}
	}

	// If no uncollapsed cells remain, done
	if (BestIdx == -1) return false;

	// Weighted random pick from Options
	float Sum = 0.f;
	for (const FWfcOption& O : Grid[BestIdx].Options) Sum += O.Weight;
	float Pick = Rng.FRandRange(0.f, Sum);
	float Acc = 0.f;
	FWfcOption Chosen = Grid[BestIdx].Options[0];
	for (const FWfcOption& O : Grid[BestIdx].Options)
	{
		Acc += O.Weight;
		if (Pick <= Acc)
		{
			Chosen = O;
			break;
		}
	}

	Grid[BestIdx].bCollapsed = true;
	Grid[BestIdx].Final = Chosen;
	Grid[BestIdx].Options = { Chosen };

	// Propagate constraints from this cell
	TArray<int32> Q;
	Q.Add(BestIdx);
	Propagate(Q);

	// Check for contradictions in propagation (any cell with 0 options)
	for (const FWfcCell& C : Grid)
	{
		if (!C.bCollapsed && C.Options.Num() == 0)
		{
			bOutFailedConstraint = true;
			return false;
		}
	}

	return true;
}

// Propagate constraints (BFS-like)
void ATileMapGenerator::Propagate(TArray<int32>& Queue)
{
	// Directions: N(0,-1), E(1,0), S(0,1), W(-1,0)
	const int DX[4] = { 0, 1, 0, -1 };
	const int DY[4] = { -1, 0, 1, 0 };

	TQueue<int32> Q;
	for (int32 id : Queue) Q.Enqueue(id);

	while (!Q.IsEmpty())
	{
		int32 id; Q.Dequeue(id);
		int32 x = id % GridW;
		int32 y = id / GridW;

		// For each neighbour, eliminate incompatible options
		for (int side = 0; side < 4; side++)
		{
			int nx = x + DX[side];
			int ny = y + DY[side];
			if (!InBounds(nx, ny)) continue;
			int nid = Idx(nx, ny);
			FWfcCell& Neighbour = Grid[nid];
			FWfcCell& Source = Grid[nid];

			// For neighbour options, keep only those that are compatible with at least one source option
			TArray<FWfcOption> NewOptions;
			for (const FWfcOption& OptB : Neighbour.Options)
			{
				bool bCompatible = false;

				// If source is collapsed, check against its final option only
				if (Source.bCollapsed)
				{
					if (AreNeighboursCompatible(Source.Final.RotatedMask, side, OptB.RotatedMask))
					{
						bCompatible = true;
					}
					else
					{
						for (const FWfcOption& OptA : Source.Options)
						{
							if (AreNeighboursCompatible(OptA.RotatedMask, side, OptB.RotatedMask))
							{
								bCompatible = true;
								break;
							}
						}
					}
					if (bCompatible) NewOptions.Add(OptB);
				}

				if (NewOptions.Num() < Neighbour.Options.Num())
				{
					Neighbour.Options = MoveTemp(NewOptions);

					// If neighbour has become singleton, collapse it (but keep the Final set when collapsed in CollapseStep)
					if (Neighbour.Options.Num() == 1 && !Neighbour.bCollapsed)
					{
						Neighbour.Final = Neighbour.Options[0];
						Neighbour.bCollapsed = true;
						Q.Enqueue(nid);
					}
					else
					{
						// Re-enqueue neighbour to continue propagation
						Q.Enqueue(nid);
					}
				}
			}
		}
	}
}

// Compatibility: require bit equality on touching sides
// Side: 0=N 1-E 2=S 3=W (side of A touching neighbour)
bool ATileMapGenerator::AreNeighboursCompatible(uint8 A, int SideA, uint8 B)
{
	int Opp = (SideA + 2) & 3;
	int BitA = (A >> SideA) & 1;
	int BitB = (B >> Opp) & 1;

	// For traces to be connected, both bits should be 1
	return BitA == BitB;
}

// Extract paths from input ports to CPU and validate at least 3
// A BFS per input to the CPU using edges that connect (bit=1 both sides)
bool ATileMapGenerator::ExtractPathsAndValidate(TArray<TArray<FIntPoint>>& OutPaths)
{
	OutPaths.Empty();

	// Find CPU cell
	FIntPoint Cpu(-1, -1);
	TArray<FIntPoint> Inputs;
	for (int y = 0; y < GridH; y++)
	{
		for (int x = 0; x < GridW; x++)
		{
			const FWfcCell& C = Grid[Idx(x, y)];
			if (!C.bCollapsed) continue;
			const FTileVariant& V = TileSet->Variants[C.Final.VariantIndex];
			if (V.Category == ETileCategory::CPU) Cpu = { x, y };
			if (V.Category == ETileCategory::InputPort) Inputs.Add({x,y});
		}
	}

	if (Cpu.X == -1) return false;
	if (Inputs.Num() == 0) return false;

	// BFS from each input to CPU
	const int DX[4] = { 0, 1, 0, -1 };
	const int DY[4] = { -1, 0, 1, 0 };

	auto CanTraverse = [&](int ax, int ay, int bx, int by) -> bool
	{
		if (!InBounds(ax, ay) || !InBounds(bx, by)) return false;
		const FWfcCell& A = Grid[Idx(ax, ay)];
		const FWfcCell& B = Grid[Idx(bx, by)];
		if (!A.bCollapsed || !B.bCollapsed) return false;

		// Determine the side of A that faces B
		int side = -1;
		if (bx == ax && by == ay - 1) side = 0; // N
		else if (bx == ax + 1 && by == ay) side = 1; // E
		else if (bx == ax && by == ay + 1) side = 2; // S
		else if (bx == ax - 1 && by == ay) side = 3; // W
		if (side == -1) return false;
			
		return AreNeighboursCompatible(A.Final.RotatedMask, side, B.Final.RotatedMask);
	};

	for (const FIntPoint& InP : Inputs)
	{
		// BFS with parent map
		TQueue<FIntPoint> Q;
		Q.Enqueue(InP);
		TMap<FIntPoint, FIntPoint> Parent;
		TSet<FIntPoint> Visited;
		Visited.Add(InP);
		bool bFound = false;

		while (!Q.IsEmpty())
		{
			FIntPoint C; Q.Dequeue(C);
			if (C == Cpu) { bFound = true; break; }

			for (int d = 0; d < 4; d++)
			{
				int nx = C.X + DX[d];
				int ny = C.Y + DY[d];
				if (!InBounds(nx,ny)) continue;
				FIntPoint NP(nx,ny);
				if (Visited.Contains(NP)) continue;
				if (!CanTraverse(C.X, C.Y, nx, ny)) continue;
				Visited.Add(NP);
				Parent.Add(NP, C);
				Q.Enqueue(NP);
			}
		}

		if (bFound || Visited.Contains(Cpu))
		{
			// Reconstruct path
			TArray<FIntPoint> Path;
			FIntPoint Cur = Cpu;
			Path.Add(Cpu);

			while (Cur != InP)
			{
				if (!Parent.Contains(Cur)) break;
				Cur = Parent[Cur];
				Path.Add(Cur);
				if (Path.Num() > GridW * GridH) break;
			}
			Algo::Reverse(Path);
			if (Path.Num() >= 2)
			{
				OutPaths.Add(Path);
			}
		}

		// Early out if we have 3 already
		if (OutPaths.Num() >= 3) break;
	}
	
	// Accept if at least 3 distinct paths found
	return OutPaths.Num() >= 3;
}

// Spawn tile meshes and any special actors
void ATileMapGenerator::SpawnTilesAndSpecials()
{
	FTransform RootTransform = GetActorTransform();

	for (int y = 0; y < GridH; y++)
	{
		for (int x = 0; x < GridW; x++)
		{
			int id = Idx(x, y);
			if (!Grid[id].bCollapsed) continue;
			const FWfcOption& Opt = Grid[id].Final;
			SpawnTileMeshAt(x, y, Opt);

			// If the variant wants to spawn a special actor, do it rotated correctly
			const FTileVariant& V = TileSet->Variants[Opt.VariantIndex];
			if (V.SpawnActorClass)
			{
				FVector Loc = GetActorLocation() + FVector(x * CellSize, y * CellSize, SpawnZ);
				FRotator Rot(0.f, Opt.RotSteps * 90.f, 0.f);
				FActorSpawnParameters P;
				GetWorld()->SpawnActor<AActor>(V.SpawnActorClass, Loc, Rot, P);
			}
		}
	}
}

void ATileMapGenerator::SpawnTileMeshAt(int32 X, int32 Y, const FWfcOption& Opt)
{
	const FTileVariant& V = TileSet->Variants[Opt.VariantIndex];
	FVector Loc = GetActorLocation() + FVector(X * CellSize, Y * CellSize, SpawnZ);
	FRotator Rot(0.f, Opt.RotSteps * 90.f, 0.f);
	if (V.Mesh)
	{
		FActorSpawnParameters Params;
		AActor* Actor = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), Loc, Rot, Params);
		if (Actor)
		{
			UStaticMeshComponent* MeshComp = NewObject<UStaticMeshComponent>(Actor);
			MeshComp->RegisterComponent();
			MeshComp->SetStaticMesh(V.Mesh);
			Actor->SetRootComponent(MeshComp);
			Actor->SetActorRotation(Rot);
		}
	}
	else if (DefaultTileActorClass)
	{
		FActorSpawnParameters Params;
		GetWorld()->SpawnActor<AActor>(DefaultTileActorClass, Loc, Rot, Params);
	}
	else if (DefaultTileMesh)
	{
		// Spawn simple mesh actor
		AActor* Actor = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), Loc, Rot);
		if (Actor)
		{
			UStaticMeshComponent* MeshComp = NewObject<UStaticMeshComponent>(Actor);
			MeshComp->RegisterComponent();
			MeshComp->SetStaticMesh(DefaultTileMesh);
			Actor->SetRootComponent(MeshComp);
			Actor->SetActorRotation(Rot);
		}
	}
}

// Build splines: spawn PathSplineClass for each path and call a Build function if present
void ATileMapGenerator::BuildSplinesFromPaths(const TArray<TArray<FIntPoint>>& Paths)
{
	if (!PathSplineClass)
	{
		UE_LOG(LogTemp, Warning, TEXT("TileMapGenerator: PathSplineClass not set; skipping spline build."));
		return;
	}

	for (const TArray<FIntPoint>& P : Paths)
	{
		// Spawn actor
		FVector FirstLoc = GetActorLocation() + FVector(P[0].X * CellSize, P[0].Y * CellSize, SpawnZ);
		FRotator Rot = FRotator::ZeroRotator;
		FActorSpawnParameters Params;
		AActor* SplineActor = GetWorld()->SpawnActor<AActor>(PathSplineClass, FirstLoc, Rot, Params);
		if (SplineActor)
		{
			//  Try to find a function named "BuildFromCells" that takes TArray<FIntPoint> and float
			static FName FnName("BuildFromCell");
			UFunction* Fn = SplineActor->FindFunction(FnName);
			if (Fn)
			{
				struct FBuildParams
				{
					TArray<FIntPoint> Cells;
					float CellSize;
				};
				FBuildParams ParamsStruct;
				ParamsStruct.Cells = P;
				ParamsStruct.CellSize = CellSize;
				SplineActor->ProcessEvent(Fn, &ParamsStruct);
			}
			else
			{
				UE_LOG(LogTemp, Warning, TEXT("Spline actor spawned but has no BuildFromCells function."));
			}
		}
	}
}