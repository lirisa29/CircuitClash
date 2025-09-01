#include "CircuitClash/ProceduralGeneration/PathSpline.h"
#include "Components/SplineComponent.h"
#include "Components/InstancedStaticMeshComponent.h"
#include "Engine/StaticMesh.h"

APathSpline::APathSpline()
{
	PrimaryActorTick.bCanEverTick = false;

	Spline = CreateDefaultSubobject<USplineComponent>(TEXT("Spline"));
	RootComponent = Spline;

	TraceISM = CreateDefaultSubobject<UInstancedStaticMeshComponent>(TEXT("TraceISM"));
	TraceISM->SetupAttachment(RootComponent);
	TraceISM->SetCollisionEnabled(ECollisionEnabled::NoCollision);

	TraceSegmentMesh = nullptr;
	bUseCurvePoints = true;
}

void APathSpline::OnConstruction(const FTransform& Transform)
{
	Super::OnConstruction(Transform);

	if (TraceSegmentMesh)
	{
		TraceISM->SetStaticMesh(TraceSegmentMesh);
	}
}

void APathSpline::BuildFromCells(const TArray<FIntPoint>& Cells, float CellSize)
{
	if (Cells.Num() == 0) return;

	// Clear old spline
	Spline->ClearSplinePoints(false);
	TraceISM->ClearInstances();

	// Add a spline point per cell center
	for (int32 i = 0; i < Cells.Num(); ++i)
	{
		const FIntPoint& C = Cells[i];
		FVector WorldPos = GetActorLocation() + FVector(C.X * CellSize, C.Y * CellSize, 0.f);
		FSplinePoint Point(i, WorldPos, bUseCurvePoints ? ESplinePointType::Curve : ESplinePointType::Linear);
		Spline->AddPoint(Point, false);
	}

	Spline->SetClosedLoop(false);
	Spline->UpdateSpline();

	// Create instances for each segment if TraceSegmentMesh is present
	if (TraceSegmentMesh)
	{
		TraceISM->SetStaticMesh(TraceSegmentMesh);

		for (int32 i = 0; i < Spline->GetNumberOfSplinePoints() - 1; ++i)
		{
			FVector A = Spline->GetLocationAtSplinePoint(i, ESplineCoordinateSpace::World);
			FVector B = Spline->GetLocationAtSplinePoint(i + 1, ESplineCoordinateSpace::World);

			FVector Mid = (A + B) * 0.5f;
			FVector Dir = (B - A);
			
			float Len = Dir.Size();
			if (Len <= KINDA_SMALL_NUMBER) continue;

			FRotator Rot = Dir.Rotation();
			FTransform Xform;
			Xform.SetLocation(Mid);
			Xform.SetRotation(Rot.Quaternion());

			// Scale on X so that mesh length fits segment
			const float MeshUnitLength = 100.f;
			FVector Scale(Len / MeshUnitLength, 1.f, 1.f);
			Xform.SetScale3D(Scale);

			TraceISM->AddInstance(Xform);
		}
	}
}