using System.Collections;
using UnityEngine;

namespace CoreGame.FightSystem.UI
{
    public class FightWindow : MonoBehaviour, IFightWindow
    {
        [SerializeField] private FillSlider _healthPlayer;
        [SerializeField] private FillSlider _healthEnemy;

        public void ResetState()
        {
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}