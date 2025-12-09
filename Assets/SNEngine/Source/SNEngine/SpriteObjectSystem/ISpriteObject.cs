using SNEngine.Animations;

namespace SNEngine.SpriteObjectSystem
{
    public interface ISpriteObject : IShowable, IHidden, IResetable, IMovableByX, IFadeable, IRotateable, IScaleable, IChangeableColor
    {
    }
}