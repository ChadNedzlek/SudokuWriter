using System.Windows;
using System.Windows.Media;

namespace VaettirNet.SudokuWriter.Gui.UiRules;

public abstract class UiGameRule
{
    public readonly UiGameRuleFactory Factory;

    protected UiGameRule(UiGameRuleFactory factory, Drawing drawing)
    {
        Factory = factory;
        Drawing = drawing;
    }

    public Drawing Drawing { get; }
    public abstract bool IsValid { get; }

    public bool TryAddSegment(Point point, RuleParameters parameters) => TryAddSegment(Factory.TranslateFromPoint(point), parameters);
    public abstract bool TryAddSegment(CellLocation location, RuleParameters parameters);

    public bool TryContinue(Point point) => TryContinue(Factory.TranslateFromPoint(point));
    public abstract bool TryContinue(CellLocation location);

    public abstract void Complete();
}