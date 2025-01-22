using System.Collections.Immutable;
using System.Windows;
using System.Windows.Media;
using SudokuWriter.Library;

namespace SudokuWriter.Gui.UiRules;

public abstract class EdgeUiRuleFactoryBase<T> : UiGameRuleFactory where T : IGameRule
{
    public EdgeUiRuleFactoryBase(int cellSize, string name, bool isContinuous) : base(cellSize, name, isContinuous)
    {
    }

    protected override bool TryStart(CellLocation location, RuleParameters parameters, out UiGameRule createdRule)
    {
        if (!IsOnEdge(location))
        {
            createdRule = null;
            return false;
        }

        GeometryDrawing drawing = CreateInitialDrawing();

        var grp = new GeometryGroup();
        drawing.Geometry = grp;
        createdRule = CreateRule(parameters, drawing, grp);
        createdRule.TryAddSegment(location, parameters);
        return true;
    }

    protected abstract RuleBase CreateRule(RuleParameters parameters, GeometryDrawing drawing, GeometryGroup grp);

    protected abstract GeometryDrawing CreateInitialDrawing();

    protected static bool IsOnEdge(CellLocation location)
    {
        bool isLeftRight = location.ColSide != 0;
        bool isTopBottom = location.RowSide != 0;
        bool isValid = isLeftRight != isTopBottom;
        return isValid;
    }

    protected GridEdge GetGridEdge(Point dotCenter)
    {
        CellLocation location = TranslateFromPoint(dotCenter);
        if (location.ColSide < 0)
        {
            location = location with { ColSide = 1, Col = location.Col - 1 };
        }
        
        if (location.RowSide < 0)
        {
            location = location with { RowSide = 1, Row = location.Row - 1 };
        }

        return location.RowSide != 0 ? GridEdge.BottomOf(location.ToCoord()) : GridEdge.RightOf(location.ToCoord());
    }

    protected abstract ImmutableArray<GridEdge> GetEdgesFromRule(T rule);

    protected override IEnumerable<UiGameRule> DeserializeCore(IGameRule rule)
    {
        if (rule is not T edgeRule) return [];

        ImmutableArray<GridEdge> list = GetEdgesFromRule(edgeRule);
        UiGameRule uiRule = null;
        foreach (GridEdge edge in list)
        {
            CellLocation location = GetLocation(edge);
            if (uiRule is null)
            {
                TryStart(location, default, out uiRule);
            }
            else
            {
                uiRule.TryAddSegment(location, default);
            }
        }

        return [uiRule];
    }

    protected static CellLocation GetLocation(GridEdge edge)
    {
        (GridCoord a, GridCoord b) = edge.GetSides();
        CellLocation location = new CellLocation(a.Row, a.Col, b.Row - a.Row, b.Col - a.Col);
        return location;
    }

    protected abstract class RuleBase : UiGameRule
    {
        public GeometryGroup GeometryGroup { get; }

        public RuleBase(UiGameRuleFactory factory, Drawing drawing, GeometryGroup geometryGroup) : base(factory, drawing)
        {
            GeometryGroup = geometryGroup;
        }

        public override bool IsValid => true;
        
        public override bool TryAddSegment(CellLocation location, RuleParameters _)
        {
            if (!IsOnEdge(location)) return false;
            
            GeometryGroup.Children.Add(BuildEdgeDisplayGeometry(location));
            return true;
        }

        protected abstract Geometry BuildEdgeDisplayGeometry(CellLocation location);

        public override bool TryContinue(CellLocation location) => false;

        public override void Complete()
        {
        }
    }
}