using System.Collections.Immutable;
using System.Windows;
using System.Windows.Media;
using SudokuWriter.Library;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Gui.UiRules;

public abstract class LineUiRule<T> : UiGameRuleFactory where T : LineRule<T>, ILineRule<T>
{
    protected abstract class Rule : UiGameRule
    {
        public PathFigure Path { get; }

        protected Rule(UiGameRuleFactory factory, Drawing drawing, PathFigure path) : base(factory, drawing)
        {
            Path = path;
        }

        public override bool TryAddSegment(CellLocation location)
        {
            if (!Path.Segments.Any(p => p is LineSegment line && Factory.TranslateFromPoint(line.Point).Center == location) &&
                Factory.TranslateFromPoint(Path.StartPoint).Center != location.Center)
            {
                return false;
            }
            
            Point p = Factory.TranslateToPoint(location);
            Point endPoint = Path.Segments.LastOrDefault() is LineSegment seg ? seg.Point : Path.StartPoint;
            if (endPoint != p)
            {
                Path.Segments.Add(new LineSegment(p, false));
            }

            return true;
        }

        public override bool TryContinue(CellLocation loc)
        {
            if (!loc.IsCenter) return false;
            
            Point p = Factory.TranslateToPoint(loc);
            Point endPoint = Path.Segments.LastOrDefault() is LineSegment seg ? seg.Point : Path.StartPoint;
            if (endPoint != p)
            {
                var endLocation = Factory.TranslateFromPoint(endPoint);
                if (Math.Abs(endLocation.Row - loc.Row) <= 1 && Math.Abs(endLocation.Col - loc.Col) <= 1)
                {
                    Path.Segments.Add(new LineSegment(p, true));
                    return true;
                }
            }

            return false;
        }

        public override void Complete()
        {
        }

        public override bool IsValid => Path.Segments.Count > 0;
    }

    protected Brush LineBrush { get; }

    public LineUiRule(int cellSize, string name, Brush lineBrush) : base(cellSize, name, true)
    {
        LineBrush = lineBrush;
    }

    protected override bool TryStart(CellLocation location, out UiGameRule createdRule)
    {
        var drawing = new GeometryDrawing
        {
            Pen = new Pen
            {
                Brush = LineBrush,
                Thickness = 5,
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
            }
        };
        

        var fig = new PathFigure(TranslateToPoint(location.Center), [], false) { IsFilled = false };
        drawing.Geometry = new PathGeometry([fig]);
        createdRule = CreateRule(drawing, fig);
        return true;
    }

    protected abstract Rule CreateRule(GeometryDrawing drawing, PathFigure figure);

    protected override IEnumerable<IGameRule> SerializeCore(IEnumerable<UiGameRule> rules)
    {
        var ruleBuilder = ImmutableArray.CreateBuilder<BranchingRuleLine>();
        foreach (var rule in rules.OfType<Rule>().Where(r => r.IsValid))
        {
            var line = ImmutableArray.CreateBuilder<LineRuleSegment>();
            var segment = ImmutableArray.CreateBuilder<GridCoord>();
            segment.Add(TranslateFromPoint(rule.Path.StartPoint).ToCoord());
            foreach (var pathSegment in rule.Path.Segments)
            {
                if (pathSegment is not LineSegment lineSeg) continue;
                
                if (!pathSegment.IsStroked && segment.Count != 0)
                {
                    line.Add(new LineRuleSegment(segment.ToImmutable()));
                    segment.Clear();
                }
                
                segment.Add(TranslateFromPoint(lineSeg.Point).ToCoord());
            }
            line.Add(new LineRuleSegment(segment.ToImmutable()));
            ruleBuilder.Add(new BranchingRuleLine(line.ToImmutable()));
        }

        if (ruleBuilder.Count == 0) return [];

        return [SerializeLineRule(ruleBuilder.ToImmutable())];
    }

    protected abstract T SerializeLineRule(ImmutableArray<BranchingRuleLine> ruleLines);

    protected override IEnumerable<UiGameRule> DeserializeCore(IGameRule rule)
    {
        if (rule is not T lineRule) return [];

        return lineRule.Lines.Select(CreateUiRule).Where(x => x is not null);
    }

    private UiGameRule CreateUiRule(BranchingRuleLine line)
    {
        UiGameRule rule = null;
        foreach (LineRuleSegment branch in line.Branches)
        {
            bool restart = true;
            foreach (CellLocation location in branch.Cells.Select(cell => new CellLocation(cell.Row, cell.Col)))
            {
                if (rule is null)
                {
                    TryStart(location, out rule);
                    restart = false;
                    continue;
                }

                if (restart)
                {
                    rule.TryAddSegment(location);
                    restart = false;
                    continue;
                }
                
                rule.TryContinue(location);
            }
        }

        return rule;
    }
}