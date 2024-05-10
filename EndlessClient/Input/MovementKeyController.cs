using EndlessClient.Controllers;
using EndlessClient.ControlSets;
using EndlessClient.GameExecution;
using EndlessClient.HUD.Controls;
using EndlessClient.UIControls;
using EOLib.Config;
using EOLib.Domain.Map;
using Microsoft.Xna.Framework.Input;
using Optional;
using System.Linq;

namespace EndlessClient.Input
{
    public class MovementKeyController : InputHandlerBase
    {
        private readonly IMoveKeyController moveKeyController;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IHudControlProvider _hudControlProvider;

        public MovementKeyController(IEndlessGameProvider endlessGameProvider,
                               IUserInputProvider userInputProvider,
                               IUserInputTimeRepository userInputTimeRepository,
                               IMoveKeyController arrowKeyController,
                               ICurrentMapStateRepository currentMapStateRepository,
                               IConfigurationProvider configurationProvider,
                               IHudControlProvider hudControlProvider)
            : base(endlessGameProvider, userInputProvider, userInputTimeRepository, currentMapStateRepository, hudControlProvider)
        {
            moveKeyController = arrowKeyController;
            _configurationProvider = configurationProvider;
            _hudControlProvider = hudControlProvider;
        }

        protected override Option<Keys> HandleInput()
        {
            var left = new Keys?[] { Keys.Left };
            var down = new Keys?[] { Keys.Down };
            var right = new Keys?[] { Keys.Right };
            var up = new Keys?[] { Keys.Up };

            var chatBoxEmpty = string.IsNullOrEmpty(_hudControlProvider.GetComponent<ChatTextBox>(HudControlIdentifier.ChatTextBox).Text);
            if (_configurationProvider.UseWasdMovement && chatBoxEmpty)
            {
                left = left.Append(Keys.A).ToArray();
                down = down.Append(Keys.S).ToArray();
                right = right.Append(Keys.D).ToArray();
                up = up.Append(Keys.W).ToArray();
            }

            Keys? leftHeld = left.FirstOrDefault(x => IsKeyHeld(x.Value));
            Keys? downHeld = down.FirstOrDefault(x => IsKeyHeld(x.Value));
            Keys? rightHeld = right.FirstOrDefault(x => IsKeyHeld(x.Value));
            Keys? upHeld = up.FirstOrDefault(x => IsKeyHeld(x.Value));

            if (leftHeld.HasValue && moveKeyController.MoveLeft())
                return Option.Some(leftHeld.Value);

            if (downHeld.HasValue && moveKeyController.MoveDown())
                return Option.Some(downHeld.Value);

            if (rightHeld.HasValue && moveKeyController.MoveRight())
                return Option.Some(rightHeld.Value);

            if (upHeld.HasValue && moveKeyController.MoveUp())
                return Option.Some(upHeld.Value);

            if (KeysAreUp(left.Concat(down).Concat(right).Concat(up).Select(x => x.Value).ToArray()))
                moveKeyController.KeysUp();

            return Option.None<Keys>();
        }
    }
}
