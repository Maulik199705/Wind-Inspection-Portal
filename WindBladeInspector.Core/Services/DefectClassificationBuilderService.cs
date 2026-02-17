using WindBladeInspector.Core.Entities;
using WindBladeInspector.Core.Enums;

namespace WindBladeInspector.Core.Services;

/// <summary>
/// Service for building defect classifications in the UI.
/// Provides dropdown options and hierarchical navigation.
/// </summary>
public class DefectClassificationBuilderService
{
    private readonly DefectMigrationService _migrationService;
    private readonly DefectClassificationValidator _validator;
    
    public DefectClassificationBuilderService(
        DefectMigrationService migrationService,
        DefectClassificationValidator validator)
    {
        _migrationService = migrationService;
        _validator = validator;
    }
    
    /// <summary>
    /// Gets all primary categories.
    /// </summary>
    public Dictionary<ComponentCategory, string> GetCategories()
    {
        return new Dictionary<ComponentCategory, string>
        {
            { ComponentCategory.Blade, "Blade" },
            { ComponentCategory.AuxiliaryComponent, "Auxiliary Component" }
        };
    }
    
    /// <summary>
    /// Gets all blade materials.
    /// </summary>
    public Dictionary<BladeMaterial, string> GetBladeMaterials()
    {
        return new Dictionary<BladeMaterial, string>
        {
            { BladeMaterial.Surface, "Surface" },
            { BladeMaterial.TopCoat, "Top Coat" },
            { BladeMaterial.Laminate, "Laminate" },
            { BladeMaterial.Structure, "Structure" },
            { BladeMaterial.Through, "Through" }
        };
    }
    
    /// <summary>
    /// Gets all auxiliary component types.
    /// </summary>
    public Dictionary<AuxiliaryComponent, string> GetAuxiliaryComponents()
    {
        return new Dictionary<AuxiliaryComponent, string>
        {
            { AuxiliaryComponent.Hub, "Hub" },
            { AuxiliaryComponent.Cover, "Cover" },
            { AuxiliaryComponent.Other, "Other" },
            { AuxiliaryComponent.Nozzle, "Nozzle" },
            { AuxiliaryComponent.VortexGenerators, "Vortex Generators" },
            { AuxiliaryComponent.Serrations, "Serrations" },
            { AuxiliaryComponent.RainCollar, "Rain Collar" },
            { AuxiliaryComponent.LeadingEdgeProtection, "Leading Edge Protection" },
            { AuxiliaryComponent.PitchSystem, "Pitch System" },
            { AuxiliaryComponent.LightningReceptors, "Lightning Receptors" },
            { AuxiliaryComponent.GurneyFlaps, "Gurney Flaps" },
            { AuxiliaryComponent.Spoiler, "Spoiler" },
            { AuxiliaryComponent.TipBrakeSystem, "Tip Brake System" },
            { AuxiliaryComponent.Bolts, "Bolts" }
        };
    }
    
    /// <summary>
    /// Gets defect types for a given blade material.
    /// </summary>
    public Dictionary<int, string> GetDefectTypesForBladeMaterial(BladeMaterial material)
    {
        return material switch
        {
            BladeMaterial.Surface => Enum.GetValues<SurfaceDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            BladeMaterial.TopCoat => Enum.GetValues<TopCoatDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            BladeMaterial.Laminate => Enum.GetValues<LaminateDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            BladeMaterial.Structure => Enum.GetValues<StructureDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            BladeMaterial.Through => Enum.GetValues<ThroughDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            _ => new Dictionary<int, string>()
        };
    }
    
