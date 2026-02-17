using WindBladeInspector.Core.Entities;
using WindBladeInspector.Core.Enums;

namespace WindBladeInspector.Core.Services;

/// <summary>
/// Service for validating defect classifications according to hierarchical rules.
/// </summary>
public class DefectClassificationValidator
{
    /// <summary>
    /// Validates that a defect classification is valid according to the hierarchical schema.
    /// </summary>
    public ValidationResult Validate(DefectClassification classification)
    {
        var errors = new List<string>();
        
        // Validate Category
        if (classification.Category == ComponentCategory.Blade)
        {
            if (!classification.BladeMaterial.HasValue)
            {
                errors.Add("BladeMaterial is required when Category is Blade");
            }
            else
            {
                ValidateBladeMaterialDefect(classification, errors);
            }
            
            if (classification.AuxiliaryComponentType.HasValue)
            {
                errors.Add("AuxiliaryComponentType should be null when Category is Blade");
            }
        }
        else if (classification.Category == ComponentCategory.AuxiliaryComponent)
        {
            if (!classification.AuxiliaryComponentType.HasValue)
            {
                errors.Add("AuxiliaryComponentType is required when Category is AuxiliaryComponent");
            }
            else
            {
                ValidateAuxiliaryComponentDefect(classification, errors);
            }
            
            if (classification.BladeMaterial.HasValue)
            {
                errors.Add("BladeMaterial should be null when Category is AuxiliaryComponent");
            }
        }
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
    
    private void ValidateBladeMaterialDefect(DefectClassification classification, List<string> errors)
    {
        switch (classification.BladeMaterial!.Value)
        {
            case BladeMaterial.Surface:
                ValidateSurfaceDefect(classification, errors);
                break;
            case BladeMaterial.TopCoat:
                ValidateTopCoatDefect(classification, errors);
                break;
            case BladeMaterial.Laminate:
                ValidateLaminateDefect(classification, errors);
                break;
            case BladeMaterial.Structure:
                ValidateStructureDefect(classification, errors);
                break;
            case BladeMaterial.Through:
                ValidateThroughDefect(classification, errors);
                break;
        }
    }
    
    private void ValidateSurfaceDefect(DefectClassification classification, List<string> errors)
    {
        if (!Enum.IsDefined(typeof(SurfaceDefectType), classification.DefectType))
        {
            errors.Add($"Invalid DefectType {classification.DefectType} for Surface material");
            return;
        }
        
        var defectType = (SurfaceDefectType)classification.DefectType;
        
        switch (defectType)
        {
            case SurfaceDefectType.Discoloration:
                if (classification.DefectSubtype.HasValue && 
                    !Enum.IsDefined(typeof(SurfaceDiscolorationSubtype), classification.DefectSubtype.Value))
                {
                    errors.Add($"Invalid DefectSubtype {classification.DefectSubtype.Value} for Surface > Discoloration");
                }
                break;
            case SurfaceDefectType.Erosion:
                if (classification.DefectSubtype.HasValue && 
                    !Enum.IsDefined(typeof(SurfaceErosionSubtype), classification.DefectSubtype.Value))
                {
                    errors.Add($"Invalid DefectSubtype {classification.DefectSubtype.Value} for Surface > Erosion");
                }
                break;
        }
    }
    
    private void ValidateTopCoatDefect(DefectClassification classification, List<string> errors)
    {
        if (!Enum.IsDefined(typeof(TopCoatDefectType), classification.DefectType))
        {
            errors.Add($"Invalid DefectType {classification.DefectType} for TopCoat material");
            return;
        }
        
        var defectType = (TopCoatDefectType)classification.DefectType;
        
        switch (defectType)
        {
            case TopCoatDefectType.Crack:
                if (classification.DefectSubtype.HasValue && 
                    !Enum.IsDefined(typeof(TopCoatCrackSubtype), classification.DefectSubtype.Value))
                {
                    errors.Add($"Invalid DefectSubtype {classification.DefectSubtype.Value} for TopCoat > Crack");
                }
                break;
            case TopCoatDefectType.Pinholes:
                if (classification.DefectSubtype.HasValue && 
                    !Enum.IsDefined(typeof(TopCoatPinholesSubtype), classification.DefectSubtype.Value))
                {
                    errors.Add($"Invalid DefectSubtype {classification.DefectSubtype.Value} for TopCoat > Pinholes");
                }
                break;
            case TopCoatDefectType.Scratch:
            case TopCoatDefectType.Scorch:
                // No subtypes allowed
                if (classification.DefectSubtype.HasValue && classification.DefectSubtype.Value != 0)
                {
                    errors.Add($"DefectSubtype should be null or 0 for TopCoat > {defectType}");
                }
                break;
        }
    }
    
    private void ValidateLaminateDefect(DefectClassification classification, List<string> errors)
    {
        if (!Enum.IsDefined(typeof(LaminateDefectType), classification.DefectType))
        {
            errors.Add($"Invalid DefectType {classification.DefectType} for Laminate material");
            return;
        }
        
        var defectType = (LaminateDefectType)classification.DefectType;
        
        switch (defectType)
        {
            case LaminateDefectType.Erosion:
                if (classification.DefectSubtype.HasValue && 
                    !Enum.IsDefined(typeof(LaminateErosionSubtype), classification.DefectSubtype.Value))
                {
                    errors.Add($"Invalid DefectSubtype {classification.DefectSubtype.Value} for Laminate > Erosion");
                }
                break;
            case LaminateDefectType.Delamination:
                if (classification.DefectSubtype.HasValue && 
                    !Enum.IsDefined(typeof(LaminateDelaminationSubtype), classification.DefectSubtype.Value))
                {
                    errors.Add($"Invalid DefectSubtype {classification.DefectSubtype.Value} for Laminate > Delamination");
                }
                break;
            case LaminateDefectType.Scratch:
                // No subtypes allowed
                if (classification.DefectSubtype.HasValue && classification.DefectSubtype.Value != 0)
                {
                    errors.Add($"DefectSubtype should be null or 0 for Laminate > Scratch");
                }
                break;
        }
    }
    
    private void ValidateStructureDefect(DefectClassification classification, List<string> errors)
    {
        if (!Enum.IsDefined(typeof(StructureDefectType), classification.DefectType))
        {
            errors.Add($"Invalid DefectType {classification.DefectType} for Structure material");
            return;
        }
        
        var defectType = (StructureDefectType)classification.DefectType;
        
        switch (defectType)
        {
            case StructureDefectType.Crack:
                if (classification.DefectSubtype.HasValue && 
                    !Enum.IsDefined(typeof(StructureCrackSubtype), classification.DefectSubtype.Value))
                {
                    errors.Add($"Invalid DefectSubtype {classification.DefectSubtype.Value} for Structure > Crack");
                }
                break;
            case StructureDefectType.Delamination:
                if (classification.DefectSubtype.HasValue && 
                    !Enum.IsDefined(typeof(StructureDelaminationSubtype), classification.DefectSubtype.Value))
                {
                    errors.Add($"Invalid DefectSubtype {classification.DefectSubtype.Value} for Structure > Delamination");
                }
                break;
            case StructureDefectType.Erosion:
            case StructureDefectType.Hole:
                // No subtypes allowed
                if (classification.DefectSubtype.HasValue && classification.DefectSubtype.Value != 0)
                {
                    errors.Add($"DefectSubtype should be null or 0 for Structure > {defectType}");
                }
                break;
        }
    }
    
    private void ValidateThroughDefect(DefectClassification classification, List<string> errors)
    {
        if (!Enum.IsDefined(typeof(ThroughDefectType), classification.DefectType))
        {
            errors.Add($"Invalid DefectType {classification.DefectType} for Through material");
            return;
        }
        
        var defectType = (ThroughDefectType)classification.DefectType;
        
        switch (defectType)
        {
            case ThroughDefectType.Bondline:
                if (classification.DefectSubtype.HasValue && 
                    !Enum.IsDefined(typeof(ThroughBondlineSubtype), classification.DefectSubtype.Value))
                {
                    errors.Add($"Invalid DefectSubtype {classification.DefectSubtype.Value} for Through > Bondline");
                }
                break;
            case ThroughDefectType.Erosion:
                // No subtypes allowed
                if (classification.DefectSubtype.HasValue && classification.DefectSubtype.Value != 0)
                {
                    errors.Add($"DefectSubtype should be null or 0 for Through > Erosion");
                }
                break;
        }
    }
    
    private void ValidateAuxiliaryComponentDefect(DefectClassification classification, List<string> errors)
    {
        var isValid = classification.AuxiliaryComponentType!.Value switch
        {
            AuxiliaryComponent.Hub => Enum.IsDefined(typeof(HubDefectType), classification.DefectType),
            AuxiliaryComponent.Nozzle => Enum.IsDefined(typeof(NozzleDefectType), classification.DefectType),
            AuxiliaryComponent.VortexGenerators => Enum.IsDefined(typeof(VortexGeneratorDefectType), classification.DefectType),
            AuxiliaryComponent.Serrations => Enum.IsDefined(typeof(SerrationDefectType), classification.DefectType),
            AuxiliaryComponent.RainCollar => Enum.IsDefined(typeof(RainCollarDefectType), classification.DefectType),
            AuxiliaryComponent.LeadingEdgeProtection => Enum.IsDefined(typeof(LeadingEdgeProtectionDefectType), classification.DefectType),
            AuxiliaryComponent.PitchSystem => Enum.IsDefined(typeof(PitchSystemDefectType), classification.DefectType),
            AuxiliaryComponent.LightningReceptors => Enum.IsDefined(typeof(LightningReceptorDefectType), classification.DefectType),
            AuxiliaryComponent.GurneyFlaps => Enum.IsDefined(typeof(GurneyFlapDefectType), classification.DefectType),
            AuxiliaryComponent.Spoiler => Enum.IsDefined(typeof(SpoilerDefectType), classification.DefectType),
            AuxiliaryComponent.TipBrakeSystem => Enum.IsDefined(typeof(TipBrakeSystemDefectType), classification.DefectType),
            AuxiliaryComponent.Cover or AuxiliaryComponent.Other or AuxiliaryComponent.Bolts => true, // Generic types
            _ => false
        };
        
        if (!isValid)
        {
            errors.Add($"Invalid DefectType {classification.DefectType} for {classification.AuxiliaryComponentType.Value}");
        }
        
        // Auxiliary components don't have subtypes
        if (classification.DefectSubtype.HasValue && classification.DefectSubtype.Value != 0)
        {
            errors.Add("DefectSubtype should be null or 0 for AuxiliaryComponent defects");
        }
    }
    
    /// <summary>
    /// Gets all valid defect types for a given material or component.
    /// </summary>
    public IEnumerable<int> GetValidDefectTypes(ComponentCategory category, BladeMaterial? bladeMaterial, AuxiliaryComponent? auxComponent)
    {
        if (category == ComponentCategory.Blade && bladeMaterial.HasValue)
        {
            return bladeMaterial.Value switch
            {
                BladeMaterial.Surface => Enum.GetValues<SurfaceDefectType>().Cast<int>(),
                BladeMaterial.TopCoat => Enum.GetValues<TopCoatDefectType>().Cast<int>(),
                BladeMaterial.Laminate => Enum.GetValues<LaminateDefectType>().Cast<int>(),
                BladeMaterial.Structure => Enum.GetValues<StructureDefectType>().Cast<int>(),
                BladeMaterial.Through => Enum.GetValues<ThroughDefectType>().Cast<int>(),
                _ => Enumerable.Empty<int>()
            };
        }
        else if (category == ComponentCategory.AuxiliaryComponent && auxComponent.HasValue)
        {
            return auxComponent.Value switch
            {
                AuxiliaryComponent.Hub => Enum.GetValues<HubDefectType>().Cast<int>(),
                AuxiliaryComponent.Nozzle => Enum.GetValues<NozzleDefectType>().Cast<int>(),
                AuxiliaryComponent.VortexGenerators => Enum.GetValues<VortexGeneratorDefectType>().Cast<int>(),
                AuxiliaryComponent.Serrations => Enum.GetValues<SerrationDefectType>().Cast<int>(),
                AuxiliaryComponent.RainCollar => Enum.GetValues<RainCollarDefectType>().Cast<int>(),
                AuxiliaryComponent.LeadingEdgeProtection => Enum.GetValues<LeadingEdgeProtectionDefectType>().Cast<int>(),
                AuxiliaryComponent.PitchSystem => Enum.GetValues<PitchSystemDefectType>().Cast<int>(),
                AuxiliaryComponent.LightningReceptors => Enum.GetValues<LightningReceptorDefectType>().Cast<int>(),
                AuxiliaryComponent.GurneyFlaps => Enum.GetValues<GurneyFlapDefectType>().Cast<int>(),
                AuxiliaryComponent.Spoiler => Enum.GetValues<SpoilerDefectType>().Cast<int>(),
                AuxiliaryComponent.TipBrakeSystem => Enum.GetValues<TipBrakeSystemDefectType>().Cast<int>(),
                _ => Enumerable.Empty<int>()
            };
        }
        
        return Enumerable.Empty<int>();
    }
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
