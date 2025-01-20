using System.Collections.Immutable;
using System.Windows;
using System.Windows.Media;
using SudokuWriter.Library;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Gui.UiRules;

public class KropkiDotUiRule : UiGameRuleFactory
{
    public bool IsDouble { get; }

    public KropkiDotUiRule(int cellSize, string name, bool isDouble) : base(cellSize, name, false)
    {
        IsDouble = isDouble;
    }

    protected override bool TryStart(CellLocation location, out UiGameRule createdRule)
    {
        if (!IsOnEdge(location))
        {
            createdRule = null;
            return false;
        }

        var drawing = new GeometryDrawing
        {
            Brush = IsDouble ? new SolidColorBrush(Color.FromRgb(32,32,32)) : Brushes.White,
            Pen = new Pen
            {
                Brush = new SolidColorBrush(Color.FromRgb(32,32,32)),
                Thickness = 0.5,
            },
        };

        var grp = new GeometryGroup();
        drawing.Geometry = grp;
        createdRule = new Rule(this, drawing, grp);
        createdRule.TryAddSegment(location);
        return true;
    }

    private static bool IsOnEdge(CellLocation location)
    {
        var isLeftRight = location.ColSide != 0;
        var isTopBottom = location.RowSide != 0;
        var isValid = isLeftRight != isTopBottom;
        return isValid;
    }

    protected override IEnumerable<UiGameRule> DeserializeCore(IGameRule rule)
    {
        if (rule is not KropkiDotRule dotRule) return [];

        ImmutableArray<GridEdge> list = IsDouble ? dotRule.Doubles : dotRule.Sequential;
        Rule uiRule = null;
        foreach (GridEdge edge in list)
        {
            var (a,b) = edge.GetSides();
            CellLocation location = new CellLocation(a.Row, a.Col, b.Row - a.Row, b.Col - a.Col);
            if (uiRule is null)
            {
                TryStart(location, out var r);
                uiRule = (Rule)r;
            }
            else
            {
                uiRule.TryAddSegment(location);
            }
        }

        return [uiRule];
    }

    protected override IEnumerable<IGameRule> SerializeCore(IEnumerable<UiGameRule> rules)
    {
        // Both dots are handled by the same factory, so skip it for one of them
        if (IsDouble) return [];

        var doubles = ImmutableArray.CreateBuilder<GridEdge>();
        var sequence = ImmutableArray.CreateBuilder<GridEdge>();
        foreach (Rule dotRule in rules.OfType<Rule>())
        {
            ref var builder = ref dotRule.KropkiDotUiRule.IsDouble ? ref doubles : ref sequence; 
            foreach (var geometry in dotRule.GeometryGroup.Children.OfType<EllipseGeometry>())
            {
                builder.Add(GetGridEdge(geometry.Center));
            }
        }

        if (doubles.Count == 0 && sequence.Count == 0) return [];
        
        return [new KropkiDotRule(doubles.ToImmutable(), sequence.ToImmutable())];
    }

    private GridEdge GetGridEdge(Point dotCenter)
    {
        var location = TranslateFromPoint(dotCenter);
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

    private class Rule : UiGameRule
    {
        public KropkiDotUiRule KropkiDotUiRule { get; }
        public GeometryGroup GeometryGroup { get; }

        public Rule(KropkiDotUiRule factory, Drawing drawing, GeometryGroup geometryGroup) : base(factory, drawing)
        {
            KropkiDotUiRule = factory;
            GeometryGroup = geometryGroup;
        }

        public override bool IsValid => true;
        
        public override bool TryAddSegment(CellLocation location)
        {
            if (!IsOnEdge(location)) return false;
            
            GeometryGroup.Children.Add(new EllipseGeometry(Factory.TranslateToPoint(location), Factory.CellSize / 8, Factory.CellSize / 8));
            return true;
        }

        public override bool TryContinue(CellLocation location) => false;

        public override void Complete()
        {
        }
    }
}