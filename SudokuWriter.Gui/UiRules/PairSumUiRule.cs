using System.Collections.Immutable;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using SudokuWriter.Library;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Gui.UiRules;

public class PairSumUiRule : EdgeUiRuleFactoryBase<PairSumRule>
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

    protected override RuleBase CreateRule(RuleParameters parameters, GeometryDrawing drawing, GeometryGroup grp)
    {
        return new Rule(this, drawing, grp, parameters.SingleValue);
    }

    protected override GeometryDrawing CreateInitialDrawing()
    {
        return new GeometryDrawing
        {
            Brush = Brushes.Black,
        };
    }
    
    protected override ImmutableArray<GridEdge> GetEdgesFromRule(PairSumRule rule)
    {
        return rule.Pairs;
    }

    protected override IEnumerable<UiGameRule> DeserializeCore(IGameRule rule)
    {
        if (rule is not PairSumRule pair) return [];
        UiGameRule uiRule = null;
        foreach (GridEdge r in pair.Pairs)
        {
            CellLocation location = GetLocation(r);
            var ruleParameters = new RuleParameters(0, pair.Sum);
            if (uiRule is null)
            {
                TryStart(location, ruleParameters, out uiRule);
            }
            else
            {
                uiRule.TryAddSegment(location, ruleParameters);
            }
        }

        return [uiRule];
    }

    protected override IEnumerable<IGameRule> SerializeCore(IEnumerable<UiGameRule> rules)
    {
        List<PairSumRule> gameRules = [];
        foreach (Rule dotRule in rules.OfType<Rule>())
        {
            ImmutableArray<GridEdge>.Builder edges = ImmutableArray.CreateBuilder<GridEdge>();
            foreach (Geometry geometry in dotRule.GeometryGroup.Children)
            {
                Rect b = geometry.Bounds;
                (double x, double y) = (b.Left + b.Width / 2, b.Top + b.Height / 2);
                edges.Add(GetGridEdge(new Point(x, y)));
            }
            gameRules.Add(new PairSumRule(dotRule.Sum, edges.ToImmutable()));
        }

        return gameRules;
    }

    private class Rule : RuleBase
    {
        public ushort Sum { get; }
        public PairSumUiRule PairSumUiRule { get; }

        private static readonly Typeface s_typeface = BuildTypeface();

        private static Typeface BuildTypeface()
        {
            Typeface verdanaDefault = new Typeface("Verdana");
            return new Typeface(verdanaDefault.FontFamily, verdanaDefault.Style, FontWeights.Bold, verdanaDefault.Stretch, null);
        }

        public Rule(PairSumUiRule factory, Drawing drawing, GeometryGroup geometryGroup, ushort sum) : base(factory, drawing, geometryGroup)
        {
            PairSumUiRule = factory;
            Sum = sum;
        }

        public override bool IsValid => true;

        protected override Geometry BuildEdgeDisplayGeometry(CellLocation location)
        {
            FormattedText txt = new FormattedText(ToRomanNumeral(Sum),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                s_typeface,
                Factory.CellSize / 3.0,
                Brushes.Black,
                1);
            Geometry textGeo = txt.BuildGeometry(Factory.TranslateToPoint(location));
            textGeo.Transform = new TranslateTransform(-textGeo.Bounds.Width / 2, -textGeo.Bounds.Height / 2 - Factory.CellSize / 10.0);
            return textGeo;
        }

        public override bool TryAddSegment(CellLocation location, RuleParameters parameters)
        {
            if (parameters.SingleValue != Sum) return false;

            return base.TryAddSegment(location, parameters);
        }

        public override bool TryContinue(CellLocation location) => false;

        public override void Complete()
        {
        }
    }
}