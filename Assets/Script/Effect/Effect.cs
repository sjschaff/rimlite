namespace BB
{
    public abstract class Effect
    {
        public readonly Game game;
        protected Effect(Game game) => this.game = game;
        public abstract void Update(float dt);
        public abstract void Destroy();
    }
}
