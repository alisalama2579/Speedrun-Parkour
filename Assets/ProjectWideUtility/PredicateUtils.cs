using NUnit.Framework.Internal;
using System;
using UnityEngine;

public interface IPredicate
{
    public Func<bool> Func { get; set; }
    public bool Test => Func.Invoke();
}

public class ConditionPredicate : IPredicate
{
    public Func<bool> Func { get; set; }
    public ConditionPredicate(Func<bool> func)
    {
        this.Func = func;
    }
}

public class TimedPredicate : IPredicate
{
    public Func<bool> Func { get; set; }
    private float timeStarted;
    private float delay;

    private bool TestMethod() => Time.time >= timeStarted + delay;

    public TimedPredicate(float delay)
    {
        timeStarted = Time.time;
        this.delay = delay;
        Func = TestMethod;
    }
}
