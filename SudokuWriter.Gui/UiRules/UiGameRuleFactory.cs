using System.Windows;
using SudokuWriter.Library;

namespace SudokuWriter.Gui.UiRules;

public readonly record struct RuleParameters(ushort MultipleValues, ushort SingleValue);

public abstract class UiGameRuleFactory
{
    public string Name { get; }
    public bool IsContinuous { get; }
    public virtual Range? VariationRange => null;
    public virtual bool HasVariationDigits => false;

    public int CellSize { get; }
    public int CenterPadding { get; }
    public int HalfCellSize { get; }

    public UiGameRuleFactory(int cellSize, string name, bool isContinuous)
    {
        CellSize = cellSize;
        Name = name;
        IsContinuous = isContinuous;
        CenterPadding = CellSize / 4;
        HalfCellSize = CellSize >> 1;
    }
    
    public CellLocation TranslateFromPoint(Point point)
    {
        double col = point.X / CellSize;
        double row = point.Y / CellSize;
        int colOffset;
        if (point.X % CellSize < CenterPadding)
            colOffset = -1;
        else if (point.X % CellSize > CellSize - CenterPadding)
            colOffset = 1;
        else
            colOffset = 0;

        int rowOffset;
        if (point.Y % CellSize < CenterPadding)
            rowOffset = -1;
        else if (point.Y % CellSize > CellSize - CenterPadding)
            rowOffset = 1;
        else
            rowOffset = 0;

        return new CellLocation((int)row, (int)col, rowOffset, colOffset);
    }

    public Point TranslateToPoint(CellLocation location)
    {
        return new(
            location.Col * CellSize + (location.ColSide + 1) * HalfCellSize,
            location.Row * CellSize + (location.RowSide + 1) * HalfCellSize
        );
    }
    
    public bool TryStart(Point location, RuleParameters parameters, out UiGameRule createdRule) => TryStart(TranslateFromPoint(location), parameters, out createdRule);
    protected abstract bool TryStart(CellLocation location, RuleParameters parameters,out UiGameRule createdRule);

    public IEnumerable<IGameRule> SerializeRules(IEnumerable<UiGameRule> rules)
    {
        return SerializeCore(rules.Where(r => r.Factory.GetType() == GetType()));
    }

    public IEnumerable<UiGameRule> DeserializeRules(IEnumerable<IGameRule> rules)
    {
        foreach (IGameRule rule in rules)
        {
            if (DeserializeCore(rule) is not { } uiRuleSet) continue;
            
            foreach (UiGameRule uiRule in uiRuleSet)
            {
                yield return uiRule;
            }
        }
    }

    protected abstract IEnumerable<UiGameRule> DeserializeCore(IGameRule rule);

    protected abstract IEnumerable<IGameRule> SerializeCore(IEnumerable<UiGameRule> uiRules);
}