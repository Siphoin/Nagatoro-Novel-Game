
using CoreGame.FightSystem;
using CoreGame.FightSystem.Models;
using CoreGame.FightSystem.UI;
using SNEngine;
using SNEngine.CharacterSystem;
using SNEngine.MainMenuSystem;
using SNEngine.Services;
using SNEngine.Utils;
using System.Collections.Generic;
using UnityEngine;
namespace CoreGame.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Service/New FightService")]
    public class FightService : ServiceBase
    {
        private CharacterService _characterService;
        private Dictionary<Character, CharacterFightData> _currentStatsCharacters;
        private Dictionary<Character, IFightComponent> _fightComponents;
        private IFightWindow _fightWindow;
        private const string FIGHT_WINDOW_VANILLA_PATH = "FightWindow";
        public override void Initialize()
        {
            _characterService = NovelGame.Instance.GetService<CharacterService>();

            var ui = NovelGame.Instance.GetService<UIService>();

            var input = ResourceLoader.LoadCustomOrVanilla<FightWindow>(FIGHT_WINDOW_VANILLA_PATH);

            if (input == null)
            {
                return;
            }

            var prefab = Object.Instantiate(input);

            prefab.name = input.name;

            _fightWindow = prefab;

            ui.AddElementToUIContainer(prefab.gameObject);

            prefab.gameObject.SetActive(false);
        }

        public override void ResetState()
        {
            HideCharacters();
            ClearupFightComponents();
            _currentStatsCharacters = null;
            _fightWindow.ResetState();
        }

        public void TurnFight(FightCharacter playerCharacter, FightCharacter enemyCharacter)
        {
            _currentStatsCharacters = new();
            _fightComponents = new();
            ShowCharacter(playerCharacter.ReferenceCharacter);
            ShowCharacter(enemyCharacter.ReferenceCharacter);
            SetupCharacterForFight(playerCharacter);
            SetupCharacterForFight(enemyCharacter);
            _fightWindow.Show();
        }

        private void SetupCharacterForFight(FightCharacter character)
        {
            CharacterFightData fightData = new(character);
            _currentStatsCharacters[character.ReferenceCharacter] = fightData;

            ICharacterRenderer characterRenderer = _characterService.GetWorldCharacter(character.ReferenceCharacter);
            IFightComponent fightComponent = characterRenderer.AddComponent<FightComponent>();
            fightComponent.AddComponents();
            _fightComponents.Add(character.ReferenceCharacter, fightComponent);
        }

        private void ShowCharacter (Character character)
        {
            _characterService.ShowCharacter(character);
        }

        private void HideCharacter(Character character)
        {
            _characterService.HideCharacter(character);
        }

        private void ClearupFightComponents ()
        {
                foreach (var component in _fightComponents)
                {
                try
                {
                    FightComponent fightComponent = component.Value as FightComponent;
                    UnityEngine.Object.Destroy(fightComponent);
                }
                catch
                {
                    continue;
                }
                }

                _fightComponents.Clear();
                _fightComponents = null;

                
            }

        private void HideCharacters ()
        {
            foreach (var fightComponent in _fightComponents)
            {
                Character character = fightComponent.Key;
                HideCharacter(character);
            }
        }
        }
    }