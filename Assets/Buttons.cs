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
    #endregion

    #region Lambda_Function
    Func<float, float, float> mul = (x, y) => x * y;
    Func<float, float, float> div = (x, y) => x / y;
    Func<float, float, float> add = (x, y) => x + y;
    Func<float, float, float> sub = (x, y) => x - y;
    Func<float, float, float> pow = (x, y) => Mathf.Pow(x, y);
    #endregion

    private Dictionary<string, Func<float, float, float>> _firstOrderOfOperations  = new Dictionary<string, Func<float, float, float>>();
    private Dictionary<string, Func<float, float, float>> _secondOrderOfOperations = new Dictionary<string, Func<float, float, float>>();

    private StringBuilder _equation = new StringBuilder();
    private int _numberOpenBrackets = 0;
    private bool _canOpenBracket = true;
    private bool _lastIsOperand = false;

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
        _equation.Append(input);

        _canOpenBracket = false;
        _lastIsOperand = false;

        UpdateText();
	}

    public void InputClear(string input)
    {
        _lastIsOperand = false;
        _canOpenBracket = true;
        _equation.Length = 0;

        UpdateText();
    }

    public void InputSymbol(string input)
    {
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
            if(_numberOpenBrackets > 0)
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
            _equation.Append(" " + input + " ");
        }

        UpdateText();
    }

    public void InputEquals(string input)
    {
        _canOpenBracket = false;
        _lastIsOperand = false;

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
            if(splitEquation[i] == "(")
            {
                if (startIndex == -1)
                {
                    startIndex = i;
                }
                bracketCount++;
            }
            else if(splitEquation[i] == ")")
            {
                bracketCount--;
                if (bracketCount == 0)
                {
                    endIndex = i;
                    string result = SolveEquation(splitEquation, startIndex+1, endIndex);
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
        for(int i = start; i < end; i++)
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
        _closeBracket.interactable = _numberOpenBrackets > 0;
    }
}
