using System.Collections.Immutable;
using System.Windows.Media;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Gui.UiRules;

public class ThermoUiRule : LineUiRule<ThermoLineRule>
{
    private class RuleImpl : Rule
    {
        public RuleImpl(UiGameRuleFactory factory, Drawing drawing, PathFigure path) : base(factory, drawing, path)
        {
        }
    }

    private readonly double _radius;

    public ThermoUiRule(int cellSize, string name) : base(cellSize, name, Brushes.LightGray)
    {
        _radius = cellSize / 2.0 - cellSize / 10.0 - 2.5;
    }

    protected override bool TryStart(CellLocation location, RuleParameters _, out UiGameRule createdRule)
    {
        if (!base.TryStart(location, _, out createdRule)) return false;

        if (createdRule is RuleImpl { Drawing: GeometryDrawing geoDrawing })
        {
            if (geoDrawing.Geometry is not GeometryGroup grp)
            {
                grp = new GeometryGroup();
                grp.Children.Add(geoDrawing.Geometry);
                geoDrawing.Geometry = grp;
            }
            
            grp.Children.Add(new EllipseGeometry(TranslateToPoint(location.Center), _radius, _radius));
        }

        return true;
    }

    protected override Rule CreateRule(GeometryDrawing drawing, PathFigure figure)
    {
        return new RuleImpl(this, drawing, figure);
    }

    protected override ThermoLineRule SerializeLineRule(ImmutableArray<BranchingRuleLine> ruleLines) => new(ruleLines);
}