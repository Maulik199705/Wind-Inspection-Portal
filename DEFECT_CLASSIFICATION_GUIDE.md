# Defect Classification System - Implementation Guide

## Overview

The defect classification system has been refactored from a simple string-based approach to a strict hierarchical model following the standardized wind blade inspection schema.

## Architecture

### Hierarchical Structure

```
Primary Category
??? Blade
?   ??? Material Type (Surface, TopCoat, Laminate, Structure, Through)
?   ?   ??? Defect Type
?   ?   ?   ??? Defect Subtype (optional)
??? AuxiliaryComponent
    ??? Component Type (Hub, Nozzle, VortexGenerators, etc.)
    ?   ??? Defect Type
```

## Core Components

### 1. Enums (`DefectEnums.cs`)

All classification levels are defined as strongly-typed enums:

- **ComponentCategory**: Blade or AuxiliaryComponent
- **BladeMaterial**: Surface, TopCoat, Laminate, Structure, Through
- **AuxiliaryComponent**: Hub, Cover, Nozzle, VortexGenerators, etc.
- **Defect Type Enums**: Specific to each material/component
- **Defect Subtype Enums**: Specific to certain defect types

### 2. Entity (`DefectClassification.cs`)

Represents a complete defect classification:

```csharp
public class DefectClassification
{
    public ComponentCategory Category { get; set; }
    public BladeMaterial? BladeMaterial { get; set; }
    public AuxiliaryComponent? AuxiliaryComponentType { get; set; }
    public int DefectType { get; set; }
    public int? DefectSubtype { get; set; }
}
```

**Key Methods:**
- `GetDefectTypeString()`: Returns human-readable defect type
- `GetDefectSubtypeString()`: Returns human-readable subtype
- `GetFullPath()`: Returns full hierarchy (e.g., "Blade > Surface > Erosion > Chip")

### 3. Validator (`DefectClassificationValidator.cs`)

Enforces hierarchical rules:

```csharp
var validator = new DefectClassificationValidator();
var result = validator.Validate(classification);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error);
    }
}
```

**Validation Rules:**
- Category-specific material/component requirements
- Valid defect types for each material/component
- Valid subtypes for each defect type
- No subtypes where not allowed

### 4. Migration Service (`DefectMigrationService.cs`)

Provides backward compatibility with legacy string types:

```csharp
var migrationService = new DefectMigrationService();

// Legacy ? New
var classification = migrationService.MigrateLegacyType("Erosion");
// Result: Blade > Surface > Erosion

// New ? Legacy (for display)
var legacyString = migrationService.ToLegacyString(classification);
```

**Legacy Mappings:**
- "Erosion" ? Blade > Surface > Erosion
- "Crack" ? Blade > TopCoat > Crack
- "Pinholes" ? Blade > TopCoat > Pinholes
- "Flaking" ? Blade > Surface > Erosion > Flaking
- "Discoloration" ? Blade > Surface > Discoloration
- "Damaged or Misaligned" ? AuxiliaryComponent > VortexGenerators > DamagedOrMisaligned
- "Other" ? Blade > Surface > Discoloration (default)

### 5. Builder Service (`DefectClassificationBuilderService.cs`)

Helps build UI dropdowns and navigation:

```csharp
var builder = new DefectClassificationBuilderService(migrationService, validator);

// Get dropdown options
var categories = builder.GetCategories();
var materials = builder.GetBladeMaterials();
var defectTypes = builder.GetDefectTypesForBladeMaterial(BladeMaterial.Surface);
var subtypes = builder.GetSubtypesForDefect(BladeMaterial.Surface, (int)SurfaceDefectType.Discoloration);
```

## Complete Schema

### 1. Blade > Surface

| Defect Type | Subtype Options |
|-------------|----------------|
| Discoloration | Mechanical, Scorch, IceContamination |
| Erosion | Chip, Flaking |

### 2. Blade > TopCoat

| Defect Type | Subtype Options |
|-------------|----------------|
| Crack | None, FatigueCracks, TLCShapedCracks, SpiderWebShaped, BondTransverseOnTE, BondLongitudinalOnTE, BondTransverseOnLE, BondLongitudinalOnLE |
| Scratch | (none) |
| Pinholes | None, Scorch |
| Scorch | (none) |

### 3. Blade > Laminate

| Defect Type | Subtype Options |
|-------------|----------------|
| Erosion | Chip, None, Lightning |
| Scratch | (none) |
| Delamination | None, Lightning |

### 4. Blade > Structure

| Defect Type | Subtype Options |
|-------------|----------------|
| Erosion | (none) |
| Crack | Transverse, Longitudinal, TLCShapedCracks, Other, TrailingTransverse, Diagonal, Surface |
| Delamination | Edge, Lightning, NonLightning |
| Hole | (none) |

### 5. Blade > Through

