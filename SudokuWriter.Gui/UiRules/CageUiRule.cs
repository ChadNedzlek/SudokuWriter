using System.Collections.Immutable;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using SudokuWriter.Library;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Gui.UiRules;

public class CageUiRule : UiGameRuleFactory
{
    public override Range? VariationRange => 0..45;

    public CageUiRule(int cellSize, string name) : base(cellSize, name, true)
    {
    }

    protected override bool TryStart(CellLocation location, RuleParameters parameters, out UiGameRule createdRule)
    {
        BuildMainDrawing(out DrawingGroup mainGroup, out PathFigure figure);

        Geometry textGeo = null;
        if (parameters.SingleValue >= 3)
        {
            textGeo = BuildTextGeometry(mainGroup, parameters.SingleValue);
        }
        
        var rule = new Rule(this, mainGroup, figure, textGeo, location, parameters.SingleValue);
        rule.UpdateOutline();
        rule.MoveTextToUpperLeft(location.ToCoord());
        createdRule = rule;
        return true;
    }

    private static void BuildMainDrawing(out DrawingGroup mainGroup, out PathFigure figure)
    {
        mainGroup = new DrawingGroup();
        var boxDrawing = new GeometryDrawing
        {
            Pen = new Pen
            {
                Brush = Brushes.DimGray,
                Thickness = 0.75,
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                DashStyle = new DashStyle([6, 4], 0),
                DashCap = PenLineCap.Round,
            }
        };

        figure = new PathFigure{IsClosed = true};
        PathGeometry path = new PathGeometry([figure]);

        boxDrawing.Geometry = path;
        mainGroup.Children.Add(boxDrawing);
    }

    private Geometry BuildTextGeometry(DrawingGroup mainGroup, ushort sum)
    {
        var txt = new FormattedText(
            sum.ToString(),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            s_typeface,
            CellSize / 6.0,
            Brushes.Black,
            1
        );
        var txtDrawing = new GeometryDrawing
        {
            Brush = Brushes.Black,
        };
        Geometry textGeo = txt.BuildGeometry(default);
        txtDrawing.Geometry = textGeo;
        mainGroup.Children.Add(txtDrawing);
        return textGeo;
    }

    private double Padding => CellSize / 10.0;

    protected override IEnumerable<UiGameRule> DeserializeCore(IGameRule rule)
    {
        if (rule is not CageRule cage) return [];
        
        BuildMainDrawing(out var drawingGroup, out var figure);
        Geometry textGeo = null;
        if (cage.Sum >= 3)
        {
            textGeo = BuildTextGeometry(drawingGroup, cage.Sum);
        }

        var r = new Rule(this, drawingGroup, figure, textGeo, cage.Cage, cage.Sum);
        r.UpdateOutline();
        r.MoveTextToUpperLeft(cage.Cage);
        return [r];
    }

    protected override IEnumerable<IGameRule> SerializeCore(IEnumerable<UiGameRule> uiRules)
    {
        List<IGameRule> rules = [];
        foreach (Rule uiRule in uiRules.OfType<Rule>())
        {
            rules.Add(new CageRule(uiRule.Sum, uiRule.Cells.ToImmutableArray()));
        }

        return rules;
    }

    private static readonly Typeface s_typeface = BuildTypeface();

    private static Typeface BuildTypeface() => new("Verdana");

    private class Rule : UiGameRule
    {
        public ushort Sum { get; }
        public List<GridCoord> Cells { get; }
        private readonly CageUiRule _factory;
        private readonly PathFigure _figure;
        private readonly Geometry _textGeo;

        public Rule(CageUiRule factory, Drawing drawing, PathFigure figure, Geometry textGeo, CellLocation location, ushort sum) : base(factory, drawing)
        {
            Sum = sum;
            _factory = factory;
            _figure = figure;
            _textGeo = textGeo;
            Cells = [location.ToCoord()];
        }
        
        public Rule(CageUiRule factory, Drawing drawing, PathFigure figure, Geometry textGeo, IEnumerable<GridCoord> coords, ushort sum) : base(factory, drawing)
        {
            Sum = sum;
            _factory = factory;
            _figure = figure;
            _textGeo = textGeo;
            Cells = [..coords];
        }

