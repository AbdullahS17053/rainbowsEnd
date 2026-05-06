namespace RainbowsEnd.Systems.Pooling
{
    public interface IPoolable
    {
        void OnSpawned();

        void OnDespawned();
    }
}