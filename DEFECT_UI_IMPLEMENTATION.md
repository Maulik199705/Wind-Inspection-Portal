# Defect Classification UI Implementation

## Overview
This document describes the implementation of the hierarchical defect classification system in the Wind Blade Inspector application's user interface.

## What Was Changed

### 1. New Component: `DefectClassificationSelector.razor`
**Location:** `WindBladeInspector.Web/Components/Inspection/DefectClassificationSelector.razor`

This is a reusable Blazor component that replaces the simple "Type" dropdown with a hierarchical classification selector.

**Features:**
- Cascading dropdowns that update based on parent selections
- Category ? Material/Component ? Defect Type ? Subtype hierarchy
- Real-time validation with error messages
- Live preview of the full classification path
- Smart hiding/showing of relevant fields based on selection

**Usage:**
```razor
<DefectClassificationSelector 
    Classification="@anomaly.Classification" 
    ClassificationChanged="@OnClassificationChanged" />
```

### 2. Updated `Inspection.razor` Page

**Changed Sections:**

#### A. Defect Entry Form (Current Anomaly)
- Replaced simple `Type` dropdown with `DefectClassificationSelector`
- Added callback method `OnCurrentDefectClassificationChanged`
- Added validation to ensure classification is set before saving

#### B. Defect Editor Form (Selected Anomaly)
- Replaced simple `Type` dropdown with `DefectClassificationSelector`
- Added callback method `OnSelectedDefectClassificationChanged`
- Maintains backward compatibility with legacy `Type` field

#### C. Defect List Sidebar
- Updated to display defect type using `GetDefectTypeDisplay()`
- Shows shortened version in list item
- Full classification path shown in tooltip on hover

#### D. Report Generation
- Updated to use `GetDefectTypeDisplay()` for full classification path
- Executive summary table now shows complete classification
- Detailed defect analysis shows hierarchical classification

### 3. New Methods Added

```csharp
private void OnCurrentDefectClassificationChanged(DefectClassification? classification)
{
    // Updates current anomaly classification and legacy Type field
}

private void OnSelectedDefectClassificationChanged(DefectClassification? classification)
{
    // Updates selected anomaly classification and legacy Type field
}

private string GetShortDefectDisplay(Anomaly anomaly)
{
    // Returns shortened defect display for sidebar list
}
```

## How to Use the New Defect Classification UI

### Creating a New Defect

1. **Draw a bounding box** on the image
2. The defect form appears with the new classification selector
3. **Select Severity** (1-5)
4. **Select Category:**
   - Blade
   - Auxiliary Component

5. **If Blade is selected:**
   - Select Material: Surface, Top Coat, Laminate, Structure, or Through
   - Select Defect Type (options update based on material)
   - Select Defect Subtype (if applicable - only shown when subtypes exist)

6. **If Auxiliary Component is selected:**
   - Select Component: Hub, Nozzle, Vortex Generators, etc.
   - Select Defect Type (options update based on component)

7. **Select Blade Side** (or leave as Auto)
8. **Click "Save Defect"**

### Classification Preview
As you make selections, a live preview shows the full classification path:
```
Blade > Surface > Erosion > Chip
```

### Validation
The component validates your selections in real-time:
- ? Valid combinations are accepted
- ?? Invalid combinations show error messages
- ?? Save button is disabled until valid classification is selected

## Examples of Valid Classifications

### Blade Defects

1. **Surface Erosion with Chip:**
   - Category: Blade
   - Material: Surface
   - Defect Type: Erosion
   - Subtype: Chip
   - **Result:** `Blade > Surface > Erosion > Chip`

2. **Top Coat Crack - Fatigue:**
   - Category: Blade
   - Material: Top Coat
   - Defect Type: Crack
   - Subtype: Fatigue Cracks
   - **Result:** `Blade > Top Coat > Crack > Fatigue Cracks`

3. **Structure Crack - Transverse:**
   - Category: Blade
   - Material: Structure
   - Defect Type: Crack
   - Subtype: Transverse
   - **Result:** `Blade > Structure > Crack > Transverse`

### Auxiliary Component Defects

1. **Vortex Generator Damaged:**
   - Category: Auxiliary Component
   - Component: Vortex Generators
   - Defect Type: Damaged Or Misaligned
   - **Result:** `Auxiliary Component > Vortex Generators > Damaged Or Misaligned`

2. **Lightning Receptor Missing:**
   - Category: Auxiliary Component
   - Component: Lightning Receptors
   - Defect Type: Missing
   - **Result:** `Auxiliary Component > Lightning Receptors > Missing`

## Backward Compatibility

The system maintains full backward compatibility:

1. **Legacy `Type` field** is automatically populated from the classification
2. **Existing defects** without classification continue to work
3. **Reports** show the full classification path for new defects, legacy type for old ones
4. **Migration service** can convert legacy types to new classifications when needed

## UI Enhancements

### Defect List Sidebar
- Shows shortened type name (e.g., "Erosion - Chip")
- Hover tooltip shows full path
- Color-coded severity dots

### Report Generation
- Executive summary table includes full classification
- Detailed analysis shows complete hierarchical path
- Professional formatting for print/PDF export

## Validation Rules

The component enforces the following rules:

1. **Category Selection:**
   - Must select either Blade or Auxiliary Component

2. **Blade Defects:**
   - Material is required
   - Defect Type must be valid for the selected material
   - Subtype must be valid for the selected defect type (if applicable)

3. **Auxiliary Component Defects:**
   - Component type is required
   - Defect Type must be valid for the selected component
   - No subtypes allowed for auxiliary components

4. **Mutual Exclusion:**
   - Cannot have both BladeMaterial and AuxiliaryComponentType set
   - Component enforces this automatically

## Troubleshooting

### Defect won't save
- Ensure all required fields are selected (shown in red outline)
- Check for validation error messages below the classification selector
- Verify severity is selected

### Dropdown options not updating
- This is by design - options cascade based on previous selections
- Change parent selections to see updated child options

### Old defects show different format
- Legacy defects display their original Type field
- New defects show the full hierarchical classification
- Both formats are supported in reports

## Technical Details

### Services Used
- `DefectClassificationBuilderService` - Provides dropdown options
- `DefectClassificationValidator` - Validates selections
- `DefectMigrationService` - Converts legacy types (if needed)

### State Management
- Classification stored in `Anomaly.Classification` property
- Legacy `Type` field synchronized for backward compatibility
- All changes logged for debugging

### Performance
- Dropdown options are generated on-demand
- Validation runs in real-time without blocking UI
- Efficient enum-based lookups

## Future Enhancements

Potential improvements:
1. Search/filter in dropdowns for large option lists
2. Recent selections quick-access
3. Bulk classification update for multiple defects
4. Classification templates/favorites
5. AI-suggested classifications based on image analysis

---

**Note:** All changes maintain strict adherence to the hierarchical defect classification schema defined in `DEFECT_CLASSIFICATION_GUIDE.md`.
