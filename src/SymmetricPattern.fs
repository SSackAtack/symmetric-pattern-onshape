FeatureScript 2945;
import(path : "onshape/std/common.fs", version : "2945.0");

// ============================================================
// Symmetric Pattern -- symmetric pattern tool for Onshape
// Version: 1.0 (MVP)
// ============================================================

// --- Enums ---


export enum GridMode
{
    annotation { "Name" : "Linear" }
    LINEAR,
    annotation { "Name" : "Grid" }
    GRID
}

export enum StaggerMode
{
    annotation { "Name" : "None" }
    NONE,
    annotation { "Name" : "50%" }
    HALF,
    annotation { "Name" : "Custom" }
    CUSTOM
}

export enum AlignmentMode
{
    annotation { "Name" : "Skip" }
    SKIP
    // TRIM (boolean cut) -- planned for Phase 2
}

export enum TargetType
{
    annotation { "Name" : "Body" }
    BODY,
    annotation { "Name" : "Faces" }
    FACES
}

// --- Parameter bounds ---

export const COLUMN_COUNT_BOUNDS =
{
    (unitless) : [2, 3, 500]
} as IntegerBoundSpec;

export const ROW_COUNT_BOUNDS =
{
    (unitless) : [2, 2, 100]
} as IntegerBoundSpec;

export const STAGGER_OFFSET_BOUNDS =
{
    (unitless) : [1, 50, 99]
} as RealBoundSpec;

export const REDUCE_COUNT_BOUNDS =
{
    (unitless) : [1, 1, 499]
} as IntegerBoundSpec;

// --- Feature definition ---

annotation { "Feature Type Name" : "Symmetric Pattern",
             "Feature Type Description" : "Symmetric linear and grid pattern for bodies or faces using boundary vertices" }
