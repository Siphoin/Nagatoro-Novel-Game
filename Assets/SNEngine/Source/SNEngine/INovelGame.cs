using SNEngine.Services;

namespace SNEngine
{
    public interface INovelGame
    {
        T GetService<T>() where T : ServiceBase;
    }
}