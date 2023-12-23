using EndlessClient.Controllers;
using EndlessClient.ControlSets;
using EndlessClient.GameExecution;
using EndlessClient.HUD.Controls;
using EndlessClient.UIControls;
using EOLib.Domain.Map;
using Microsoft.Xna.Framework.Input;
using Optional;

namespace EndlessClient.Input
{
    public class MovementKeyHandler : InputHandlerBase
    {
        private readonly IArrowKeyController _arrowKeyController;
        private readonly IHudControlProvider _hudControlProvider;

        public MovementKeyHandler(IEndlessGameProvider endlessGameProvider,
                               IUserInputProvider userInputProvider,
                               IUserInputTimeRepository userInputTimeRepository,
                               IArrowKeyController arrowKeyController,
                               ICurrentMapStateRepository currentMapStateRepository,
                               IHudControlProvider hudControlProvider)
            : base(endlessGameProvider, userInputProvider, userInputTimeRepository, currentMapStateRepository)
        {
            _arrowKeyController = arrowKeyController;
            this._hudControlProvider = hudControlProvider;
        }

        private ChatTextBox ChatTextBox => _hudControlProvider.GetComponent<ChatTextBox>(HudControlIdentifier.ChatTextBox);

        protected override Option<Keys> HandleInput()
        {
            if ((IsKeyHeld(Keys.Left) || (IsKeyHeld(Keys.A) && string.IsNullOrEmpty(ChatTextBox.Text))) && _arrowKeyController.MoveLeft())
                return Option.Some(Keys.Left);
            if ((IsKeyHeld(Keys.Right) || (IsKeyHeld(Keys.D) && string.IsNullOrEmpty(ChatTextBox.Text))) && _arrowKeyController.MoveRight())
                return Option.Some(Keys.Right);
            if ((IsKeyHeld(Keys.Up) || (IsKeyHeld(Keys.W) && string.IsNullOrEmpty(ChatTextBox.Text))) && _arrowKeyController.MoveUp())
                return Option.Some(Keys.Up);
            if ((IsKeyHeld(Keys.Down) || (IsKeyHeld(Keys.S) && string.IsNullOrEmpty(ChatTextBox.Text))) && _arrowKeyController.MoveDown())
                return Option.Some(Keys.Down);

            return Option.None<Keys>();
        }
    }
}
