using System;

namespace SNEngine.DialogSystem
{
    public interface IDialogue
    {
        object Name { get; }

        event Action OnEndExecute;

        void Execute();
        bool HasNextDialogueOnExit();
        void Pause();

        void Stop();
    }
}
