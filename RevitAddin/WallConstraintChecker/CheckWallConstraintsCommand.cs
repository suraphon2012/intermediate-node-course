using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace WallConstraintChecker
{
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    public class CheckWallConstraintsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // Check if there's an active document
                if (doc == null)
                {
                    TaskDialog.Show("Error", "No active Revit document found.");
                    return Result.Failed;
                }

                // Get all walls in the project
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                ICollection<Element> walls = collector.OfClass(typeof(Wall)).ToElements();

                if (walls.Count == 0)
                {
                    TaskDialog.Show("Wall Constraint Checker", "No walls found in the project.");
                    return Result.Succeeded;
                }

                // Check walls for constraint violations
                WallConstraintAnalyzer analyzer = new WallConstraintAnalyzer(doc);
                List<WallConstraintViolation> violations = analyzer.CheckAllWalls(walls);

                // Display results
                DisplayResults(violations, walls.Count);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void DisplayResults(List<WallConstraintViolation> violations, int totalWalls)
        {
            if (violations.Count == 0)
            {
                TaskDialog.Show("Wall Constraint Checker",
                    $"All {totalWalls} walls are within their constraint levels.\n\n" +
                    "No violations found!");
            }
            else
            {
                string violationDetails = "The following walls extend beyond their constraint levels:\n\n";

                foreach (var violation in violations)
                {
                    violationDetails += $"Wall ID: {violation.WallId}\n";
                    violationDetails += $"  Base Constraint: {violation.BaseConstraint}\n";
                    violationDetails += $"  Top Constraint: {violation.TopConstraint}\n";
                    violationDetails += $"  Issue: {violation.ViolationType}\n";

                    if (violation.ExtensionAmount > 0)
                    {
                        violationDetails += $"  Extension: {Math.Round(violation.ExtensionAmount, 2)} feet\n";
                    }

                    violationDetails += "\n";
                }

                TaskDialog mainDialog = new TaskDialog("Wall Constraint Checker")
                {
                    MainInstruction = $"Found {violations.Count} wall constraint violation(s)",
                    MainContent = $"Out of {totalWalls} total walls, {violations.Count} extend beyond their constraint levels.",
                    ExpandedContent = violationDetails,
                    CommonButtons = TaskDialogCommonButtons.Ok
                };

                mainDialog.Show();
            }
        }
    }

    public class WallConstraintViolation
    {
        public ElementId WallId { get; set; }
        public string BaseConstraint { get; set; }
        public string TopConstraint { get; set; }
        public string ViolationType { get; set; }
        public double ExtensionAmount { get; set; }
    }
}
