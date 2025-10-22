using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace WallConstraintChecker
{
    public class WallConstraintAnalyzer
    {
        private readonly Document _doc;
        private const double TOLERANCE = 0.01; // 0.01 feet tolerance (~1/8 inch)

        public WallConstraintAnalyzer(Document doc)
        {
            _doc = doc;
        }

        public List<WallConstraintViolation> CheckAllWalls(ICollection<Element> wallElements)
        {
            List<WallConstraintViolation> violations = new List<WallConstraintViolation>();

            foreach (Element element in wallElements)
            {
                Wall wall = element as Wall;
                if (wall == null) continue;

                WallConstraintViolation violation = CheckWallConstraints(wall);
                if (violation != null)
                {
                    violations.Add(violation);
                }
            }

            return violations;
        }

        private WallConstraintViolation CheckWallConstraints(Wall wall)
        {
            try
            {
                // Get wall parameters
                Parameter baseConstraintParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
                Parameter topConstraintParam = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                Parameter baseOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
                Parameter topOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
                Parameter unconnectedHeightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);

                if (baseConstraintParam == null)
                {
                    return null; // Can't check if no base constraint
                }

                // Get base constraint level
                ElementId baseConstraintId = baseConstraintParam.AsElementId();
                if (baseConstraintId == ElementId.InvalidElementId)
                {
                    return null;
                }

                Level baseLevel = _doc.GetElement(baseConstraintId) as Level;
                if (baseLevel == null)
                {
                    return null;
                }

                double baseOffset = baseOffsetParam?.AsDouble() ?? 0.0;
                double wallBaseElevation = baseLevel.Elevation + baseOffset;

                // Check top constraint
                if (topConstraintParam != null)
                {
                    ElementId topConstraintId = topConstraintParam.AsElementId();

                    // If wall is constrained to a level at the top
                    if (topConstraintId != ElementId.InvalidElementId)
                    {
                        Level topLevel = _doc.GetElement(topConstraintId) as Level;
                        if (topLevel != null)
                        {
                            double topOffset = topOffsetParam?.AsDouble() ?? 0.0;
                            double constraintTopElevation = topLevel.Elevation + topOffset;

                            // Get actual wall geometry to check real top
                            double actualTopElevation = GetWallActualTopElevation(wall);

                            // Check if wall extends beyond top constraint
                            if (actualTopElevation > constraintTopElevation + TOLERANCE)
                            {
                                return new WallConstraintViolation
                                {
                                    WallId = wall.Id,
                                    BaseConstraint = baseLevel.Name,
                                    TopConstraint = topLevel.Name,
                                    ViolationType = "Wall extends above top constraint level",
                                    ExtensionAmount = actualTopElevation - constraintTopElevation
                                };
                            }

                            // Check if wall extends below base constraint
                            double actualBottomElevation = GetWallActualBottomElevation(wall);
                            if (actualBottomElevation < wallBaseElevation - TOLERANCE)
                            {
                                return new WallConstraintViolation
                                {
                                    WallId = wall.Id,
                                    BaseConstraint = baseLevel.Name,
                                    TopConstraint = topLevel.Name,
                                    ViolationType = "Wall extends below base constraint level",
                                    ExtensionAmount = wallBaseElevation - actualBottomElevation
                                };
                            }
                        }
                    }
                    else if (unconnectedHeightParam != null)
                    {
                        // Wall has unconnected height
                        double unconnectedHeight = unconnectedHeightParam.AsDouble();
                        double expectedTopElevation = wallBaseElevation + unconnectedHeight;

                        double actualTopElevation = GetWallActualTopElevation(wall);

                        // Check if actual top differs from expected
                        if (Math.Abs(actualTopElevation - expectedTopElevation) > TOLERANCE)
                        {
                            if (actualTopElevation > expectedTopElevation)
                            {
                                return new WallConstraintViolation
                                {
                                    WallId = wall.Id,
                                    BaseConstraint = baseLevel.Name,
                                    TopConstraint = "Unconnected Height",
                                    ViolationType = "Wall geometry extends beyond defined height",
                                    ExtensionAmount = actualTopElevation - expectedTopElevation
                                };
                            }
                        }
                    }
                }

                return null; // No violation found
            }
            catch (Exception)
            {
                // Skip walls that can't be analyzed
                return null;
            }
        }

        private double GetWallActualTopElevation(Wall wall)
        {
            try
            {
                BoundingBoxXYZ bbox = wall.get_BoundingBox(null);
                if (bbox != null)
                {
                    return bbox.Max.Z;
                }

                // Fallback: use location curve and wall height
                LocationCurve locationCurve = wall.Location as LocationCurve;
                if (locationCurve != null)
                {
                    Parameter heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                    if (heightParam != null)
                    {
                        double height = heightParam.AsDouble();
                        Parameter baseOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
                        double baseOffset = baseOffsetParam?.AsDouble() ?? 0.0;

                        ElementId baseConstraintId = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsElementId();
                        Level baseLevel = _doc.GetElement(baseConstraintId) as Level;

                        if (baseLevel != null)
                        {
                            return baseLevel.Elevation + baseOffset + height;
                        }
                    }
                }
            }
            catch { }

            return 0.0;
        }

        private double GetWallActualBottomElevation(Wall wall)
        {
            try
            {
                BoundingBoxXYZ bbox = wall.get_BoundingBox(null);
                if (bbox != null)
                {
                    return bbox.Min.Z;
                }

                // Fallback: use base level and offset
                Parameter baseOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
                double baseOffset = baseOffsetParam?.AsDouble() ?? 0.0;

                ElementId baseConstraintId = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsElementId();
                Level baseLevel = _doc.GetElement(baseConstraintId) as Level;

                if (baseLevel != null)
                {
                    return baseLevel.Elevation + baseOffset;
                }
            }
            catch { }

            return 0.0;
        }
    }
}
