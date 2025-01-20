using System.Collections.Immutable;
using System.Windows.Media;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Gui.UiRules;

public class GermanWhisperUiRule : LineUiRule<GermanWhispersLineRule>
{
    private class RuleImpl : Rule {
        public RuleImpl(UiGameRuleFactory factory, Drawing drawing, PathFigure path) : base(factory, drawing, path)
        {
        }
    }

    public GermanWhisperUiRule(int cellSize, string name) : base(cellSize, name, Brushes.GreenYellow)
    {
    }
    
    protected override Rule CreateRule(GeometryDrawing drawing, PathFigure figure)
    {
        return new RuleImpl(this, drawing, figure);
    }

    protected override GermanWhispersLineRule SerializeLineRule(ImmutableArray<BranchingRuleLine> ruleLines) => new(ruleLines);
}