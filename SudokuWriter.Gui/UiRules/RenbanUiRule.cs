using System.Collections.Immutable;
using System.Windows.Media;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Gui.UiRules;

public class RenbanUiRule : LineUiRule<RenbanLine>
{
    private class RuleImpl : Rule {
        public RuleImpl(UiGameRuleFactory factory, Drawing drawing, PathFigure path) : base(factory, drawing, path)
        {
        }
    }

    public RenbanUiRule(int cellSize, string name) : base(cellSize, name, Brushes.Magenta)
    {
    }
    
    protected override Rule CreateRule(GeometryDrawing drawing, PathFigure figure)
    {
        return new RuleImpl(this, drawing, figure);
    }

    protected override RenbanLine SerializeLineRule(ImmutableArray<BranchingRuleLine> ruleLines) => new(ruleLines);
}