        public override bool IsValid => true;
        
        public override bool TryAddSegment(CellLocation location, RuleParameters parameters)
        {
            return Cells.Contains(location.ToCoord());
        }

        public override bool TryContinue(CellLocation location)
        {
            var lCoord = location.ToCoord();
            int distanceToCage = Cells.Min(c => c.DistanceTo(lCoord));
            
            if (distanceToCage == 0)
            {
                return true;
            }

            if (distanceToCage > 1)
            {
                return false;
            }

            Cells.Add(lCoord);
            
            UpdateOutline();
            if (_textGeo != null)
            {
                MoveTextToUpperLeft(Cells);
            }

            return true;
        }

        public void MoveTextToUpperLeft(params IEnumerable<GridCoord> coords)
        {
            if (_textGeo == null) return;
            var topLeftCoord = coords.OrderBy(c => c.Col).ThenBy(c => c.Row).First();
            _textGeo.Transform = new TranslateTransform(
                topLeftCoord.Col * _factory.CellSize + _factory.Padding,
                topLeftCoord.Row * _factory.CellSize + _factory.Padding
            );
        }

        public void UpdateOutline()
        {
            _figure.Segments.Clear();
            GridCoord topLeftCoord = Cells.OrderBy(c => c.Col).ThenBy(c => c.Row).First();
            _figure.StartPoint =
                new Point(_factory.CellSize * topLeftCoord.Col + _factory.Padding, _factory.CellSize * topLeftCoord.Row + _factory.Padding);
            char side = 'u';
            GridCoord current = topLeftCoord;
            do
            {
                var overlapCell = side switch
                {
                    'u' => current - (1, 0),
                    'd' => current + (1, 0),
                    'l' => current - (0, 1),
                    'r' => current + (0, 1),
                };

                if (Cells.Contains(overlapCell))
                {
                    var nextEndPoint = side switch
                    {
                        'u' => new Point(
                            _factory.CellSize * current.Col + _factory.Padding,
                            _factory.CellSize * current.Row - _factory.Padding
                        ),
                        'd' => new Point(
                            _factory.CellSize * (current.Col + 1) - _factory.Padding,
                            _factory.CellSize * (current.Row + 1) + _factory.Padding
                        ),
                        'l' => new Point(
                            _factory.CellSize * current.Col - _factory.Padding,
                            _factory.CellSize * (current.Row + 1) - _factory.Padding
                        ),
                        'r' => new Point(
                            _factory.CellSize * (current.Col + 1) + _factory.Padding,
                            _factory.CellSize * current.Row + _factory.Padding
                        ),
                    };
                    _figure.Segments.Add(new LineSegment(nextEndPoint, true));
                    
                    current = overlapCell;
                    side = side switch
                    {
                        'u' => 'l',
                        'd' => 'r',
                        'l' => 'd',
                        'r' => 'u',
                    };
                    continue;
                }

                var end = side switch
                {
                    'u' => new Point(
                        _factory.CellSize * (current.Col + 1) - _factory.Padding,
                        _factory.CellSize * current.Row + _factory.Padding
                    ),
                    'd' => new Point(
                        _factory.CellSize * current.Col + _factory.Padding,
                        _factory.CellSize * (current.Row + 1) - _factory.Padding
                    ),
                    'l' => new Point(
                        _factory.CellSize * current.Col + _factory.Padding,
                        _factory.CellSize * current.Row + _factory.Padding
                    ),
                    'r' => new Point(
                        _factory.CellSize * (current.Col + 1) - _factory.Padding,
                        _factory.CellSize * (current.Row + 1) - _factory.Padding
                    ),
                };
                _figure.Segments.Add(new LineSegment(end, true));
                side = side switch
                {
                    'u' => 'r',
                    'd' => 'l',
                    'l' => 'u',
                    'r' => 'd',
                };
            } while (current != topLeftCoord || side != 'u');
        }

        public override void Complete()
        {
        }
    }
}