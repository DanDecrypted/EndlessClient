using EndlessClient.Controllers;
using EndlessClient.ControlSets;
using EndlessClient.Dialogs;
using EndlessClient.GameExecution;
using EndlessClient.HUD;
using EndlessClient.Rendering;
using EOLib.Config;
using EOLib.Domain.Map;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using XNAControls;

namespace EndlessClient.Input
{
    public class UserInputHandler : XNAControl, IUserInputHandler
    {
        private readonly List<IInputHandler> _handlers;
        private readonly IActiveDialogProvider _activeDialogProvider;

        public UserInputHandler(IEndlessGameProvider endlessGameProvider,
                                IUserInputProvider userInputProvider,
                                IUserInputTimeRepository userInputTimeRepository,
                                IMoveKeyController arrowKeyController,
                                IAttackKeyController controlKeyController,
                                IFunctionKeyController functionKeyController,
                                INumPadController numPadController,
                                IHudButtonController hudButtonController,
                                ICurrentMapStateRepository currentMapStateRepository,
                                IActiveDialogProvider activeDialogProvider,
                                IClientWindowSizeProvider clientWindowSizeProvider,
                                IConfigurationProvider configurationProvider,
                                IHudControlProvider hudControlProvider)
        {
            _handlers = new List<IInputHandler>
            {
                new MovementKeyHandler(endlessGameProvider,
                    userInputProvider,
                    userInputTimeRepository,
                    arrowKeyController,
                    currentMapStateRepository,
                    configurationProvider,
                    hudControlProvider),
                new AttackKeyHandler(endlessGameProvider,
                    userInputProvider,
                    userInputTimeRepository,
                    controlKeyController,
                    currentMapStateRepository,
                    configurationProvider,
                    hudControlProvider),
                new FunctionKeyHandler(endlessGameProvider,
                    userInputProvider,
                    userInputTimeRepository,
                    functionKeyController,
                    currentMapStateRepository,
                    hudControlProvider),
                new NumPadHandler(endlessGameProvider,
                    userInputProvider,
                    userInputTimeRepository,
                    currentMapStateRepository,
                    numPadController,
                    hudControlProvider),
            };

            if (clientWindowSizeProvider.Resizable)
            {
                _handlers.Add(new PanelShortcutHandler(endlessGameProvider, userInputProvider, userInputTimeRepository, currentMapStateRepository, hudButtonController, hudControlProvider));
            }

            _activeDialogProvider = activeDialogProvider;
        }

        protected override void OnUpdateControl(GameTime gameTime)
        {
            if (_activeDialogProvider.ActiveDialogs.Any(x => x.HasValue))
                return;

            var timeAtBeginningOfUpdate = DateTime.Now;

            foreach (var handler in _handlers)
                handler.HandleKeyboardInput(timeAtBeginningOfUpdate);

            base.OnUpdateControl(gameTime);
        }
    }

    public interface IUserInputHandler : IGameComponent
    {
    }
}
