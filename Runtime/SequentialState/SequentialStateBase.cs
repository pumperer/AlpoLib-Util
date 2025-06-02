using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace alpoLib.Util
{
    public abstract class SequentialStateBase<TMachineBase> : ISequentialState<TMachineBase>
        where TMachineBase : SequentialStateMachineBase
    {
        public virtual void OnEnter(TMachineBase machine)
        {
        }

        public virtual void OnUpdate(TMachineBase machine)
        {
        }

        public virtual void OnExit(TMachineBase machine)
        {
        }
    }
}