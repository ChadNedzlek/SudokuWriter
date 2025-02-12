using System.Collections.Immutable;
using System.Windows.Media;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Gui.UiRules;

public class ModLineUiRule : LineUiRule<ModLineRule>
{
    private class RuleImpl : Rule
    {
        public RuleImpl(UiGameRuleFactory factory, Drawing drawing, PathFigure path) : base(factory, drawing, path)
        {
        }
    }

    public ModLineUiRule(int cellSize, string name) : base(cellSize, name, Brushes.Cyan)
    {
    }
    
    protected override Rule CreateRule(GeometryDrawing drawing, PathFigure figure) => new RuleImpl(this, drawing, figure);

    protected override ModLineRule SerializeLineRule(ImmutableArray<BranchingRuleLine> ruleLines) => new(ruleLines);
}