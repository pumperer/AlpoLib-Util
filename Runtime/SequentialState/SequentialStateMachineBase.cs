using System.Collections.Generic;

namespace alpoLib.Util
{
    public abstract class SequentialStateMachineBase
    {
        public Queue<ISequentialState> StatesQueue { get; } = new();
        public ISequentialState CurrentState { get; private set; }

        public virtual void DoNextState()
        {
            if (CurrentState == null)
                OnStartMachine();
            
            CurrentState?.OnExit(this);

            if (StatesQueue.Count <= 0)
            {
                CurrentState = null;
                OnEndMachine();
                return;
            }

            CurrentState = StatesQueue.Dequeue();
            CurrentState?.OnEnter(this);
        }

        public virtual void OnUpdate()
        {
            CurrentState?.OnUpdate(this);
        }

        protected virtual void OnStartMachine()
        {
            
        }

        protected virtual void OnEndMachine()
        {
            
        }

        public virtual void AddState(ISequentialState state)
        {
            StatesQueue.Enqueue(state);
        }

        public virtual void ClearState()
        {
            StatesQueue.Clear();
            CurrentState = null;
        }
    }
}