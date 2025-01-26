using System.Collections.Immutable;
using System.Windows.Media;
using VaettirNet.SudokuWriter.Library;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Gui.UiRules;

public class KropkiDotUiRule : EdgeUiRuleFactoryBase<KropkiDotRule>
{
    public bool IsDouble { get; }

    public KropkiDotUiRule(int cellSize, string name, bool isDouble) : base(cellSize, name, false)
    {
        IsDouble = isDouble;
    }

    protected override IEnumerable<IGameRule> SerializeCore(IEnumerable<UiGameRule> uiRules)
    {
        // Both dots are handled by the same factory, so skip it for one of them
        if (IsDouble) return [];

        ImmutableArray<GridEdge>.Builder doubles = ImmutableArray.CreateBuilder<GridEdge>();
        ImmutableArray<GridEdge>.Builder sequence = ImmutableArray.CreateBuilder<GridEdge>();
        foreach (Rule dotRule in uiRules.OfType<Rule>())
        {
            ref ImmutableArray<GridEdge>.Builder builder = ref dotRule.KropkiDotUiRule.IsDouble ? ref doubles : ref sequence; 
            foreach (EllipseGeometry geometry in dotRule.GeometryGroup.Children.OfType<EllipseGeometry>())
            {
                builder.Add(GetGridEdge(geometry.Center));
            }
        }

        if (doubles.Count == 0 && sequence.Count == 0) return [];
        
        return [new KropkiDotRule(doubles.ToImmutable(), sequence.ToImmutable())];
    }

    private class Rule : RuleBase
    {
        public KropkiDotUiRule KropkiDotUiRule { get; }

        public Rule(KropkiDotUiRule factory, Drawing drawing, GeometryGroup geometryGroup) : base(factory, drawing, geometryGroup)
        {
            KropkiDotUiRule = factory;
        }

        public override bool IsValid => true;
        
        public override bool TryAddSegment(CellLocation location, RuleParameters _)
        {
            if (!IsOnEdge(location)) return false;
            
            GeometryGroup.Children.Add(new EllipseGeometry(Factory.TranslateToPoint(location), Factory.CellSize / 8, Factory.CellSize / 8));
            return true;
        }

        public override bool TryContinue(CellLocation location) => false;

        public override void Complete()
        {
        }

        protected override Geometry BuildEdgeDisplayGeometry(CellLocation location)
        {
            return new EllipseGeometry(Factory.TranslateToPoint(location), Factory.CellSize / 8, Factory.CellSize / 8);
        }
    }

    protected override ImmutableArray<GridEdge> GetEdgesFromRule(KropkiDotRule rule)
    {
        return IsDouble ? rule.Doubles : rule.Sequential;
    }

    protected override RuleBase CreateRule(RuleParameters parameters, GeometryDrawing drawing, GeometryGroup grp)
    {
        return new Rule(this, drawing, grp);
    }

    protected override GeometryDrawing CreateInitialDrawing()
    {
        var drawing = new GeometryDrawing
        {
            Brush = IsDouble ? new SolidColorBrush(Color.FromRgb(32,32,32)) : Brushes.White,
            Pen = new Pen
            {
                Brush = new SolidColorBrush(Color.FromRgb(32,32,32)),
                Thickness = 0.5,
            },
        };
        return drawing;
    }
}