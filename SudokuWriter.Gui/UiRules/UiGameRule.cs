using System.Windows;
using System.Windows.Media;

namespace SudokuWriter.Gui.UiRules;

public abstract class UiGameRule
{
    public readonly UiGameRuleFactory Factory;

    protected UiGameRule(UiGameRuleFactory factory, Drawing drawing)
    {
        Factory = factory;
        Drawing = drawing;
    }

    public Drawing Drawing { get; }

    public bool TryAddSegment(Point point) => TryAddSegment(Factory.TranslateFromPoint(point));
    public abstract bool TryAddSegment(CellLocation location);

    public void Continue(Point point) => Continue(Factory.TranslateFromPoint(point));
    public abstract void Continue(CellLocation location);

    public abstract void Complete();
}