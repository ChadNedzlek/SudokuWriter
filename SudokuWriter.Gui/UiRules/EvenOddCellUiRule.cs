using System.Collections.Immutable;
using System.Windows;
using System.Windows.Media;
using SudokuWriter.Library;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Gui.UiRules;

public class EvenOddCellUiRule : UiGameRuleFactory
{
    public bool IsEven { get; }
    
    public EvenOddCellUiRule(int cellSize, string name, bool isEven) : base(cellSize, name, false)
    {
        IsEven = isEven;
    }

    protected override bool TryStart(CellLocation location, RuleParameters _,  out UiGameRule createdRule)
    {
        var drawing = new GeometryDrawing
        {
            Brush = Brushes.LightGray,
        };

        var grp = new GeometryGroup();
        drawing.Geometry = grp;
        grp.Children.Add(CreateGeometry(location));
        createdRule = new Rule(this, drawing, grp);
        return true;
    }

    private Geometry CreateGeometry(CellLocation location)
    {
        Point point = TranslateToPoint(location.Center);
        return !IsEven
            ? new EllipseGeometry(new(point.X, point.Y), CellSize / 3, CellSize / 3)
            : new RectangleGeometry(new Rect(point.X - CellSize / 3, point.Y - CellSize / 3, 2 * CellSize / 3, 2 * CellSize / 3));
    }

    protected override IEnumerable<UiGameRule> DeserializeCore(IGameRule rule)
    {
        if (rule is not ParityCellRule parity) return [];

        ImmutableArray<GridCoord> cells = IsEven ? parity.EvenCells : parity.OddCells;

        if (cells.IsEmpty) return [];

        var drawingGroup = new GeometryDrawing();
        var geometryGroup = new GeometryGroup();
        drawingGroup.Geometry = geometryGroup;
        Rule uiRule = new Rule(this, drawingGroup, geometryGroup);
        foreach (GridCoord coord in cells)
        {
            uiRule.TryAddSegment(new CellLocation(coord.Row, coord.Col), default);
        }

        return [uiRule];
    }

    protected override IEnumerable<IGameRule> SerializeCore(IEnumerable<UiGameRule> rules)
    {
        // Odd serializes both even and odd, so just skip the evens
        if (IsEven) return [];
        
        ImmutableArray<GridCoord>.Builder even = ImmutableArray.CreateBuilder<GridCoord>();
        ImmutableArray<GridCoord>.Builder odd = ImmutableArray.CreateBuilder<GridCoord>();
        foreach (Rule rule in rules.OfType<Rule>())
        {
            ref ImmutableArray<GridCoord>.Builder list = ref rule.IsEven ? ref even : ref odd;
            foreach (Geometry geo in rule.GeometryGroup.Children)
            {
                list.Add(TranslateFromPoint(geo switch
                {
                    EllipseGeometry e => e.Center,
                    RectangleGeometry r => r.Bounds.Location,
                }).ToCoord());
            }
        }

        if (even.Count == 0 && odd.Count == 0) return [];

        return [new ParityCellRule(even.ToImmutable(), odd.ToImmutable())];
    }

    private class Rule : UiGameRule
    {
        private readonly EvenOddCellUiRule _factory;
        public readonly GeometryGroup GeometryGroup;
        public bool IsEven => _factory.IsEven;

        public Rule(EvenOddCellUiRule factory, Drawing drawing, GeometryGroup geometryGroup) : base(factory, drawing)
        {
            _factory = factory;
            GeometryGroup = geometryGroup;
        }

        public override bool IsValid => true;
        
        public override bool TryAddSegment(CellLocation location, RuleParameters _)
        {
            GeometryGroup.Children.Add(_factory.CreateGeometry(location));
            return true;
        }

        public override bool TryContinue(CellLocation location) => false;

        public override void Complete()
        {
        }
    }
}