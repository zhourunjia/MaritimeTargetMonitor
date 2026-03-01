using System;

namespace Maritime.Core.State
{
    public class StateManager
    {
        public SystemState CurrentState { get; private set; }

        public event EventHandler<SystemState> StateChanged;

        public StateManager()
        {
            CurrentState = SystemState.Initializing;
        }

        public void ChangeState(SystemState newState)
        {
            if (CurrentState != newState)
            {
                CurrentState = newState;
                StateChanged?.Invoke(this, newState);
            }
        }
    }
}
