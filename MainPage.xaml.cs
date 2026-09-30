using CalculatriceMaui.Services;

namespace CalculatriceMaui;

public partial class MainPage : ContentPage
{
    private const int MaxHistory = 20;

    private readonly CalculatorEngine _engine = new();
    private bool? _isLandscape;

    public MainPage()
    {
        InitializeComponent();
        _engine.Evaluated += AddToHistory;
        Refresh();
    }

    // ---- Gestionnaires des touches ----

    private void OnDigitClicked(object? sender, EventArgs e)
    {
        if (sender is Button b) _engine.Digit(b.Text[0]);
        Refresh();
    }

    private void OnOperatorClicked(object? sender, EventArgs e)
    {
        if (sender is Button b) _engine.Operator(b.Text[0]);
        Refresh();
    }

    private void OnDecimalClicked(object? sender, EventArgs e) { _engine.DecimalPoint(); Refresh(); }
    private void OnEqualsClicked(object? sender, EventArgs e) { _engine.Equals(); Refresh(); }
    private void OnClearClicked(object? sender, EventArgs e) { _engine.Clear(); Refresh(); }
    private void OnBackspaceClicked(object? sender, EventArgs e) { _engine.Backspace(); Refresh(); }
    private void OnToggleSignClicked(object? sender, EventArgs e) { _engine.ToggleSign(); Refresh(); }
    private void OnPercentClicked(object? sender, EventArgs e) { _engine.Percent(); Refresh(); }
    private void OnSquareRootClicked(object? sender, EventArgs e) { _engine.SquareRoot(); Refresh(); }
    private void OnSquareClicked(object? sender, EventArgs e) { _engine.Square(); Refresh(); }
    private void OnReciprocalClicked(object? sender, EventArgs e) { _engine.Reciprocal(); Refresh(); }

    private void OnClearHistoryClicked(object? sender, EventArgs e)
    {
        HistoryList.Children.Clear();
        UpdateHistoryHeader();
    }

    // ---- Affichage ----

    private void Refresh()
    {
        ExpressionLabel.Text = string.IsNullOrEmpty(_engine.Expression) ? " " : _engine.Expression;
        ResultLabel.Text = _engine.Display;
        ResultLabel.TextColor = GetColor(_engine.HasError ? "ErrorText" : "TextPrimary");
        FitResultFont();
    }

    private void AddToHistory(string expression, string result)
    {
        var entry = new VerticalStackLayout
        {
            Spacing = 0,
            Children =
            {
                new Label
                {
                    Text = expression,
                    FontSize = 13,
                    TextColor = GetColor("TextSecondary"),
                    HorizontalTextAlignment = TextAlignment.End,
                    LineBreakMode = LineBreakMode.HeadTruncation,
                },
                new Label
                {
                    Text = result,
                    FontSize = 17,
                    TextColor = GetColor("TextPrimary"),
                    HorizontalTextAlignment = TextAlignment.End,
                    LineBreakMode = LineBreakMode.HeadTruncation,
                },
            },
        };

        HistoryList.Children.Add(entry);
        if (HistoryList.Children.Count > MaxHistory) HistoryList.Children.RemoveAt(0);
        UpdateHistoryHeader();

        // Fait défiler jusqu'au dernier calcul une fois la mise en page terminée.
        Dispatcher.Dispatch(async () => await HistoryScroll.ScrollToAsync(entry, ScrollToPosition.End, false));
    }

    private void UpdateHistoryHeader()
    {
        var n = HistoryList.Children.Count;
        HistoryCountLabel.Text = n == 0 ? "· historique vide" : $"· {n} calcul{(n > 1 ? "s" : "")}";
        ClearHistoryButton.IsVisible = n > 0;
    }

    // ---- Adaptation à la taille de l'écran et à l'orientation ----

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        if (Width <= 0 || Height <= 0) return;

        var landscape = Width > Height;
        if (landscape == _isLandscape) { FitResultFont(); return; }
        _isLandscape = landscape;

        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        if (landscape)
        {
            // Paysage : affichage à gauche, clavier à droite.
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.2, GridUnitType.Star)));

            Place(HeaderBar, row: 0, column: 0, columnSpan: 2);
            Place(DisplayCard, row: 1, column: 0);
            Place(Keypad, row: 1, column: 1);
        }
        else
        {
            // Portrait : affichage en haut (2/5), clavier en bas (3/5).
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(2, GridUnitType.Star)));
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(3, GridUnitType.Star)));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            Place(HeaderBar, row: 0, column: 0);
            Place(DisplayCard, row: 1, column: 0);
            Place(Keypad, row: 2, column: 0);
        }

        FitResultFont();
    }

    private static void Place(View view, int row, int column, int columnSpan = 1)
    {
        Grid.SetRow(view, row);
        Grid.SetColumn(view, column);
        Grid.SetColumnSpan(view, columnSpan);
    }

    /// <summary>Taille la police des touches d'après la taille réelle d'une cellule du clavier.</summary>
    private void OnKeypadSizeChanged(object? sender, EventArgs e)
    {
        if (Keypad.Width <= 0 || Keypad.Height <= 0) return;

        var cell = Math.Min(Keypad.Height / 6, Keypad.Width / 4);
        var fontSize = Math.Clamp(cell * 0.38, 14, 32);

        foreach (var button in Keypad.Children.OfType<Button>().Concat(FunctionRow.Children.OfType<Button>()))
            button.FontSize = fontSize;

        // Aligne les touches de la FlexLayout sur les colonnes de la grille du clavier.
        var columnWidth = (Keypad.Width - Keypad.ColumnSpacing * 3) / 4;
        foreach (var button in FunctionRow.Children.OfType<Button>())
            button.WidthRequest = columnWidth;

        FitResultFont();
    }

    /// <summary>Réduit la police du résultat pour qu'un nombre long tienne toujours sur une ligne.</summary>
    private void FitResultFont()
    {
        var width = DisplayCard.Width - DisplayCard.Padding.HorizontalThickness;
        var height = DisplayCard.Height;
        if (width <= 0 || height <= 0) return;

        var maxFont = Math.Clamp(height * 0.3, 28, 64);
        var chars = Math.Max(ResultLabel.Text?.Length ?? 1, 1);
        ResultLabel.FontSize = Math.Clamp(width / (chars * 0.62), 16, maxFont);
        ExpressionLabel.FontSize = Math.Clamp(maxFont * 0.38, 14, 24);
    }

    private static Color GetColor(string key) =>
        Application.Current!.Resources.TryGetValue(key, out var value) && value is Color c ? c : Colors.White;
}
