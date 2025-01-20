using System.Collections.Immutable;
using System.Windows.Media;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Gui.UiRules;

public class RenbanUiRule : LineUiRule<RenbanLine>
{
    private class RuleImpl : Rule {
        public RuleImpl(UiGameRuleFactory factory, Drawing drawing, PathFigure path) : base(factory, drawing, path)
        {
        }
    }

    public RenbanUiRule(int cellSize, string name) : base(cellSize, name, Brushes.MediumPurple)
    {
    }
    
    protected override Rule CreateRule(GeometryDrawing drawing, PathFigure figure)
    {
        return new RuleImpl(this, drawing, figure);
    }

    protected override RenbanLine SerializeLineRule(ImmutableArray<BranchingRuleLine> ruleLines) => new(ruleLines);
}