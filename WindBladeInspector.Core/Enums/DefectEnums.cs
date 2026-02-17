namespace WindBladeInspector.Core.Enums;

/// <summary>
/// Primary component category for defects.
/// </summary>
public enum ComponentCategory
{
    Blade = 1,
    AuxiliaryComponent = 2
}

/// <summary>
/// Blade material types for classification.
/// </summary>
public enum BladeMaterial
{
    Surface = 1,
    TopCoat = 2,
    Laminate = 3,
    Structure = 4,
    Through = 5
}

/// <summary>
/// Auxiliary component types.
/// </summary>
public enum AuxiliaryComponent
{
    Hub = 1,
    Cover = 2,
    Other = 3,
    Nozzle = 4,
    VortexGenerators = 5,
    Serrations = 6,
    RainCollar = 7,
    LeadingEdgeProtection = 8,
    PitchSystem = 9,
    LightningReceptors = 10,
    GurneyFlaps = 11,
    Spoiler = 12,
    TipBrakeSystem = 13,
    Bolts = 14
}

/// <summary>
/// Surface defect types (Blade > Surface).
/// </summary>
public enum SurfaceDefectType
{
    Discoloration = 1,
    Erosion = 2
}

/// <summary>
/// Surface discoloration subtypes.
/// </summary>
public enum SurfaceDiscolorationSubtype
{
    Mechanical = 1,
    Scorch = 2,
    IceContamination = 3
}

/// <summary>
/// Surface erosion subtypes.
/// </summary>
public enum SurfaceErosionSubtype
{
    Chip = 1,
    Flaking = 2
}

/// <summary>
/// Top Coat defect types (Blade > TopCoat).
/// </summary>
public enum TopCoatDefectType
{
    Crack = 1,
    Scratch = 2,
    Pinholes = 3,
    Scorch = 4
}

/// <summary>
/// Top Coat crack subtypes.
/// </summary>
public enum TopCoatCrackSubtype
{
    None = 0,
    FatigueCracks = 1,
    TLCShapedCracks = 2,
    SpiderWebShaped = 3,
    BondTransverseOnTE = 4,
    BondLongitudinalOnTE = 5,
    BondTransverseOnLE = 6,
    BondLongitudinalOnLE = 7
}

/// <summary>
/// Top Coat pinholes subtypes.
/// </summary>
public enum TopCoatPinholesSubtype
{
    None = 0,
    Scorch = 1
}

/// <summary>
/// Laminate defect types (Blade > Laminate).
/// </summary>
public enum LaminateDefectType
{
    Erosion = 1,
    Scratch = 2,
    Delamination = 3
}

/// <summary>
/// Laminate erosion subtypes.
/// </summary>
public enum LaminateErosionSubtype
{
    Chip = 1,
    None = 0,
    Lightning = 2
}

/// <summary>
/// Laminate delamination subtypes.
/// </summary>
public enum LaminateDelaminationSubtype
{
    None = 0,
    Lightning = 1
}

/// <summary>
/// Structure defect types (Blade > Structure).
/// </summary>
public enum StructureDefectType
{
    Erosion = 1,
    Crack = 2,
    Delamination = 3,
    Hole = 4
}

/// <summary>
/// Structure crack subtypes.
/// </summary>
public enum StructureCrackSubtype
{
    Transverse = 1,
    Longitudinal = 2,
    TLCShapedCracks = 3,
    Other = 4,
    TrailingTransverse = 5,
    Diagonal = 6,
    Surface = 7
}

/// <summary>
/// Structure delamination subtypes.
/// </summary>
public enum StructureDelaminationSubtype
{
    Edge = 1,
    Lightning = 2,
    NonLightning = 3
}

/// <summary>
/// Through defect types (Blade > Through).
/// </summary>
public enum ThroughDefectType
{
    Erosion = 1,
    Bondline = 2
}

/// <summary>
/// Through bondline subtypes.
/// </summary>
public enum ThroughBondlineSubtype
{
    None = 0,
    Crushed = 1,
    OpenTip = 2
}

/// <summary>
/// Hub defect types.
/// </summary>
public enum HubDefectType
{
    Damaged = 1
}

/// <summary>
/// Nozzle defect types.
/// </summary>
public enum NozzleDefectType
{
    Damaged = 1,
    Crack = 2
}

/// <summary>
/// Vortex Generator defect types.
/// </summary>
public enum VortexGeneratorDefectType
{
    DamagedOrMisaligned = 1,
    Missing = 2
}

/// <summary>
/// Serration defect types.
/// </summary>
public enum SerrationDefectType
{
    DamagedOrMisaligned = 1,
    Missing = 2
}

/// <summary>
/// Rain Collar defect types.
/// </summary>
public enum RainCollarDefectType
{
    Peeling = 1,
    Damaged = 2
}

/// <summary>
/// Leading Edge Protection defect types.
/// </summary>
public enum LeadingEdgeProtectionDefectType
{
    Damaged = 1,
    Missing = 2
}

/// <summary>
/// Pitch System defect types.
/// </summary>
public enum PitchSystemDefectType
{
    Damaged = 1,
    Other = 2
}

/// <summary>
/// Lightning Receptor defect types.
/// </summary>
public enum LightningReceptorDefectType
{
    DamagedOrMisaligned = 1,
    Missing = 2
}

/// <summary>
/// Gurney Flap defect types.
/// </summary>
public enum GurneyFlapDefectType
{
    DamagedOrMisaligned = 1,
    Missing = 2
}

/// <summary>
/// Spoiler defect types.
/// </summary>
public enum SpoilerDefectType
{
    DamagedOrMisaligned = 1,
    Missing = 2
}

/// <summary>
/// Tip Brake System defect types.
/// </summary>
public enum TipBrakeSystemDefectType
{
    DamagedOrMisaligned = 1,
    Crack = 2
}