    /// <summary>
    /// Gets defect types for a given auxiliary component.
    /// </summary>
    public Dictionary<int, string> GetDefectTypesForAuxiliaryComponent(AuxiliaryComponent component)
    {
        return component switch
        {
            AuxiliaryComponent.Hub => Enum.GetValues<HubDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            AuxiliaryComponent.Nozzle => Enum.GetValues<NozzleDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            AuxiliaryComponent.VortexGenerators => Enum.GetValues<VortexGeneratorDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            AuxiliaryComponent.Serrations => Enum.GetValues<SerrationDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            AuxiliaryComponent.RainCollar => Enum.GetValues<RainCollarDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            AuxiliaryComponent.LeadingEdgeProtection => Enum.GetValues<LeadingEdgeProtectionDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            AuxiliaryComponent.PitchSystem => Enum.GetValues<PitchSystemDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            AuxiliaryComponent.LightningReceptors => Enum.GetValues<LightningReceptorDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            AuxiliaryComponent.GurneyFlaps => Enum.GetValues<GurneyFlapDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            AuxiliaryComponent.Spoiler => Enum.GetValues<SpoilerDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            AuxiliaryComponent.TipBrakeSystem => Enum.GetValues<TipBrakeSystemDefectType>()
                .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            AuxiliaryComponent.Cover => new Dictionary<int, string> { { 1, "Damaged" } },
            AuxiliaryComponent.Other => new Dictionary<int, string> { { 1, "Other" } },
            AuxiliaryComponent.Bolts => new Dictionary<int, string> { { 1, "Damaged" } },
            
            _ => new Dictionary<int, string>()
        };
    }
    
    /// <summary>
    /// Gets subtypes for a given blade material and defect type combination.
    /// </summary>
    public Dictionary<int, string>? GetSubtypesForDefect(BladeMaterial material, int defectType)
    {
        return material switch
        {
            BladeMaterial.Surface when defectType == (int)SurfaceDefectType.Discoloration =>
                Enum.GetValues<SurfaceDiscolorationSubtype>()
                    .Where(e => e != SurfaceDiscolorationSubtype.IceContamination) // Filter if needed
                    .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            BladeMaterial.Surface when defectType == (int)SurfaceDefectType.Erosion =>
                Enum.GetValues<SurfaceErosionSubtype>()
                    .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            BladeMaterial.TopCoat when defectType == (int)TopCoatDefectType.Crack =>
                Enum.GetValues<TopCoatCrackSubtype>()
                    .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            BladeMaterial.TopCoat when defectType == (int)TopCoatDefectType.Pinholes =>
                Enum.GetValues<TopCoatPinholesSubtype>()
                    .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            BladeMaterial.Laminate when defectType == (int)LaminateDefectType.Erosion =>
                Enum.GetValues<LaminateErosionSubtype>()
                    .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            BladeMaterial.Laminate when defectType == (int)LaminateDefectType.Delamination =>
                Enum.GetValues<LaminateDelaminationSubtype>()
                    .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            BladeMaterial.Structure when defectType == (int)StructureDefectType.Crack =>
                Enum.GetValues<StructureCrackSubtype>()
                    .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            BladeMaterial.Structure when defectType == (int)StructureDefectType.Delamination =>
                Enum.GetValues<StructureDelaminationSubtype>()
                    .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            BladeMaterial.Through when defectType == (int)ThroughDefectType.Bondline =>
                Enum.GetValues<ThroughBondlineSubtype>()
                    .ToDictionary(e => (int)e, e => FormatEnumName(e.ToString())),
            
            _ => null // No subtypes for this combination
        };
    }
    
    /// <summary>
    /// Creates a defect classification from legacy string type.
    /// </summary>
    public DefectClassification CreateFromLegacy(string legacyType)
    {
        return _migrationService.MigrateLegacyType(legacyType);
    }
    
    /// <summary>
    /// Formats an enum name to be more readable (adds spaces before capitals).
    /// </summary>
    private string FormatEnumName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        
        // Special cases
        name = name.Replace("TLCShapedCracks", "T-LC Shaped Cracks");
        name = name.Replace("DamagedOrMisaligned", "Damaged or Misaligned");
        name = name.Replace("TE", " TE");
        name = name.Replace("LE", " LE");
        name = name.Replace("PS", " PS");
        name = name.Replace("SS", " SS");
        
        // Add space before capitals
        return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
    }
}
