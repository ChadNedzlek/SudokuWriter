using System.Globalization;
using System.Windows;
using System.Windows.Media;
using SudokuWriter.Library;

namespace SudokuWriter.Gui.UiRules;

public class PairSumUiRule : UiGameRuleFactory
{
    public override Range? VariationRange => 3..17;

    public PairSumUiRule(int cellSize, string name) : base(cellSize, name, false)
    {
    }

    private static string ToRomanNumeral(uint value) =>
        value switch
        {
            >= 1000 => "M" + ToRomanNumeral(value - 1000),
            >= 900 => "C" + ToRomanNumeral(value + 100),
            >= 500 => "D" + ToRomanNumeral(value - 500),
            >= 100 => "C" + ToRomanNumeral(value - 100),
            >= 90 => "X" + ToRomanNumeral(value + 10),
            >= 50 => "L" + ToRomanNumeral(value - 50),
            >= 10 => "X" + ToRomanNumeral(value - 10),
            >= 9 => "I" + ToRomanNumeral(value + 1),
            >= 5 => "V" + ToRomanNumeral(value - 5),
            4 => "IV",
            0 => "",
            _ => "I" + ToRomanNumeral(value - 1),
        };

    protected override bool TryStart(CellLocation location, RuleParameters parameters, out UiGameRule createdRule)
    {
        if (!IsOnEdge(location))
        {
            createdRule = null;
            return false;
        }

        var drawing = new GeometryDrawing
        {
            Brush = Brushes.Black,
        };
        

        var grp = new GeometryGroup();
        drawing.Geometry = grp;
        createdRule = new Rule(this, drawing, grp, parameters.SingleValue);
        createdRule.TryAddSegment(location, parameters);
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
        return [];
    }

    protected override IEnumerable<IGameRule> SerializeCore(IEnumerable<UiGameRule> rules)
    {
        return [];
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
        public ushort Sum { get; }
        public PairSumUiRule PairSumUiRule { get; }
        public GeometryGroup GeometryGroup { get; }

        public Rule(PairSumUiRule factory, Drawing drawing, GeometryGroup geometryGroup, ushort sum) : base(factory, drawing)
        {
            PairSumUiRule = factory;
            GeometryGroup = geometryGroup;
            Sum = sum;
        }

        public override bool IsValid => true;
        
        public override bool TryAddSegment(CellLocation location, RuleParameters parameters)
        {
            if (!IsOnEdge(location)) return false;

            Typeface verdanaDefault = new Typeface("Verdana");
            Typeface typeface = new Typeface(verdanaDefault.FontFamily, verdanaDefault.Style, FontWeights.Bold, verdanaDefault.Stretch, null);

            FormattedText txt = new FormattedText(ToRomanNumeral(parameters.SingleValue),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                Factory.CellSize / 3.0,
                Brushes.Black,
                1);
            Geometry textGeo = txt.BuildGeometry(Factory.TranslateToPoint(location));
            textGeo.Transform = new TranslateTransform(-textGeo.Bounds.Width / 2, -textGeo.Bounds.Height / 2);
            GeometryGroup.Children.Add(textGeo);
            return true;
        }

        public override bool TryContinue(CellLocation location) => false;

        public override void Complete()
        {
        }
    }
}