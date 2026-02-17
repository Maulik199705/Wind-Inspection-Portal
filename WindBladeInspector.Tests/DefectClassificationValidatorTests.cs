using WindBladeInspector.Core.Entities;
using WindBladeInspector.Core.Enums;
using WindBladeInspector.Core.Services;
using Xunit;

namespace WindBladeInspector.Tests;

/// <summary>
/// Unit tests for DefectClassificationValidator.
/// Tests all valid combinations and ensures invalid subtypes are rejected.
/// </summary>
public class DefectClassificationValidatorTests
{
    private readonly DefectClassificationValidator _validator;
    
    public DefectClassificationValidatorTests()
    {
        _validator = new DefectClassificationValidator();
    }
    
    #region Blade Surface Tests
    
    [Fact]
    public void Validate_BladeSurfaceDiscolorationMechanical_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Discoloration,
            DefectSubtype = (int)SurfaceDiscolorationSubtype.Mechanical
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
    
    [Fact]
    public void Validate_BladeSurfaceErosionChip_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Erosion,
            DefectSubtype = (int)SurfaceErosionSubtype.Chip
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
    }
    
    [Fact]
    public void Validate_BladeSurfaceWithInvalidSubtype_IsInvalid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Discoloration,
            DefectSubtype = 999 // Invalid subtype
        };
        
        var result = _validator.Validate(classification);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Invalid DefectSubtype"));
    }
    
    #endregion
    
    #region Blade TopCoat Tests
    
    [Fact]
    public void Validate_BladeTopCoatCrackFatigue_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.TopCoat,
            DefectType = (int)TopCoatDefectType.Crack,
            DefectSubtype = (int)TopCoatCrackSubtype.FatigueCracks
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
    }
    
    [Fact]
    public void Validate_BladeTopCoatPinholes_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.TopCoat,
            DefectType = (int)TopCoatDefectType.Pinholes,
            DefectSubtype = null
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
    }
    
    [Fact]
    public void Validate_BladeTopCoatScratchWithSubtype_IsInvalid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.TopCoat,
            DefectType = (int)TopCoatDefectType.Scratch,
            DefectSubtype = 1 // Scratch shouldn't have subtypes
        };
        
        var result = _validator.Validate(classification);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("should be null or 0"));
    }
    
    #endregion
    
    #region Blade Laminate Tests
    
    [Fact]
    public void Validate_BladeLaminateErosionLightning_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Laminate,
            DefectType = (int)LaminateDefectType.Erosion,
            DefectSubtype = (int)LaminateErosionSubtype.Lightning
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
    }
    
    [Fact]
    public void Validate_BladeLaminateDelaminationNonLightning_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Laminate,
            DefectType = (int)LaminateDefectType.Delamination,
            DefectSubtype = 0 // None is valid
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
    }
    
    #endregion
    
    #region Blade Structure Tests
    
    [Fact]
    public void Validate_BladeStructureCrackTransverse_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Structure,
            DefectType = (int)StructureDefectType.Crack,
            DefectSubtype = (int)StructureCrackSubtype.Transverse
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
    }
    
    [Fact]
    public void Validate_BladeStructureDelaminationEdge_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Structure,
            DefectType = (int)StructureDefectType.Delamination,
            DefectSubtype = (int)StructureDelaminationSubtype.Edge
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
    }
    
    [Fact]
    public void Validate_BladeStructureHole_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Structure,
            DefectType = (int)StructureDefectType.Hole,
            DefectSubtype = null
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
    }
    
    #endregion
    
    #region Blade Through Tests
    
    [Fact]
    public void Validate_BladeThroughBondlineCrushed_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Through,
            DefectType = (int)ThroughDefectType.Bondline,
            DefectSubtype = (int)ThroughBondlineSubtype.Crushed
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
    }
    
    [Fact]
    public void Validate_BladeThroughErosion_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Through,
            DefectType = (int)ThroughDefectType.Erosion,
            DefectSubtype = null
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
    }
    
    #endregion
    
    #region Auxiliary Component Tests
    
    [Fact]
    public void Validate_AuxiliaryVortexGeneratorsDamaged_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.AuxiliaryComponent,
            AuxiliaryComponentType = AuxiliaryComponent.VortexGenerators,
            DefectType = (int)VortexGeneratorDefectType.DamagedOrMisaligned
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
    }
    
    [Fact]
    public void Validate_AuxiliaryLightningReceptorMissing_IsValid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.AuxiliaryComponent,
            AuxiliaryComponentType = AuxiliaryComponent.LightningReceptors,
            DefectType = (int)LightningReceptorDefectType.Missing
        };
        
        var result = _validator.Validate(classification);
        
        Assert.True(result.IsValid);
    }
    
    [Fact]
    public void Validate_AuxiliaryComponentWithSubtype_IsInvalid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.AuxiliaryComponent,
            AuxiliaryComponentType = AuxiliaryComponent.Nozzle,
            DefectType = (int)NozzleDefectType.Damaged,
            DefectSubtype = 1 // Auxiliary components don't have subtypes
        };
        
        var result = _validator.Validate(classification);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("should be null or 0"));
    }
    
    #endregion
    
    #region Category Validation Tests
    
    [Fact]
    public void Validate_BladeWithoutMaterial_IsInvalid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = null,
            DefectType = 1
        };
        
        var result = _validator.Validate(classification);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("BladeMaterial is required"));
    }
    
    [Fact]
    public void Validate_AuxiliaryWithoutComponent_IsInvalid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.AuxiliaryComponent,
            AuxiliaryComponentType = null,
            DefectType = 1
        };
        
        var result = _validator.Validate(classification);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("AuxiliaryComponentType is required"));
    }
    
    [Fact]
    public void Validate_BladeMaterialWithAuxiliaryComponent_IsInvalid()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            AuxiliaryComponentType = AuxiliaryComponent.Hub, // Should be null
            DefectType = (int)SurfaceDefectType.Erosion
        };
        
        var result = _validator.Validate(classification);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("AuxiliaryComponentType should be null"));
    }
    
    #endregion
}