export const symmetricPattern = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        // === Target type ===
        annotation { "Name" : "Pattern type",
                     "UIHint" : UIHint.HORIZONTAL_ENUM }
        definition.targetType is TargetType;

        if (definition.targetType == TargetType.BODY)
        {
            annotation { "Name" : "Target body",
                         "Filter" : EntityType.BODY,
                         "MaxNumberOfPicks" : 1 }
            definition.targetBody is Query;
        }
        else
        {
            annotation { "Name" : "Target faces",
                         "Filter" : EntityType.FACE && SketchObject.NO }
            definition.targetFaces is Query;
        }

        // === Boundary (two points) ===
        annotation { "Name" : "Start point",
                     "Filter" : EntityType.VERTEX,
                     "MaxNumberOfPicks" : 1 }
        definition.startPoint is Query;

        annotation { "Name" : "End point",
                     "Filter" : EntityType.VERTEX,
                     "MaxNumberOfPicks" : 1 }
        definition.endPoint is Query;

        // === Grid layout ===
        annotation { "Name" : "Columns" }
        isInteger(definition.columnCount, COLUMN_COUNT_BOUNDS);

        annotation { "Name" : "Mode",
                     "UIHint" : UIHint.HORIZONTAL_ENUM }
        definition.gridMode is GridMode;

        if (definition.gridMode == GridMode.GRID)
        {
            annotation { "Name" : "Rows" }
            isInteger(definition.rowCount, ROW_COUNT_BOUNDS);

            annotation { "Name" : "Row end point",
                         "Filter" : EntityType.VERTEX,
                         "MaxNumberOfPicks" : 1 }
            definition.gridRowEndPoint is Query;

            // === Stagger ===
            annotation { "Name" : "Stagger",
                         "UIHint" : UIHint.HORIZONTAL_ENUM }
            definition.staggerMode is StaggerMode;

            if (definition.staggerMode == StaggerMode.CUSTOM)
            {
                annotation { "Name" : "Offset (%)" }
                isReal(definition.customOffset, STAGGER_OFFSET_BOUNDS);
            }

            if (definition.staggerMode != StaggerMode.NONE)
            {
                annotation { "Name" : "Reduce staggered rows" }
                definition.reduceStaggeredRows is boolean;

                if (definition.reduceStaggeredRows)
                {
                    annotation { "Name" : "Reduce by" }
                    isInteger(definition.staggeredRowReduction, REDUCE_COUNT_BOUNDS);
                }

                annotation { "Name" : "Edge mode",
                             "UIHint" : UIHint.HORIZONTAL_ENUM }
                definition.alignment is AlignmentMode;
            }
        }
    }
    {
        var startPos = getSingleVertexPoint(context, definition.startPoint, "Start point");
        var endPos = getSingleVertexPoint(context, definition.endPoint, "End point");

        // ============================================================
        // 2. COMPUTE DIRECTIONS AND SPACING
        // ============================================================

        var mainVec = endPos - startPos;
        var totalLength = norm(mainVec);

        // Validation: start and end must be different
        if ((totalLength / meter) < 1e-8)
        {
            throw regenError("Start and End points are too close together.");
        }

        var dirMain = normalize(mainVec);

        // --- Measure original entity position for symmetric margins ---
        var origBox;
        if (definition.targetType == TargetType.BODY)
        {
            origBox = evBox3d(context, {
                "topology" : definition.targetBody
            });
        }
        else
        {
            origBox = evBox3d(context, {
                "topology" : definition.targetFaces
            });
        }
        var origCentroid = (origBox.minCorner + origBox.maxCorner) / 2;

        // Distance from Start to original along main direction
        var originOffset = dot(origCentroid - startPos, dirMain);

        // Symmetric span: mirror the original's margin to End side
        // Last element at totalLength - originOffset from Start
        // Usable span = (totalLength - originOffset) - originOffset = totalLength - 2*originOffset
        var symmetricSpan = totalLength - 2 * originOffset;

        if ((symmetricSpan / meter) < 1e-6)
        {
            throw regenError("Original is beyond the midpoint of Start-End range.");
        }

        // Column spacing: distribute N elements (including original) across symmetricSpan
        var colSpacing = 0 * meter;
        if (definition.columnCount > 1)
        {
            colSpacing = symmetricSpan / (definition.columnCount - 1);
        }

        // Determine row count and spacing
        var rowCount = 1;
        var dirRow = vector(0, 0, 0);
        var rowSpacing = 0 * meter;
        var staggerMode = StaggerMode.NONE;
        var staggerFraction = 0.0;
        var alignment = AlignmentMode.SKIP;
        var reduceStaggeredRows = false;
        var staggeredRowReduction = 0;

        if (definition.gridMode == GridMode.GRID)
        {
            rowCount = definition.rowCount;

            if (size(evaluateQuery(context, definition.gridRowEndPoint)) < 1)
            {
                reportFeatureWarning(context, id, "Select Row end point to create grid rows.");
                return;
            }

            var rowEndPos = getSingleVertexPoint(context, definition.gridRowEndPoint, "Row end point");

            var rowGuide = rowEndPos - startPos;
            var rowVec = rowGuide - dirMain * dot(rowGuide, dirMain);
            var rowTotalLength = norm(rowVec);
            if ((rowTotalLength / meter) < 1e-8)
            {
                throw regenError("Row end point must not be on the Start-End line.");
            }

            dirRow = normalize(rowVec);

            var rowOriginOffset = dot(origCentroid - startPos, dirRow);
            var rowUsableSpan = rowTotalLength - 2 * rowOriginOffset;

            if ((rowUsableSpan / meter) < 1e-6)
            {
                throw regenError("Original is beyond the midpoint of the row range.");
            }

            if (definition.rowCount > 1)
            {
                rowSpacing = rowUsableSpan / (definition.rowCount - 1);
            }
            staggerMode = definition.staggerMode;

            if (staggerMode == StaggerMode.HALF)
            {
                staggerFraction = 0.5;
            }
            else if (staggerMode == StaggerMode.CUSTOM)
            {
                staggerFraction = definition.customOffset / 100.0;
            }

            if (staggerMode != StaggerMode.NONE)
            {
                reduceStaggeredRows = definition.reduceStaggeredRows;
                if (reduceStaggeredRows)
                {
                    staggeredRowReduction = definition.staggeredRowReduction;
                }

                alignment = definition.alignment;
            }
        }

        // ============================================================
        // 3. GENERATE TRANSFORMS
        // ============================================================

        var transforms = [];

        for (var i = 0; i < rowCount; i += 1)
        {
            // Compute stagger offset for odd rows
            var isStaggeredRow = (i % 2 != 0 && staggerMode != StaggerMode.NONE);
            var staggerOffset = 0 * meter;
            if (isStaggeredRow)
            {
                staggerOffset = colSpacing * staggerFraction;
            }

            var currentColumnCount = definition.columnCount;
            if (isStaggeredRow && reduceStaggeredRows)
            {
                currentColumnCount = definition.columnCount - staggeredRowReduction;
                if (currentColumnCount < 1)
                {
                    currentColumnCount = 1;
                }
            }

            for (var j = 0; j < currentColumnCount; j += 1)
            {
                // Row 0: skip j=0 (that's the original entity)
                // Row > 0: include j=0 (copy below original)
                if (i == 0 && j == 0)
                {
                    continue;
                }

                // Offset from original along main direction
                var mainOffset = j * colSpacing + staggerOffset;

                // SKIP mode: skip elements beyond boundary
                if (staggerMode != StaggerMode.NONE && alignment == AlignmentMode.SKIP)
                {
                    // Check if absolute position is outside Start-End range
                    var absPos = originOffset + mainOffset;
                    if ((absPos / meter) > (totalLength / meter) + 1e-8 ||
                        (absPos / meter) < -1e-8)
                    {
                        continue;
                    }
                }

                // Position along row direction
                var rowOffset = i * rowSpacing;

                // Create translation transform (relative to original)
                var translation = dirMain * mainOffset + dirRow * rowOffset;
                transforms = append(transforms, transform(translation));
            }
        }

        // ============================================================
        // 4. EXECUTE PATTERN
        // ============================================================

        if (size(transforms) > 0)
        {
            // Generate unique instance names
            var instanceNames = [];
            for (var idx = 0; idx < size(transforms); idx += 1)
            {
                instanceNames = append(instanceNames, "instance_" ~ toString(idx));
            }

            // Select entities based on pattern type
            var patternEntities;
            if (definition.targetType == TargetType.BODY)
            {
                patternEntities = definition.targetBody;
            }
            else
            {
                patternEntities = definition.targetFaces;
            }

            opPattern(context, id + "symmetricPattern", {
                "entities" : patternEntities,
                "transforms" : transforms,
                "instanceNames" : instanceNames
            });
        }
        else
        {
            reportFeatureWarning(context, id, "No copies created. Check parameters.");
        }
    });

// ============================================================
// Helper functions
// ============================================================

function getSingleVertexPoint(context is Context, vertexQuery is Query, parameterName is string) returns Vector
{
    var resolvedVertices = evaluateQuery(context, vertexQuery);
    if (size(resolvedVertices) < 1)
    {
        throw regenError(parameterName ~ " does not resolve to a vertex.");
    }

    return evVertexPoint(context, {
        "vertex" : qNthElement(vertexQuery, 0)
    });
}
