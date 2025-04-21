using System;

public static class TransitionLibrary
{
    public class Transition 
    {
        public IState To { get; }
        public Func<IStateSpecificTransitionData> Func { get; }

        public event Action OnTransition;
        public void ClearEvent() => OnTransition = null;
        public bool TryExecute(out IStateSpecificTransitionData data)
        {
            data = Func.Invoke();
            if (data.ConditionMet)
                OnTransition?.Invoke();

            return data.ConditionMet;
        }

        public Transition(IState to, Func<IStateSpecificTransitionData> func)
        {
            To = to;
            Func = func;
        }
    }
    public static IStateSpecificTransitionData AnyTransitionFunc() => new SuccesfulTransitionData();


    public interface IStateSpecificTransitionData{  public bool ConditionMet { get; }  }

    public static readonly FailedTransitionData failedData = new();
    public class FailedTransitionData : IStateSpecificTransitionData {  public bool ConditionMet => false; }


    public static readonly SuccesfulTransitionData anyData = new();
    public class SuccesfulTransitionData : IStateSpecificTransitionData { public bool ConditionMet => true; }
}



