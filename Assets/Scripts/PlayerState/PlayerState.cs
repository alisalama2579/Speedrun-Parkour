using UnityEngine;
using System;

public abstract class PlayerState
{
    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void UpdateState();

    public abstract void FixedUpdateState();
}
