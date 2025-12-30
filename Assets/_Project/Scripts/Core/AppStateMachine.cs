using UnityEngine;

namespace DungeonBuilder.M0
{
    public interface IAppState
    {
        string Name { get; }
        void Enter();
        void Exit();
        void Tick(float dt);
    }

    public class AppStateMachine
    {
        private IAppState _current;

        public string CurrentStateName => _current != null ? _current.Name : "None";

        public void SetState(IAppState next)
        {
            if (_current != null)
            {
                _current.Exit();
            }

            _current = next;

            if (_current != null)
            {
                _current.Enter();
            }
        }

        public void Tick(float dt)
        {
            if (_current == null)
            {
                return;
            }

            _current.Tick(dt);
        }
    }
}

