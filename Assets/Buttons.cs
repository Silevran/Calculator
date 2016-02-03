using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Buttons : MonoBehaviour
{
    #region SerializeFields
    [SerializeField]
    private Text _currentNumberText = null;
    [SerializeField]
    private Text _fullEquationText = null;

    [SerializeField]
    private Button _openBracket = null;
    [SerializeField]
    private Button _closeBracket = null;
    [SerializeField]
    private List<Button> _numberButtons = new List<Button>();
    #endregion

    #region Lambda_Function
    Func<float, float, float> mul = (x, y) => x * y;
    Func<float, float, float> div = (x, y) => x / y;
    Func<float, float, float> add = (x, y) => x + y;
    Func<float, float, float> sub = (x, y) => x - y;
    Func<float, float, float> pow = (x, y) => Mathf.Pow(x, y);
    #endregion

    private Dictionary<string, Func<float, float, float>> _firstOrderOfOperations = new Dictionary<string, Func<float, float, float>>();
    private Dictionary<string, Func<float, float, float>> _secondOrderOfOperations = new Dictionary<string, Func<float, float, float>>();

    private StringBuilder _equation = new StringBuilder();
    private int _numberOpenBrackets = 0;
    private bool _canOpenBracket = true;
    private bool _lastIsOperand = false;
    private bool _lastIsCloseBracket = false;
    private bool _newEquation = true;

    void Start()
    {
        UpdateText();

        _firstOrderOfOperations.Add("*", mul);
        _firstOrderOfOperations.Add("/", div);
        _firstOrderOfOperations.Add("^", pow);

        _secondOrderOfOperations.Add("+", add);
        _secondOrderOfOperations.Add("-", sub);
    }

    public void InputNumber(string input)
    {
        //We have just pressed equals, so a new number will overwrite what we have.
        if (_newEquation)
        {
            _equation.Length = 0;
        }

        _equation.Append(input);

        _canOpenBracket = false;
        _lastIsOperand = false;
        _newEquation = false;
        _lastIsCloseBracket = false;

        UpdateText();
    }

    public void InputClear(string input)
    {
        _lastIsOperand = false;
        _canOpenBracket = true;
        _equation.Length = 0;
        _newEquation = false;
        _lastIsCloseBracket = false;

        UpdateText();
    }

    public void InputSymbol(string input)
    {
        string checker = _equation.ToString();
        if (checker == "Infinity" || checker == "NaN")
        {
            InputClear("Clear");
        }

        _lastIsCloseBracket = false;

        if (input == "(")
        {
            _lastIsOperand = false;
            _numberOpenBrackets++;
            _equation.Append(" " + input + " ");
        }
        else if (input == ")")
        {
            _lastIsOperand = false;
            _canOpenBracket = false;
            _lastIsCloseBracket = true;
            if (_numberOpenBrackets > 0)
            {
                _numberOpenBrackets--;
                _equation.Append(" " + input + " ");
            }
        }
        //Pressing a operand twice in a row will replace the operand
        else if (_lastIsOperand)
        {
            _canOpenBracket = true;
            _equation[_equation.Length - 2] = input[0];
        }
        else
        {
            _lastIsOperand = true;
            _canOpenBracket = true;

            if (_equation.Length == 0 || (_equation.Length > 2 && _equation[_equation.Length - 2] == '('))
            {
                InputNumber("0");
            }

            _equation.Append(" " + input + " ");
        }

        _newEquation = false;

        UpdateText();
    }

    public void InputEquals(string input)
    {
        _canOpenBracket = false;

        if (_lastIsOperand)
        {
            _equation.Length = _equation.Length - 3;
        }

        _lastIsOperand = false;
        _lastIsCloseBracket = false;

        //Add all remaining closing brackets to the endof the equation
        while (_numberOpenBrackets > 0)
        {
            _numberOpenBrackets--;
            _equation.Append(" )");
        }

        //Remove double spaces and ending spaces to optimize things a little
        string fullEquation = _equation.ToString();
        fullEquation = fullEquation.Replace("  ", " ");
        fullEquation = fullEquation.Trim();

        List<string> splitEquation = new List<string>(fullEquation.Split(' '));

        //Clear the buffer for a new string and calculate
        _equation.Length = 0;
        _equation.Append(SolveEquation(splitEquation, 0, splitEquation.Count));

        _newEquation = true;

        UpdateText();
    }

    private string SolveEquation(List<string> splitEquation, int start, int end)
    {
        int startIndex = -1;
        int endIndex = -1;
        int bracketCount = 0;

        //Find matchingbracket pairs and solve for inside them
        for (int i = start; i < end; i++)
        {
            if (splitEquation[i] == "(")
            {
                if (startIndex == -1)
                {
                    startIndex = i;
                }
                bracketCount++;
            }
            else if (splitEquation[i] == ")")
            {
                bracketCount--;
                if (bracketCount == 0)
                {
                    endIndex = i;
                    string result = SolveEquation(splitEquation, startIndex + 1, endIndex);
                    splitEquation.RemoveRange(startIndex, 2);
                    splitEquation[startIndex] = result;

                    i = startIndex;
                    end -= (endIndex - startIndex);

                    startIndex = -1;
                    endIndex = -1;
                }
            }
        }

        //First order Operation
        for (int i = start; i < end; i++)
        {
            if (_firstOrderOfOperations.ContainsKey(splitEquation[i]))
            {
                DoCalculation(splitEquation, _firstOrderOfOperations[splitEquation[i]], ref i, ref end);
            }
        }

        //Second order Operation
        for (int i = start; i < end; i++)
        {
            if (_secondOrderOfOperations.ContainsKey(splitEquation[i]))
            {
                DoCalculation(splitEquation, _secondOrderOfOperations[splitEquation[i]], ref i, ref end);
            }
        }

        return splitEquation[start];
    }

    private void DoCalculation(List<string> split, Func<float, float, float> operation,
                               ref int index, ref int endValue)
    {
        float first = float.Parse(split[index - 1]);
        float second = float.Parse(split[index + 1]);

        float total = operation.Invoke(first, second);

        split.RemoveRange(index - 1, 2);
        split[index - 1] = total.ToString();
        index -= 2;
        endValue -= 2;
    }

    private void UpdateText()
    {
        if (_equation.Length <= 0)
        {
            _currentNumberText.text = "0";
            _fullEquationText.text = "";
        }
        else
        {
            string fullEquation = _equation.ToString();
            int lastSpace = fullEquation.LastIndexOf(' ');
            if (lastSpace > 0)
            {
                string lastNumber = fullEquation.Substring(lastSpace + 1);

                if (lastNumber.Length > 0)
                {
                    _currentNumberText.text = lastNumber;
                }
                else
                {
                    _currentNumberText.text = "0";
                }
            }
            else
            {
                _currentNumberText.text = fullEquation;
            }
            _fullEquationText.text = fullEquation;

        }

        _openBracket.interactable = _canOpenBracket;
        _closeBracket.interactable = (_numberOpenBrackets > 0 & !_lastIsOperand);

        foreach (Button button in _numberButtons)
        {
            button.interactable = !_lastIsCloseBracket;
        }
    }

    private void Update()
    {
        #region keyboard number input
        if (Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Alpha0))
        {
            InputNumber("0");
        }
        if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            InputNumber("1");
        }
        if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            InputNumber("2");
        }
        if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha3))
        {
            InputNumber("3");
        }
        if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Alpha4))
        {
            InputNumber("4");
        }
        if (Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.Alpha5))
        {
            InputNumber("5");
        }
        if (Input.GetKeyDown(KeyCode.Keypad6))
        {
            InputNumber("6");
        }
        if (Input.GetKeyDown(KeyCode.Keypad7) || Input.GetKeyDown(KeyCode.Alpha7))
        {
            InputNumber("7");
        }
        if (Input.GetKeyDown(KeyCode.Keypad8))
        {
            InputNumber("8");
        }
        if (Input.GetKeyDown(KeyCode.Keypad9) || Input.GetKeyDown(KeyCode.Alpha9))
        {
            InputNumber("9");
        }
        if (Input.GetKeyDown(KeyCode.KeypadPeriod) || Input.GetKeyDown(KeyCode.Period))
        {
            InputNumber(".");
        }
        #endregion

        #region keyboard_symbol_input
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            InputSymbol("+");
        }
        if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
        {
            InputSymbol("-");
        }
        if (Input.GetKeyDown(KeyCode.KeypadDivide) || Input.GetKeyDown(KeyCode.Slash))
        {
            InputSymbol("/");
        }
        if (Input.GetKeyDown(KeyCode.KeypadMultiply))
        {
            InputSymbol("*");
        }
        #endregion

        #region bracket_symbols
        if (_canOpenBracket && (Input.GetKeyDown(KeyCode.LeftParen) || Input.GetKeyDown(KeyCode.LeftBracket)))
        {
            InputSymbol("(");
        }
        else if (_numberOpenBrackets > 0 && (Input.GetKeyDown(KeyCode.RightParen) || Input.GetKeyDown(KeyCode.RightBracket)))
        {
            InputSymbol(")");
        }
        #endregion

        #region other_symbols
        if (Input.GetKeyDown(KeyCode.KeypadEquals) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
        {
            InputEquals("=");
        }
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            InputClear("Clear");
        }
        #endregion

        #region special_cases
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                InputSymbol("^");
            }
            else
            {
                InputNumber("6");
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                InputSymbol("*");
            }
            else
            {
                InputNumber("8");
            }
        }
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                InputEquals("=");
            }
            else
            {
                InputSymbol("+");
            }
        }
        #endregion
    }
}
