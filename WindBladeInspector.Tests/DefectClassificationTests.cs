using WindBladeInspector.Core.Entities;
using WindBladeInspector.Core.Enums;
using Xunit;

namespace WindBladeInspector.Tests;

/// <summary>
/// Unit tests for DefectClassification entity.
/// Tests display string generation and path formatting.
/// </summary>
public class DefectClassificationTests
{
    [Fact]
    public void GetDefectTypeString_BladeSurface_ReturnsCorrectString()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Erosion
        };
        
        var result = classification.GetDefectTypeString();
        
        Assert.Equal("Erosion", result);
    }
    
    [Fact]
    public void GetDefectTypeString_BladeTopCoat_ReturnsCorrectString()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.TopCoat,
            DefectType = (int)TopCoatDefectType.Crack
        };
        
        var result = classification.GetDefectTypeString();
        
        Assert.Equal("Crack", result);
    }
    
    [Fact]
    public void GetDefectTypeString_AuxiliaryComponent_ReturnsCorrectString()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.AuxiliaryComponent,
            AuxiliaryComponentType = AuxiliaryComponent.VortexGenerators,
            DefectType = (int)VortexGeneratorDefectType.Missing
        };
        
        var result = classification.GetDefectTypeString();
        
        Assert.Equal("Missing", result);
    }
    
    [Fact]
    public void GetDefectSubtypeString_WithValidSubtype_ReturnsString()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Discoloration,
            DefectSubtype = (int)SurfaceDiscolorationSubtype.Mechanical
        };
        
        var result = classification.GetDefectSubtypeString();
        
        Assert.Equal("Mechanical", result);
    }
    
    [Fact]
    public void GetDefectSubtypeString_WithNullSubtype_ReturnsNull()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Erosion,
            DefectSubtype = null
        };
        
        var result = classification.GetDefectSubtypeString();
        
        Assert.Null(result);
    }
    
    [Fact]
    public void GetDefectSubtypeString_WithZeroSubtype_ReturnsNull()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.TopCoat,
            DefectType = (int)TopCoatDefectType.Crack,
            DefectSubtype = 0
        };
        
        var result = classification.GetDefectSubtypeString();
        
        Assert.Null(result);
    }
    
    [Fact]
    public void GetFullPath_BladeWithMaterialAndType_ReturnsFullHierarchy()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Erosion
        };
        
        var result = classification.GetFullPath();
        
        Assert.Equal("Blade > Surface > Erosion", result);
    }
    
    [Fact]
    public void GetFullPath_BladeWithSubtype_IncludesSubtype()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Erosion,
            DefectSubtype = (int)SurfaceErosionSubtype.Chip
        };
        
        var result = classification.GetFullPath();
        
        Assert.Equal("Blade > Surface > Erosion > Chip", result);
    }
    
    [Fact]
    public void GetFullPath_AuxiliaryComponent_ReturnsCorrectHierarchy()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.AuxiliaryComponent,
            AuxiliaryComponentType = AuxiliaryComponent.LightningReceptors,
            DefectType = (int)LightningReceptorDefectType.Missing
        };
        
        var result = classification.GetFullPath();
        
        Assert.Equal("AuxiliaryComponent > LightningReceptors > Missing", result);
    }
    
    [Fact]
    public void GetFullPath_ComplexPath_FormatsCorrectly()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.TopCoat,
            DefectType = (int)TopCoatDefectType.Crack,
            DefectSubtype = (int)TopCoatCrackSubtype.SpiderWebShaped
        };
        
        var result = classification.GetFullPath();
        
        Assert.Equal("Blade > TopCoat > Crack > SpiderWebShaped", result);
    }
}
