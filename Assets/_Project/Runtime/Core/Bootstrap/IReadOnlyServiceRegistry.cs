namespace RainbowsEnd.Core.Bootstrap
{
    public interface IReadOnlyServiceRegistry
    {
        bool TryGet<TService>(out TService service) where TService : class;

        TService Get<TService>() where TService : class;
    }
}