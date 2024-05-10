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
    public class AttackKeyHandler : InputHandlerBase
    {
        private readonly IAttackKeyController _attackKeyController;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IHudControlProvider _hudControlProvider;

        public AttackKeyHandler(IEndlessGameProvider endlessGameProvider,
                                 IUserInputProvider userInputProvider,
                                 IUserInputTimeRepository userInputTimeRepository,
                                 IAttackKeyController attackKeyController,
                                 ICurrentMapStateRepository currentMapStateRepository, 
                                 IConfigurationProvider configurationProvider,
                                 IHudControlProvider hudControlProvider)
            : base(endlessGameProvider, userInputProvider, userInputTimeRepository, currentMapStateRepository, hudControlProvider)
        {
            _attackKeyController = attackKeyController;
            _configurationProvider = configurationProvider;
            _hudControlProvider = hudControlProvider;
        }

        protected override Option<Keys> HandleInput()
        {
            var attackKeys = new Keys?[] { Keys.LeftControl, Keys.RightControl };

            var chatBoxEmpty = string.IsNullOrEmpty(_hudControlProvider.GetComponent<ChatTextBox>(HudControlIdentifier.ChatTextBox).Text);
            if (_configurationProvider.UseWasdMovement && chatBoxEmpty)
            {
                attackKeys = attackKeys.Append(Keys.Space).ToArray();
            }

            Keys? attackKeyHeld = attackKeys.FirstOrDefault(x => IsKeyHeld(x.Value));
            if (attackKeyHeld.HasValue && _attackKeyController.Attack())
                return Option.Some(attackKeyHeld.Value);

            return Option.None<Keys>();
        }
    }
}
