using System.Collections.Immutable;
using System.Windows;
using System.Windows.Media;
using SudokuWriter.Library;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Gui.UiRules;

public abstract class LineUiRule<T> : UiGameRuleFactory where T : LineRule<T>, ILineRule<T>
{
    protected class Rule : UiGameRule
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
                Path.Segments.Add(new LineSegment(p, true));
            }

            return true;
        }

        public override void Continue(CellLocation loc)
        {
            if (!loc.IsCenter) return;
            
            Point p = Factory.TranslateToPoint(loc);
            Point endPoint = Path.Segments.LastOrDefault() is LineSegment seg ? seg.Point : Path.StartPoint;
            if (endPoint != p)
            {
                var endLocation = Factory.TranslateFromPoint(endPoint);
                if (Math.Abs(endLocation.Row - loc.Row) <= 1 && Math.Abs(endLocation.Col - loc.Col) <= 1)
                {
                    Path.Segments.Add(new LineSegment(p, true));
                }
            }
        }

        public override void Complete()
        {
        }
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
        foreach (var rule in rules.OfType<Rule>())
        {
            var line = ImmutableArray.CreateBuilder<LineRuleSegment>();
            var segment = ImmutableArray.CreateBuilder<GridCoord>();
            segment.Add(TranslateFromPoint(rule.Path.StartPoint).ToCoord());
            foreach (var pathSegment in rule.Path.Segments)
            {
                if (pathSegment is not LineSegment lineSeg) continue;
                
                if (!pathSegment.IsStroked)
                {
                    line.Add(new LineRuleSegment(segment.ToImmutable()));
                    segment.Clear();
                }
                
                segment.Add(TranslateFromPoint(lineSeg.Point).ToCoord());
            }
            line.Add(new LineRuleSegment(segment.ToImmutable()));
            ruleBuilder.Add(new BranchingRuleLine(line.ToImmutable()));
        }

        return [SerializeLineRule(ruleBuilder.ToImmutable())];
    }

    protected abstract T SerializeLineRule(ImmutableArray<BranchingRuleLine> ruleLines);

    protected override IEnumerable<UiGameRule> DeserializeCore(IGameRule rule)
    {
        if (rule is not T lineRule) return null;

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
                    continue;
                }

                if (restart)
                {
                    rule.TryAddSegment(location);
                    restart = false;
                    continue;
                }
                
                rule.Continue(location);
            }
        }

        return rule;
    }
}