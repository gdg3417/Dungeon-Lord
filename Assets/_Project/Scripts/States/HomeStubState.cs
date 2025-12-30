namespace DungeonBuilder.M0
{
    public class HomeStubState : IAppState
    {
        public string Name => "HomeStub";

        private readonly GameRoot _root;

        public HomeStubState(GameRoot root)
        {
            _root = root;
        }

        public void Enter()
        {
            _root.Logger.Info("Entered HomeStubState.");
            _root.Save.lastKnownAppState = Name;
            _root.SaveService.Save(_root.Save, SaveReason.StateChange);
        }

        public void Exit()
        {
        }

        public void Tick(float dt)
        {
        }
    }
}
