using WindBladeInspector.Core.Enums;

namespace WindBladeInspector.Core.Entities;

/// <summary>
/// Represents the hierarchical classification of a defect.
/// Structure: Component ? Material/Type ? DefectType ? Subtype
/// </summary>
public class DefectClassification
{
    /// <summary>Primary category: Blade or AuxiliaryComponent.</summary>
    public ComponentCategory Category { get; set; } = ComponentCategory.Blade;
    
    /// <summary>Blade material (if Category = Blade).</summary>
    public BladeMaterial? BladeMaterial { get; set; }
    
    /// <summary>Auxiliary component type (if Category = AuxiliaryComponent).</summary>
    public AuxiliaryComponent? AuxiliaryComponentType { get; set; }
    
    /// <summary>Defect type as integer (mapped to specific enum based on Material/Component).</summary>
    public int DefectType { get; set; }
    
    /// <summary>Defect subtype as integer (optional, mapped to specific enum based on DefectType).</summary>
    public int? DefectSubtype { get; set; }
    
    /// <summary>
    /// Gets the human-readable defect type string.
    /// </summary>
    public string GetDefectTypeString()
    {
        if (Category == ComponentCategory.Blade && BladeMaterial.HasValue)
        {
            return BladeMaterial.Value switch
            {
                Enums.BladeMaterial.Surface => ((SurfaceDefectType)DefectType).ToString(),
                Enums.BladeMaterial.TopCoat => ((TopCoatDefectType)DefectType).ToString(),
                Enums.BladeMaterial.Laminate => ((LaminateDefectType)DefectType).ToString(),
                Enums.BladeMaterial.Structure => ((StructureDefectType)DefectType).ToString(),
                Enums.BladeMaterial.Through => ((ThroughDefectType)DefectType).ToString(),
                _ => "Unknown"
            };
        }
        else if (Category == ComponentCategory.AuxiliaryComponent && AuxiliaryComponentType.HasValue)
        {
            return AuxiliaryComponentType.Value switch
            {
                Enums.AuxiliaryComponent.Hub => ((HubDefectType)DefectType).ToString(),
                Enums.AuxiliaryComponent.Nozzle => ((NozzleDefectType)DefectType).ToString(),
                Enums.AuxiliaryComponent.VortexGenerators => ((VortexGeneratorDefectType)DefectType).ToString(),
                Enums.AuxiliaryComponent.Serrations => ((SerrationDefectType)DefectType).ToString(),
                Enums.AuxiliaryComponent.RainCollar => ((RainCollarDefectType)DefectType).ToString(),
                Enums.AuxiliaryComponent.LeadingEdgeProtection => ((LeadingEdgeProtectionDefectType)DefectType).ToString(),
                Enums.AuxiliaryComponent.PitchSystem => ((PitchSystemDefectType)DefectType).ToString(),
                Enums.AuxiliaryComponent.LightningReceptors => ((LightningReceptorDefectType)DefectType).ToString(),
                Enums.AuxiliaryComponent.GurneyFlaps => ((GurneyFlapDefectType)DefectType).ToString(),
                Enums.AuxiliaryComponent.Spoiler => ((SpoilerDefectType)DefectType).ToString(),
                Enums.AuxiliaryComponent.TipBrakeSystem => ((TipBrakeSystemDefectType)DefectType).ToString(),
                _ => "Other"
            };
        }
        
        return "Unknown";
    }
    
    /// <summary>
    /// Gets the human-readable defect subtype string.
    /// </summary>
    public string? GetDefectSubtypeString()
    {
        if (!DefectSubtype.HasValue || DefectSubtype.Value == 0)
            return null;
            
        if (Category == ComponentCategory.Blade && BladeMaterial.HasValue)
        {
            return BladeMaterial.Value switch
            {
                Enums.BladeMaterial.Surface when DefectType == (int)SurfaceDefectType.Discoloration 
                    => ((SurfaceDiscolorationSubtype)DefectSubtype.Value).ToString(),
                Enums.BladeMaterial.Surface when DefectType == (int)SurfaceDefectType.Erosion 
                    => ((SurfaceErosionSubtype)DefectSubtype.Value).ToString(),
                Enums.BladeMaterial.TopCoat when DefectType == (int)TopCoatDefectType.Crack 
                    => ((TopCoatCrackSubtype)DefectSubtype.Value).ToString(),
                Enums.BladeMaterial.TopCoat when DefectType == (int)TopCoatDefectType.Pinholes 
                    => ((TopCoatPinholesSubtype)DefectSubtype.Value).ToString(),
                Enums.BladeMaterial.Laminate when DefectType == (int)LaminateDefectType.Erosion 
                    => ((LaminateErosionSubtype)DefectSubtype.Value).ToString(),
                Enums.BladeMaterial.Laminate when DefectType == (int)LaminateDefectType.Delamination 
                    => ((LaminateDelaminationSubtype)DefectSubtype.Value).ToString(),
                Enums.BladeMaterial.Structure when DefectType == (int)StructureDefectType.Crack 
                    => ((StructureCrackSubtype)DefectSubtype.Value).ToString(),
                Enums.BladeMaterial.Structure when DefectType == (int)StructureDefectType.Delamination 
                    => ((StructureDelaminationSubtype)DefectSubtype.Value).ToString(),
                Enums.BladeMaterial.Through when DefectType == (int)ThroughDefectType.Bondline 
                    => ((ThroughBondlineSubtype)DefectSubtype.Value).ToString(),
                _ => null
            };
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets the full hierarchical path as a string (e.g., "Blade > Surface > Erosion > Chip").
    /// </summary>
    public string GetFullPath()
    {
        var parts = new List<string>();
        
        parts.Add(Category.ToString());
        
        if (Category == ComponentCategory.Blade && BladeMaterial.HasValue)
        {
            parts.Add(BladeMaterial.Value.ToString());
        }
        else if (Category == ComponentCategory.AuxiliaryComponent && AuxiliaryComponentType.HasValue)
        {
            parts.Add(AuxiliaryComponentType.Value.ToString());
        }
        
        parts.Add(GetDefectTypeString());
        
        var subtype = GetDefectSubtypeString();
        if (!string.IsNullOrEmpty(subtype))
        {
            parts.Add(subtype);
        }
        
        return string.Join(" > ", parts);
    }
}
