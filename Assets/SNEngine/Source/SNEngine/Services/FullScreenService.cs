using SNEngine.fullScreenSystem.Models;
using UnityEngine;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Full Screen Service")]
    public class FullScreenService : ServiceBase
    {
        public FullScreenData Data => NovelGame.Instance.GetService<UserDataService>().Data.FullScreenData;

        public void SetFullScreen (bool isFullScreen)
        {
            Data.IsOn = isFullScreen;
            Screen.fullScreen = isFullScreen;
        }
    }
}