| Defect Type | Subtype Options |
|-------------|----------------|
| Erosion | (none) |
| Bondline | None, Crushed, OpenTip |

### 6. Auxiliary Components

| Component | Defect Types |
|-----------|-------------|
| Hub | Damaged |
| Cover | Other |
| Nozzle | Damaged, Crack |
| VortexGenerators | DamagedOrMisaligned, Missing |
| Serrations | DamagedOrMisaligned, Missing |
| RainCollar | Peeling, Damaged |
| LeadingEdgeProtection | Damaged, Missing |
| PitchSystem | Damaged, Other |
| LightningReceptors | DamagedOrMisaligned, Missing |
| GurneyFlaps | DamagedOrMisaligned, Missing |
| Spoiler | DamagedOrMisaligned, Missing |
| TipBrakeSystem | DamagedOrMisaligned, Crack |
| Bolts | Damaged |

## Integration with Anomaly Entity

The `Anomaly` entity now includes both legacy and new classification:

```csharp
public class Anomaly
{
    // Legacy (kept for backward compatibility)
    public string Type { get; set; } = "Other";
    
    // New hierarchical classification
    public DefectClassification? Classification { get; set; }
    
    // Helper method
    public string GetDefectTypeDisplay()
    {
        if (Classification != null)
            return Classification.GetFullPath();
        return Type;
    }
}
```

## Usage Examples

### Creating a New Defect

```csharp
var anomaly = new Anomaly
{
    Severity = 3,
    Classification = new DefectClassification
    {
        Category = ComponentCategory.Blade,
        BladeMaterial = BladeMaterial.Surface,
        DefectType = (int)SurfaceDefectType.Erosion,
        DefectSubtype = (int)SurfaceErosionSubtype.Chip
    }
};

// Display: "Blade > Surface > Erosion > Chip"
Console.WriteLine(anomaly.Classification.GetFullPath());
```

### Migrating Legacy Data

```csharp
var migrationService = new DefectMigrationService();

// Convert old anomalies
foreach (var anomaly in oldAnomalies)
{
    if (anomaly.Classification == null && !string.IsNullOrEmpty(anomaly.Type))
    {
        anomaly.Classification = migrationService.MigrateLegacyType(anomaly.Type);
    }
}
```

### Building UI Dropdowns

```csharp
var builder = new DefectClassificationBuilderService(migrationService, validator);

// Step 1: Category
var categories = builder.GetCategories(); // Blade, AuxiliaryComponent

// Step 2: Material (if Blade selected)
var materials = builder.GetBladeMaterials(); // Surface, TopCoat, etc.

// Step 3: Defect Type
var defectTypes = builder.GetDefectTypesForBladeMaterial(BladeMaterial.Surface);
// { 1: "Discoloration", 2: "Erosion" }

// Step 4: Subtype (optional)
var subtypes = builder.GetSubtypesForDefect(BladeMaterial.Surface, (int)SurfaceDefectType.Discoloration);
// { 1: "Mechanical", 2: "Scorch", 3: "Ice Contamination" }
```

## Testing

The implementation includes comprehensive unit tests:

1. **DefectClassificationValidatorTests** (26 tests)
   - Valid combinations for all materials and components
   - Invalid subtype rejection
   - Category validation

2. **DefectMigrationServiceTests** (15 tests)
   - Legacy type mapping
   - Partial string matching
   - Reverse mapping

3. **DefectClassificationTests** (6 tests)
   - Display string generation
   - Full path formatting

Run tests:
```bash
dotnet test WindBladeInspector.Tests
```

## Migration Checklist

When updating existing code:

1. ? **Entity Updated**: `Anomaly` now has `Classification` property
2. ? **Services Registered**: Added to DI container in `Program.cs`
3. ?? **UI Updates Needed**: Update Blazor forms to use hierarchical dropdowns
4. ?? **Data Migration**: Run migration service on existing data
5. ?? **Reports**: Update report generation to use `GetFullPath()`

## Next Steps

### For UI Integration:

1. Create a Blazor component for defect classification selection:
   - Cascading dropdowns based on hierarchy
   - Dynamic options based on parent selection
   
2. Update the Inspection.razor defect form to use new component

3. Update display logic to show full classification path

### For Data Persistence:

If adding database support:
- Store as separate columns or JSON
- Consider computed columns for display paths
- Add database migration for existing records

## Best Practices

1. **Always validate** classifications before saving
2. **Use the migration service** when dealing with legacy data
3. **Prefer Classification over Type** in new code
4. **Keep Type property** for backward compatibility
5. **Use GetFullPath()** for user-facing displays
6. **Use builder service** for UI dropdown generation

## Support

For questions or issues with the defect classification system:
- Review unit tests for usage examples
- Check validator error messages for specific issues
- Use migration service for backward compatibility
