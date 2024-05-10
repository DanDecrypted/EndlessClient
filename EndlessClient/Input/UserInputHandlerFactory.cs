﻿using AutomaticTypeMapper;
using EndlessClient.Controllers;
using EndlessClient.ControlSets;
using EndlessClient.Dialogs;
using EndlessClient.GameExecution;
using EndlessClient.HUD;
using EndlessClient.Rendering;
using EOLib.Config;
using EOLib.Domain.Map;

namespace EndlessClient.Input
{
    [MappedType(BaseType = typeof(IUserInputHandlerFactory))]
    public class UserInputHandlerFactory : IUserInputHandlerFactory
    {
        private readonly IEndlessGameProvider _endlessGameProvider;
        private readonly IUserInputProvider _userInputProvider;
        private readonly IUserInputTimeRepository _userInputTimeRepository;
        private readonly IMoveKeyController _moveKeyController;
        private readonly IAttackKeyController _attackKeyController;
        private readonly IFunctionKeyController _functionKeyController;
        private readonly INumPadController _numPadController;
        private readonly IHudButtonController _hudButtonController;
        private readonly ICurrentMapStateRepository _currentMapStateRepository;
        private readonly IActiveDialogProvider _activeDialogProvider;
        private readonly IClientWindowSizeProvider _clientWindowSizeProvider;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IHudControlProvider _hudControlProvider;

        public UserInputHandlerFactory(IEndlessGameProvider endlessGameProvider,
                                       IUserInputProvider userInputProvider,
                                       IUserInputTimeRepository userInputTimeRepository,
                                       IMoveKeyController moveKeyController,
                                       IAttackKeyController attackKeyController,
                                       IFunctionKeyController functionKeyController,
                                       INumPadController numPadController,
                                       IHudButtonController hudButtonController,
                                       ICurrentMapStateRepository currentMapStateRepository,
                                       IActiveDialogProvider activeDialogProvider,
                                       IClientWindowSizeProvider clientWindowSizeProvider,
                                       IConfigurationProvider configurationProvider,
                                       IHudControlProvider hudControlProvider)
        {
            _endlessGameProvider = endlessGameProvider;
            _userInputProvider = userInputProvider;
            _userInputTimeRepository = userInputTimeRepository;
            _moveKeyController = moveKeyController;
            _attackKeyController = attackKeyController;
            _functionKeyController = functionKeyController;
            _numPadController = numPadController;
            _hudButtonController = hudButtonController;
            _currentMapStateRepository = currentMapStateRepository;
            _activeDialogProvider = activeDialogProvider;
            _clientWindowSizeProvider = clientWindowSizeProvider;
            _configurationProvider = configurationProvider;
            _hudControlProvider = hudControlProvider;
        }

        public IUserInputHandler CreateUserInputHandler()
        {
            return new UserInputHandler(_endlessGameProvider,
                                        _userInputProvider,
                                        _userInputTimeRepository,
                                        _moveKeyController,
                                        _attackKeyController,
                                        _functionKeyController,
                                        _numPadController,
                                        _hudButtonController,
                                        _currentMapStateRepository,
                                        _activeDialogProvider,
                                        _clientWindowSizeProvider,
                                        _configurationProvider,
                                        _hudControlProvider);
        }
    }

    public interface IUserInputHandlerFactory
    {
        IUserInputHandler CreateUserInputHandler();
    }
}
