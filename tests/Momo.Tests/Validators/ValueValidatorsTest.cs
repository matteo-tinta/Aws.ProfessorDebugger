using System.Text.RegularExpressions;
using Momo.Validators;

namespace Momo.Tests.Validators;

public class ValueValidatorsTest
{
    [Test]
    [TestCase("hello world", "WorLd", true)]
    [TestCase("hello world", "not contained", false)]
    public void Value_Contains_ShouldContainsIgnoreCase(string actual, string contains, bool expected)
    {
        var value = new Value()
        {
            Contains = contains
        };
        
        Assert.That(value.Verify(actual), Is.EqualTo(expected));
    }
    
    [Test]
    [TestCase(5, 6, true)]
    [TestCase(5, 5, false)]
    [TestCase(5, 4, false)]
    public void Value_Gt_ShouldReturnTrueOnlyGreaterThan(int gt, int realValue, bool expected)
    {
        var value = new Value()
        {
            Gt = gt
        };
        
        Assert.That(value.Verify(realValue), Is.EqualTo(expected));
    }
    
    [Test]
    [TestCase(5, 6, false)]
    [TestCase(5, 5, false)]
    [TestCase(5, 4, true)]
    public void Value_Ls_ShouldReturnTrueOnlyIfLessThan(int ls, int actual, bool expected)
    {
        var value = new Value()
        {
            Ls = 5
        };
        
        Assert.That(value.Verify(actual), Is.EqualTo(expected));
    }
    
    [Test]
    [TestCase(".*", "hello world", true)]
    [TestCase("do_not_match", "hello world", false)]
    public void Value_Regex_ShouldReturnTrueOnlyIfRegexMatches(string pattern, string actual, bool expected)
    {
        var value = new Value()
        {
            Match = new Regex(pattern)
        };
        
        Assert.That(value.Verify(actual), Is.EqualTo(expected));
    }
    
    [Test]
    [TestCase("5", "5", true)]
    [TestCase("hello world", "HELLO WORLD", true)]
    [TestCase("5", "6", false)]
    [TestCase(5, 5, true)]
    [TestCase(5, 6, false)]
    [TestCase(true, true, true)]
    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    public void Value_ValueEquals_ShouldReturnTrueOnlyIfRegexMatches(object first, object actual, bool expected)
    {
        var value = new Value()
        {
            ValueEquals = first
        };
        
        Assert.That(value.Verify(actual), Is.EqualTo(expected));
    }

    [Test]
    public void Value_ChangingTheValue_DoNotVerifyTwice()
    {
        var value = new Value()
        {
            Ls = 3
        };

        value.Ls = 5;
        
        Assert.That(value.Verify(4), Is.True);
    }
}