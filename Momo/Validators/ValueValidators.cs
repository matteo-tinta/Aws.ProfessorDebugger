using System.Text.RegularExpressions;
using Momo.Exceptions;
using Newtonsoft.Json;

namespace Momo.Validators;

public static class ValueValidators
{
    public static Func<object, object, bool> StringContains => (value, contains) =>
    {
        var valueAsString = TryCastToString(value);
        var containsAsString = TryCastToString(contains);
        
        return valueAsString.Contains(containsAsString, StringComparison.InvariantCultureIgnoreCase);
    };
    
    public static Func<object, object, bool> GreaterThan => (value, second) => TryCastToInt(value).CompareTo(TryCastToInt(second)) > 0;
    
    public static Func<object, object, bool> LessThan => (value, second) => TryCastToInt(value).CompareTo(TryCastToInt(second)) < 0;
    
    public static Func<object, object, bool> GreaterThanOrEqualTo => (value, second) => TryCastToInt(value).CompareTo(TryCastToInt(second)) >= 0;
    
    public static Func<object, object, bool> LessThanOrEqualTo => (value, second) => TryCastToInt(value).CompareTo(TryCastToInt(second)) <= 0;
    
    public static Func<object, object, bool> StringMatches => (value, matcher) => TryCastToRegexp(matcher).Match(TryCastToString(value)).Success;

    public static Func<object, object, bool> Equals => (first, second) => (first, second) switch
    {
        (string f, string s) => f.Equals(s, StringComparison.InvariantCultureIgnoreCase),
        (int f, int s) => f == s,
        (long f, long s) => f == s,
        (short f, short s) => f == s,
        (bool f, bool s) => f == s,
        _ => throw new AssertException("Type is not recognized or invalid. Must be the same type").BreakWhenRaised()
    };

    private static string TryCastToString(object value)
    {
        return value as string ?? throw new AssertException("value must be a string").BreakWhenRaised();
    }
    
    private static int TryCastToInt(object value)
    {
        return value is int valueAsInt ? valueAsInt : throw new AssertException("value must be an integer").BreakWhenRaised();
    }
    
    private static Regex TryCastToRegexp(object value)
    {
        return value as Regex ?? throw new AssertException("value must be an integer").BreakWhenRaised();
    }
}

public record Value
{
    private readonly List<Func<object, bool>> _functions = []; 
    
    private string? _contains;
    public string? Contains
    {
        get => _contains;
        set
        {
            if (value is null)
                return;
            
            _contains = value;
            _functions.Add(realValue => ValueValidators.StringContains(realValue, value));
        }
    }
    
    private int? _gt;
    public int? Gt
    {
        get => _gt;
        set
        {
            if (value is null)
                return;
            
            _gt = value;
            _functions.Add(realValue => ValueValidators.GreaterThan(realValue, _gt!));
        }
    }
    
    private int? _ls;
    public int? Ls
    {
        get => _ls;
        set
        {
            if (value is null)
                return;
            
            _ls = value;
            _functions.Add(realValue => ValueValidators.LessThan(realValue, _ls!));
        }
    }
    
    private Regex? _match;
    public Regex? Match
    {
        get => _match;
        set
        {
            if (value is null)
                return;
            
            _match = value;
            _functions.Add(realValue => ValueValidators.StringMatches(realValue, _match!));
        }
    }
    
    private object? _valueEquals;
    
    [JsonProperty("equals")]
    public object? ValueEquals
    {
        get => _valueEquals;
        set
        {
            if (value is null)
                return;
            
            _valueEquals = value;
            _functions.Add(realValue => ValueValidators.Equals(realValue, _valueEquals!));
        }
    }

    public bool Verify(object? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        
        if (_functions.Count == 0)
        {
            throw new AssertException("Value must specify a valid object value").BreakWhenRaised();
        }
        
        return _functions.All(f => f(value));
    }
}