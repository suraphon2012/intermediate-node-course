# Wall Constraint Checker - Revit Add-in

A Revit add-in that checks whether any walls in your project extend beyond their constraint levels.

## Features

- Checks all walls in the active Revit document
- Detects walls that extend above their top constraint level
- Detects walls that extend below their base constraint level
- Detects walls with geometry that extends beyond their defined unconnected height
- Provides detailed violation report with wall IDs and extension amounts
- User-friendly dialog showing results

## What It Checks

The add-in analyzes walls for the following constraint violations:

1. **Above Top Constraint**: Walls that extend above their top constraint level (plus any top offset)
2. **Below Base Constraint**: Walls that extend below their base constraint level (plus any base offset)
3. **Beyond Unconnected Height**: Walls with unconnected height that have geometry extending beyond the defined height

## Requirements

- Autodesk Revit 2024 (can be modified for other versions)
- .NET Framework 4.8
- Visual Studio 2019 or later (for building the project)

## Installation

### Option 1: Build from Source

1. **Open the project in Visual Studio**
   - Navigate to `RevitAddin/WallConstraintChecker/`
   - Open `WallConstraintChecker.csproj`

2. **Update Revit API References (if needed)**
   - If you're using a different Revit version, update the references in the .csproj file:
     ```xml
     <HintPath>$(ProgramW6432)\Autodesk\Revit [YOUR_VERSION]\RevitAPI.dll</HintPath>
     <HintPath>$(ProgramW6432)\Autodesk\Revit [YOUR_VERSION]\RevitAPIUI.dll</HintPath>
     ```

3. **Build the solution**
   - Build > Build Solution (or press Ctrl+Shift+B)
   - The post-build event will automatically copy files to the Revit add-ins folder

### Option 2: Manual Installation

If you already have the compiled DLL:

1. Copy the `WallConstraintChecker.addin` file to:
   ```
   C:\ProgramData\Autodesk\Revit\Addins\2024\
   ```

2. Create a folder and copy the DLL:
   ```
   C:\ProgramData\Autodesk\Revit\Addins\2024\WallConstraintChecker\
   ```
   Copy `WallConstraintChecker.dll` to this folder

3. Restart Revit

## Usage

1. Open a Revit project that contains walls
2. Go to the **Add-Ins** tab in Revit
3. Click on **External Tools** dropdown
4. Select **Check Wall Constraints**
5. The add-in will analyze all walls and display a report

### Understanding the Results

**No Violations Found:**
- A dialog will confirm that all walls are within their constraint levels

**Violations Found:**
- Main dialog shows the count of violations
- Click to expand for detailed information including:
  - Wall Element ID
  - Base Constraint Level
  - Top Constraint Level
  - Type of violation
  - Extension amount (in feet)

## Example Output

```
Wall ID: 123456
  Base Constraint: Level 1
  Top Constraint: Level 2
  Issue: Wall extends above top constraint level
  Extension: 2.5 feet

Wall ID: 123457
  Base Constraint: Level 2
  Top Constraint: Level 3
  Issue: Wall extends below base constraint level
  Extension: 0.75 feet
```

## Troubleshooting

### Add-in doesn't appear in Revit

1. Check that the .addin file is in the correct location:
   - `C:\ProgramData\Autodesk\Revit\Addins\2024\` (for all users)
   - OR `%APPDATA%\Autodesk\Revit\Addins\2024\` (for current user)

2. Verify the .addin file has correct paths to the DLL

3. Check Revit version matches the add-in configuration

### Build Errors

- Ensure Revit API DLLs are referenced correctly
- Verify .NET Framework 4.8 is installed
- Check that Revit 2024 is installed (or update references for your version)

## Technical Details

### Architecture

- **CheckWallConstraintsCommand.cs**: Main command class implementing `IExternalCommand`
- **WallConstraintAnalyzer.cs**: Core logic for analyzing wall constraints
- **WallConstraintViolation**: Data class for storing violation information

### How It Works

1. Collects all Wall elements from the active document
2. For each wall:
   - Retrieves base and top constraint parameters
   - Gets actual wall geometry bounds
   - Compares actual elevations with constraint elevations
   - Records violations with a tolerance of 0.01 feet (~1/8 inch)
3. Presents results in a user-friendly dialog

## Customization

### Changing the Tolerance

Edit the `TOLERANCE` constant in `WallConstraintAnalyzer.cs`:

```csharp
private const double TOLERANCE = 0.01; // feet
```

### Adapting for Different Revit Versions

1. Update the .csproj file to reference your Revit version's API DLLs
2. Update the .addin file paths if necessary
3. Update the post-build event path to match your Revit version

## License

This project is provided as-is for educational and professional use.

## Support

For issues or questions, please refer to the project repository.
