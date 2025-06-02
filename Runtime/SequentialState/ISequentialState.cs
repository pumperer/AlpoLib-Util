using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace alpoLib.Util
{
    public interface ISequentialState
    {
        void OnEnter(object machineBase);
        void OnUpdate(object machineBase);
        void OnExit(object machineBase);
    }

    public interface ISequentialState<in TMachineBase> : ISequentialState
        where TMachineBase : SequentialStateMachineBase
    {
        void ISequentialState.OnEnter(object machineBase)
        {
            Debug.Log($"{GetType().Name}.OnEnter");
            OnEnter(machineBase as TMachineBase);
        }
        
        void ISequentialState.OnUpdate(object machineBase)
        {
            OnUpdate(machineBase as TMachineBase);
        }
        
        void ISequentialState.OnExit(object machineBase)
        {
            Debug.Log($"{GetType().Name}.OnExit");
            OnExit(machineBase as TMachineBase);
        }
        
        void OnEnter(TMachineBase machine);
        void OnUpdate(TMachineBase machine);
        void OnExit(TMachineBase machine);
    }
}