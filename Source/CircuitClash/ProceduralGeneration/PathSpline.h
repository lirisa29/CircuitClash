#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "PathSpline.generated.h"

UCLASS()
class CIRCUITCLASH_API APathSpline : public AActor
{
	GENERATED_BODY()
	
public:	
	APathSpline();

	// Spline component that represent the enemy route
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Spline")
	class USplineComponent* Spline;

	// Instanced mesh used to visually show segments
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Spline")
	class UInstancedStaticMeshComponent* TraceISM;

	// Trace mesh and base length used for scaling instances
	UPROPERTY(EditAnywhere, Category = "Spline|Visual")
	UStaticMesh* TraceSegmentMesh;

	// If true, BuildFromCells will set spline points to curve type for smoothing
	UPROPERTY(EditAnywhere, Category = "Spline|Behaviour")
	bool bUseCurvePoints = true;

	// Build spline from grid cells (this function is called by the generator)
	UFUNCTION(BlueprintCallable, Category = "Spline")
	void BuildFromCells(const TArray<FIntPoint>& Cells, float CellSize);

protected:
	virtual void OnConstruction(const FTransform& Transform) override;
};
