using SNEngine.InputFormSystem;
using UnityEngine;
using SNEngine.Debugging;
using System.Linq;
using UnityEngine.Events;
using SharpYaml.Serialization;
using SNEngine.Utils;
using Object = UnityEngine.Object;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Input Form Service")]
    public class InputFormService : ServiceBase, IHidden, ISubmitter, IResetable
    {
        public event UnityAction<string> OnSubmit;

        private IInputForm[] _forms;
        private IInputForm _activeForm;
        private const string FORMS_VANILLA_PATH = "UI";


        public override void Initialize()
        {
            var forms = ResourceLoader.LoadAllCustomizable<InputForm>(FORMS_VANILLA_PATH);

            if (forms == null || forms.Length == 0)
            {
                NovelGameDebug.LogError("No Input Forms were loaded.");

                return;
            }

            var uiService = NovelGame.Instance.GetService<UIService>();

            _forms = new IInputForm[forms.Length];

            for (int i = 0; i < forms.Length; i++)
            {
                var form = Object.Instantiate(forms[i]);

                _forms[i] = form;

                uiService.AddElementToUIContainer(form.gameObject);
            }


            NovelGameDebug.Log($"loaded {_forms.Length} {nameof(InputForm)}s");

            ResetState();


        }

        public void Show(InputFormType type, string label, bool isTriming)
        {

            var form = _forms.SingleOrDefault(x => x.Type == type);

            if (form is null)
            {
                NovelGameDebug.LogError($"input form with type {type} not found on service {GetType().Name}");

                return;
            }

            form.Label = label;
            form.IsTrimming = isTriming;

            form.Show();

            _activeForm = form;

            _activeForm.OnSubmit += OnSumbitText;
        }

        private void OnSumbitText(string text)
        {
            _activeForm.OnSubmit -= OnSumbitText;

            OnSubmit?.Invoke(text);
        }

        public void Hide()
        {
            _activeForm?.Hide();
            _activeForm = null;
        }

        public override void ResetState()
        {
            for (int i = 0; i < _forms.Length; i++)
            {
                _forms[i].ResetState();
            }
        }
    }
}