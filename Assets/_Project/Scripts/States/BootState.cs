namespace DungeonBuilder.M0
{
    public class BootState : IAppState
    {
        public string Name => "Boot";

        private readonly GameRoot _root;

        public BootState(GameRoot root)
        {
            _root = root;
        }

        public void Enter()
        {
            _root.Logger.Info("Entered BootState.");
            _root.InitializeServicesAndData();
            _root.GoHomeStub();
        }

        public void Exit()
        {
        }

        public void Tick(float dt)
        {
        }
    }
}
