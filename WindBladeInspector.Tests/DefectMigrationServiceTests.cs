using WindBladeInspector.Core.Entities;
using WindBladeInspector.Core.Enums;
using WindBladeInspector.Core.Services;
using Xunit;

namespace WindBladeInspector.Tests;

/// <summary>
/// Unit tests for DefectMigrationService.
/// Tests legacy type mapping and conversion correctness.
/// </summary>
public class DefectMigrationServiceTests
{
    private readonly DefectMigrationService _migrationService;
    
    public DefectMigrationServiceTests()
    {
        _migrationService = new DefectMigrationService();
    }
    
    [Theory]
    [InlineData("Erosion", ComponentCategory.Blade, BladeMaterial.Surface, (int)SurfaceDefectType.Erosion)]
    [InlineData("Crack", ComponentCategory.Blade, BladeMaterial.TopCoat, (int)TopCoatDefectType.Crack)]
    [InlineData("Pinholes", ComponentCategory.Blade, BladeMaterial.TopCoat, (int)TopCoatDefectType.Pinholes)]
    [InlineData("Discoloration", ComponentCategory.Blade, BladeMaterial.Surface, (int)SurfaceDefectType.Discoloration)]
    public void MigrateLegacyType_KnownTypes_MapsCorrectly(
        string legacyType,
        ComponentCategory expectedCategory,
        BladeMaterial expectedMaterial,
        int expectedDefectType)
    {
        var result = _migrationService.MigrateLegacyType(legacyType);
        
        Assert.Equal(expectedCategory, result.Category);
        Assert.Equal(expectedMaterial, result.BladeMaterial);
        Assert.Equal(expectedDefectType, result.DefectType);
    }
    
    [Fact]
    public void MigrateLegacyType_Flaking_MapsToSurfaceErosionFlaking()
    {
        var result = _migrationService.MigrateLegacyType("Flaking");
        
        Assert.Equal(ComponentCategory.Blade, result.Category);
        Assert.Equal(BladeMaterial.Surface, result.BladeMaterial);
        Assert.Equal((int)SurfaceDefectType.Erosion, result.DefectType);
        Assert.Equal((int)SurfaceErosionSubtype.Flaking, result.DefectSubtype);
    }
    
    [Fact]
    public void MigrateLegacyType_DamagedOrMisaligned_MapsToVortexGenerators()
    {
        var result = _migrationService.MigrateLegacyType("Damaged or Misaligned");
        
        Assert.Equal(ComponentCategory.AuxiliaryComponent, result.Category);
        Assert.Equal(AuxiliaryComponent.VortexGenerators, result.AuxiliaryComponentType);
        Assert.Equal((int)VortexGeneratorDefectType.DamagedOrMisaligned, result.DefectType);
    }
    
    [Fact]
    public void MigrateLegacyType_Other_ReturnsDefault()
    {
        var result = _migrationService.MigrateLegacyType("Other");
        
        Assert.Equal(ComponentCategory.Blade, result.Category);
        Assert.Equal(BladeMaterial.Surface, result.BladeMaterial);
        Assert.Equal((int)SurfaceDefectType.Discoloration, result.DefectType);
    }
    
    [Fact]
    public void MigrateLegacyType_UnknownType_ReturnsDefault()
    {
        var result = _migrationService.MigrateLegacyType("UnknownDefectType");
        
        Assert.Equal(ComponentCategory.Blade, result.Category);
        Assert.NotNull(result.BladeMaterial);
    }
    
    [Fact]
    public void MigrateLegacyType_EmptyString_ReturnsDefault()
    {
        var result = _migrationService.MigrateLegacyType("");
        
        Assert.Equal(ComponentCategory.Blade, result.Category);
        Assert.NotNull(result.BladeMaterial);
    }
    
    [Fact]
    public void MigrateLegacyType_Null_ReturnsDefault()
    {
        var result = _migrationService.MigrateLegacyType(null!);
        
        Assert.Equal(ComponentCategory.Blade, result.Category);
        Assert.NotNull(result.BladeMaterial);
    }
    
    [Fact]
    public void MigrateLegacyType_PartialMatch_Crack_InfersCrack()
    {
        var result = _migrationService.MigrateLegacyType("Surface crack detected");
        
        Assert.Equal(ComponentCategory.Blade, result.Category);
        Assert.Equal(BladeMaterial.TopCoat, result.BladeMaterial);
        Assert.Equal((int)TopCoatDefectType.Crack, result.DefectType);
    }
    
    [Fact]
    public void MigrateLegacyType_PartialMatch_Erosion_InfersErosion()
    {
        var result = _migrationService.MigrateLegacyType("Leading edge erosion");
        
        Assert.Equal(ComponentCategory.Blade, result.Category);
        Assert.Equal(BladeMaterial.Surface, result.BladeMaterial);
        Assert.Equal((int)SurfaceDefectType.Erosion, result.DefectType);
    }
    
    [Fact]
    public void MigrateLegacyType_PartialMatch_Delamination_InfersDelamination()
    {
        var result = _migrationService.MigrateLegacyType("Delamination found");
        
        Assert.Equal(ComponentCategory.Blade, result.Category);
        Assert.Equal(BladeMaterial.Laminate, result.BladeMaterial);
        Assert.Equal((int)LaminateDefectType.Delamination, result.DefectType);
    }
    
    [Fact]
    public void ToLegacyString_KnownMapping_ReturnsOriginalLegacyName()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.TopCoat,
            DefectType = (int)TopCoatDefectType.Crack
        };
        
        var legacyString = _migrationService.ToLegacyString(classification);
        
        Assert.Equal("Crack", legacyString);
    }
    
    [Fact]
    public void ToLegacyString_UnknownMapping_ReturnsFullPath()
    {
        var classification = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Structure,
            DefectType = (int)StructureDefectType.Hole
        };
        
        var legacyString = _migrationService.ToLegacyString(classification);
        
        Assert.Contains("Structure", legacyString);
        Assert.Contains("Hole", legacyString);
    }
    
    [Fact]
    public void IsRecognizedLegacyType_KnownType_ReturnsTrue()
    {
        Assert.True(_migrationService.IsRecognizedLegacyType("Erosion"));
        Assert.True(_migrationService.IsRecognizedLegacyType("Crack"));
        Assert.True(_migrationService.IsRecognizedLegacyType("Pinholes"));
    }
    
    [Fact]
    public void IsRecognizedLegacyType_UnknownType_ReturnsFalse()
    {
        Assert.False(_migrationService.IsRecognizedLegacyType("SomethingNew"));
        Assert.False(_migrationService.IsRecognizedLegacyType(""));
        Assert.False(_migrationService.IsRecognizedLegacyType(null));
    }
    
    [Fact]
    public void GetLegacyTypeNames_ReturnsAllKnownTypes()
    {
        var legacyTypes = _migrationService.GetLegacyTypeNames().ToList();
        
        Assert.Contains("Erosion", legacyTypes);
        Assert.Contains("Crack", legacyTypes);
        Assert.Contains("Pinholes", legacyTypes);
        Assert.Contains("Flaking", legacyTypes);
        Assert.Contains("Discoloration", legacyTypes);
        Assert.Contains("Other", legacyTypes);
    }
}
