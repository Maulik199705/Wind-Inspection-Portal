using WindBladeInspector.Core.Entities;
using WindBladeInspector.Core.Enums;

namespace WindBladeInspector.Core.Services;

/// <summary>
/// Service for migrating legacy string-based defect types to the new hierarchical classification system.
/// Provides backward compatibility mapping.
/// </summary>
public class DefectMigrationService
{
    private static readonly Dictionary<string, DefectClassification> LegacyMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        // Legacy "Erosion" ? Blade > Surface > Erosion
        ["Erosion"] = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Erosion
        },
        
        // Legacy "Crack" ? Blade > TopCoat > Crack
        ["Crack"] = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.TopCoat,
            DefectType = (int)TopCoatDefectType.Crack
        },
        
        // Legacy "Pinholes" ? Blade > TopCoat > Pinholes
        ["Pinholes"] = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.TopCoat,
            DefectType = (int)TopCoatDefectType.Pinholes
        },
        
        // Legacy "Peeling" ? Blade > TopCoat > Crack (closest match)
        ["Peeling"] = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.TopCoat,
            DefectType = (int)TopCoatDefectType.Crack,
            DefectSubtype = (int)TopCoatCrackSubtype.None
        },
        
        // Legacy "Flaking" ? Blade > Surface > Erosion > Flaking
        ["Flaking"] = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Erosion,
            DefectSubtype = (int)SurfaceErosionSubtype.Flaking
        },
        
        // Legacy "Discoloration" ? Blade > Surface > Discoloration
        ["Discoloration"] = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Discoloration
        },
        
        // Legacy "Damaged or Misaligned" ? AuxiliaryComponent > VortexGenerators > DamagedOrMisaligned
        ["Damaged or Misaligned"] = new DefectClassification
        {
            Category = ComponentCategory.AuxiliaryComponent,
            AuxiliaryComponentType = AuxiliaryComponent.VortexGenerators,
            DefectType = (int)VortexGeneratorDefectType.DamagedOrMisaligned
        },
        
        // Legacy "Other" ? Blade > Surface > Discoloration (default fallback)
        ["Other"] = new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Discoloration
        }
    };
    
    /// <summary>
    /// Converts a legacy string-based defect type to the new hierarchical classification.
    /// </summary>
    public DefectClassification MigrateLegacyType(string legacyType)
    {
        if (string.IsNullOrWhiteSpace(legacyType))
        {
            return GetDefaultClassification();
        }
        
        if (LegacyMapping.TryGetValue(legacyType, out var classification))
        {
            // Return a copy to avoid modifying the static dictionary
            return new DefectClassification
            {
                Category = classification.Category,
                BladeMaterial = classification.BladeMaterial,
                AuxiliaryComponentType = classification.AuxiliaryComponentType,
                DefectType = classification.DefectType,
                DefectSubtype = classification.DefectSubtype
            };
        }
        
        // If no direct mapping found, try to infer from partial matches
        if (legacyType.Contains("crack", StringComparison.OrdinalIgnoreCase))
        {
            return new DefectClassification
            {
                Category = ComponentCategory.Blade,
                BladeMaterial = BladeMaterial.TopCoat,
                DefectType = (int)TopCoatDefectType.Crack
            };
        }
        
        if (legacyType.Contains("erosion", StringComparison.OrdinalIgnoreCase) || 
            legacyType.Contains("chip", StringComparison.OrdinalIgnoreCase))
        {
            return new DefectClassification
            {
                Category = ComponentCategory.Blade,
                BladeMaterial = BladeMaterial.Surface,
                DefectType = (int)SurfaceDefectType.Erosion
            };
        }
        
        if (legacyType.Contains("delamination", StringComparison.OrdinalIgnoreCase))
        {
            return new DefectClassification
            {
                Category = ComponentCategory.Blade,
                BladeMaterial = BladeMaterial.Laminate,
                DefectType = (int)LaminateDefectType.Delamination
            };
        }
        
        // Default fallback
        return GetDefaultClassification();
    }
    
    /// <summary>
    /// Converts the new classification back to a legacy string for display compatibility.
    /// </summary>
    public string ToLegacyString(DefectClassification classification)
    {
        // Try to find a reverse mapping
        foreach (var kvp in LegacyMapping)
        {
            if (kvp.Value.Category == classification.Category &&
                kvp.Value.BladeMaterial == classification.BladeMaterial &&
                kvp.Value.AuxiliaryComponentType == classification.AuxiliaryComponentType &&
                kvp.Value.DefectType == classification.DefectType)
            {
                return kvp.Key;
            }
        }
        
        // If no exact reverse mapping, return the full path
        return classification.GetFullPath();
    }
    
    /// <summary>
    /// Gets the default classification for unknown or invalid defect types.
    /// </summary>
    public DefectClassification GetDefaultClassification()
    {
        return new DefectClassification
        {
            Category = ComponentCategory.Blade,
            BladeMaterial = BladeMaterial.Surface,
            DefectType = (int)SurfaceDefectType.Discoloration,
            DefectSubtype = (int)SurfaceDiscolorationSubtype.Mechanical
        };
    }
    
    /// <summary>
    /// Gets all available legacy type names for backward compatibility.
    /// </summary>
    public IEnumerable<string> GetLegacyTypeNames()
    {
        return LegacyMapping.Keys;
    }
    
    /// <summary>
    /// Checks if a legacy type name is recognized.
    /// </summary>
    public bool IsRecognizedLegacyType(string legacyType)
    {
        return !string.IsNullOrWhiteSpace(legacyType) && 
               LegacyMapping.ContainsKey(legacyType);
    }
}
