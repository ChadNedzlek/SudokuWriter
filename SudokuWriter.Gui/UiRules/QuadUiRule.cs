using System.Collections.Immutable;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using VaettirNet.SudokuWriter.Library;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Gui.UiRules;

public class QuadUiRule : UiGameRuleFactory
{
    public override bool HasVariationDigits => true;

    private static readonly Typeface s_typeface = BuildTypeface();

    private static Typeface BuildTypeface() => new("Verdana");
    
    public QuadUiRule(int cellSize, string name) : base(cellSize, name, false)
    {
    }

    protected override bool TryStart(CellLocation location, RuleParameters parameters, out UiGameRule createdRule)
    {
        if (location.ColSide == 0 || location.RowSide == 0 || parameters.MultipleValues == 0)
        {
            createdRule = null;
            return false;
        }

        var mask = new CellValueMask(parameters.MultipleValues);
        createdRule = new Rule(this, CreateDrawing(location, mask), mask);
        return true;
    }

    protected override IEnumerable<UiGameRule> DeserializeCore(IGameRule rule)
    {
        if (rule is not QuadRule quadRule)
        {
            return [];
        }

        return quadRule.Quads.Select(
            q =>
            {
                var mask = (q.A | q.B | q.C | q.D);
                return new Rule(this, CreateDrawing(new CellLocation(q.Coord.Row, q.Coord.Col, 1, 1), mask), mask);
            }
        );
    }

    private Drawing CreateDrawing(CellLocation location, CellValueMask mask)
    {
        DrawingGroup group = new();
        Point p = TranslateToPoint(location);
        GeometryDrawing drawing = new()
        {
            Brush = Brushes.White,
            Pen = new Pen(Brushes.Black, 0.5),
            Geometry = new EllipseGeometry(p, CellSize / 3.5, CellSize / 3.5)
        };
        group.Children.Add(drawing);

        string list = mask.ToString();
        if (list.Length > 2)
        {
            list = list[..2] + '\n' + list[2..];
        }

        var txt = new FormattedText(
            list,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            s_typeface,
            CellSize / 5.0,
            Brushes.Black,
            1
        );
        
        var txtDrawing = new GeometryDrawing
        {
            Brush = Brushes.Black,
            Geometry = txt.BuildGeometry(p),
        };
        txtDrawing.Geometry.Transform = new TranslateTransform(txtDrawing.Bounds.Width / -2.0, (txtDrawing.Bounds.Height / -2.0) - CellSize / 20.0);
        group.Children.Add(txtDrawing);

        return group;
    }

    protected override IEnumerable<IGameRule> SerializeCore(IEnumerable<UiGameRule> uiRules)
    {
        ImmutableArray<QuadRule.Quad>.Builder quads = ImmutableArray.CreateBuilder<QuadRule.Quad>();
        foreach (Rule rule in uiRules.OfType<Rule>())
        {
            CellLocation location = TranslateFromPoint(rule.Drawing.Bounds.TopLeft);
            CellValueMask mask = rule.Mask;
            CellValue a = mask.GetMinValue();
            mask &= ~a.AsMask();
            CellValue b = mask.GetMinValue();
            mask &= ~b.AsMask();
            CellValue c = mask.GetMinValue();
            mask &= ~c.AsMask();
            CellValue d = mask.GetMinValue();
            quads.Add(new QuadRule.Quad(location.ToCoord(), a, b, c, d));
        }
        if (quads.Count == 0) return [];
        return [new QuadRule(quads.ToImmutable())];
    }
    
    private class Rule : UiGameRule
    {
        public CellValueMask Mask { get; }

        public Rule(UiGameRuleFactory factory, Drawing drawing, CellValueMask mask) : base(factory, drawing)
        {
            Mask = mask;
        }

        public override bool IsValid => true;
        
        public override bool TryAddSegment(CellLocation location, RuleParameters parameters)
        {
            return false;
        }

        public override bool TryContinue(CellLocation location)
        {
            return false;
        }

        public override void Complete()
        {
        }
    }
}