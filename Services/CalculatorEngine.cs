using System.Globalization;

namespace CalculatriceMaui.Services;

/// <summary>
/// Logique de la calculatrice, indépendante de l'interface.
/// Les nombres sont stockés en <see cref="decimal"/> pour éviter les erreurs
/// d'arrondi binaires (0,1 + 0,2 = 0,3).
/// </summary>
public class CalculatorEngine
{
    private const int MaxDigits = 16;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private string _entry = "0";          // saisie courante (séparateur '.')
    private decimal? _accumulator;        // opérande gauche
    private char? _pendingOp;             // opérateur en attente
    private bool _startNewEntry = true;   // la prochaine touche chiffre remplace la saisie
    private bool _operandReady;           // opérande droit fourni par %, ±, √… (sans frappe de chiffre)
    private char? _lastOp;                // pour la répétition de "="
    private decimal _lastOperand;

    public string Expression { get; private set; } = string.Empty;
    public bool HasError { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;

    /// <summary>Texte à afficher dans la zone résultat.</summary>
    public string Display => HasError ? ErrorMessage : ToDisplay(_entry);

    /// <summary>Déclenché à chaque calcul terminé par "=" (pour l'historique).</summary>
    public event Action<string, string>? Evaluated;

    public void Digit(char d)
    {
        if (HasError) Clear();

        if (_startNewEntry)
        {
            _entry = d.ToString();
            _startNewEntry = false;
            if (_pendingOp is null) Expression = string.Empty;
            return;
        }

        if (CountDigits(_entry) >= MaxDigits) return;
        _entry = _entry == "0" ? d.ToString()
               : _entry == "-0" ? "-" + d
               : _entry + d;
    }

    public void DecimalPoint()
    {
        if (HasError) Clear();

        if (_startNewEntry)
        {
            _entry = "0.";
            _startNewEntry = false;
            if (_pendingOp is null) Expression = string.Empty;
            return;
        }

        if (!_entry.Contains('.')) _entry += ".";
    }

    public void Operator(char op)
    {
        if (HasError) return;

        if (_pendingOp is not null && (!_startNewEntry || _operandReady))
        {
            // Enchaînement : 2 + 3 × … calcule d'abord 2 + 3.
            if (!TryCompute(_accumulator!.Value, _pendingOp.Value, Parse(_entry), out var r)) return;
            _accumulator = r;
            _entry = Format(r);
        }
        else if (_pendingOp is null)
        {
            _accumulator = Parse(_entry);
        }
        // Sinon : l'utilisateur change simplement d'opérateur.

        _pendingOp = op;
        _startNewEntry = true;
        _operandReady = false;
        Expression = $"{ToDisplay(Format(_accumulator!.Value))} {op}";
    }

    public void Equals()
    {
        if (HasError) return;

        decimal left, right;
        char op;

        if (_pendingOp is not null)
        {
            left = _accumulator!.Value;
            op = _pendingOp.Value;
            right = _startNewEntry && !_operandReady ? left : Parse(_entry); // "5 + =" => 5 + 5
        }
        else if (_lastOp is not null)
        {
            // Répétition : 2 + 3 = = => 8
            left = Parse(_entry);
            op = _lastOp.Value;
            right = _lastOperand;
        }
        else return;

        Expression = $"{ToDisplay(Format(left))} {op} {ToDisplay(Format(right))} =";
        if (!TryCompute(left, op, right, out var result)) return;

        _entry = Format(result);
        _lastOp = op;
        _lastOperand = right;
        _pendingOp = null;
        _accumulator = null;
        _startNewEntry = true;
        _operandReady = false;
        Evaluated?.Invoke(Expression, Display);
    }

    public void Clear()
    {
        _entry = "0";
        _accumulator = null;
        _pendingOp = null;
        _lastOp = null;
        _startNewEntry = true;
        _operandReady = false;
        Expression = string.Empty;
        HasError = false;
        ErrorMessage = string.Empty;
    }

    public void Backspace()
    {
        if (HasError) { Clear(); return; }
        if (_startNewEntry) return; // on n'efface pas un résultat caractère par caractère

        _entry = _entry[..^1];
        if (_entry is "" or "-") { _entry = "0"; }
    }

    public void ToggleSign()
    {
        if (HasError) return;
        if (_entry.StartsWith('-')) _entry = _entry[1..];
        else if (Parse(_entry) != 0) _entry = "-" + _entry;
        else return;

        if (_pendingOp is not null) _operandReady = true;
        else if (_startNewEntry) Expression = string.Empty;
    }

    public void Percent()
    {
        if (HasError) return;
        var value = Parse(_entry);

        // 200 + 10 % => 200 + 20 ; 50 × 10 % => 50 × 0,1
        value = _pendingOp is '+' or '−' && _accumulator is not null
            ? _accumulator.Value * value / 100m
            : value / 100m;

        _entry = Format(value);
        _startNewEntry = true;
        _operandReady = _pendingOp is not null;
    }

    // ---- Opérations avancées ----

    public void SquareRoot()
    {
        if (HasError) return;
        var x = Parse(_entry);
        SetUnaryExpression($"√({ToDisplay(_entry)})");
        if (x < 0) { SetError("Entrée invalide"); return; }
        ApplyUnary((decimal)Math.Sqrt((double)x));
    }

    public void Square()
    {
        if (HasError) return;
        var x = Parse(_entry);
        SetUnaryExpression($"sqr({ToDisplay(_entry)})");
        try { ApplyUnary(x * x); }
        catch (OverflowException) { SetError("Dépassement de capacité"); }
    }

    public void Reciprocal()
    {
        if (HasError) return;
        var x = Parse(_entry);
        SetUnaryExpression($"1/({ToDisplay(_entry)})");
        if (x == 0) { SetError("Division par zéro impossible"); return; }
        ApplyUnary(1m / x);
    }

    // ---- Outils internes ----

    private void SetUnaryExpression(string unary)
    {
        Expression = _pendingOp is null
            ? unary
            : $"{ToDisplay(Format(_accumulator!.Value))} {_pendingOp} {unary}";
    }

    private void ApplyUnary(decimal value)
    {
        _entry = Format(value);
        _startNewEntry = true;
        _operandReady = _pendingOp is not null;
    }

    private bool TryCompute(decimal a, char op, decimal b, out decimal result)
    {
        result = 0;
        try
        {
            switch (op)
            {
                case '+': result = a + b; break;
                case '−': result = a - b; break;
                case '×': result = a * b; break;
                case '÷':
                    if (b == 0)
                    {
                        SetError(a == 0 ? "Résultat indéfini" : "Division par zéro impossible");
                        return false;
                    }
                    result = a / b;
                    break;
            }
            return true;
        }
        catch (OverflowException)
        {
            SetError("Dépassement de capacité");
            return false;
        }
    }

    private void SetError(string message)
    {
        HasError = true;
        ErrorMessage = message;
        _pendingOp = null;
        _accumulator = null;
        _lastOp = null;
        _startNewEntry = true;
        _operandReady = false;
        _entry = "0";
    }

    private static decimal Parse(string s) =>
        decimal.Parse(s, NumberStyles.Float, Inv);

    private static string Format(decimal v)
    {
        var s = Math.Round(v, 10).ToString("0.##########", Inv);
        if (CountDigits(s) > MaxDigits)
            s = ((double)v).ToString("G12", Inv); // notation scientifique pour les très grands nombres
        return s == "-0" ? "0" : s;
    }

    private static int CountDigits(string s) => s.Count(char.IsDigit);

    /// <summary>Affichage à la française : virgule décimale.</summary>
    private static string ToDisplay(string s) => s.Replace('.', ',');
}
