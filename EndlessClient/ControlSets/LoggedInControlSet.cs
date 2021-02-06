﻿using System;
using System.Collections.Generic;
using System.Threading;
using EndlessClient.Controllers;
using EndlessClient.GameExecution;
using EndlessClient.Rendering;
using EndlessClient.UIControls;
using Microsoft.Xna.Framework;
using XNAControls;

namespace EndlessClient.ControlSets
{
    public class LoggedInControlSet : IntermediateControlSet
    {
        private readonly ICharacterInfoPanelFactory _characterInfoPanelFactory;
        private readonly ICharacterManagementController _characterManagementController;
        private readonly IAccountController _accountController;
        private readonly List<CharacterInfoPanel> _characterInfoPanels;

        private IXNAButton _changePasswordButton;

        private int _createRequests, _changePasswordRequests;

        public override GameStates GameState => GameStates.LoggedIn;

        public LoggedInControlSet(KeyboardDispatcher dispatcher,
                                  IMainButtonController mainButtonController,
                                  IClientWindowSizeRepository clientWindowSizeRepository,
                                  ICharacterInfoPanelFactory characterInfoPanelFactory,
                                  ICharacterManagementController characterManagementController,
                                  IAccountController accountController)
            : base(dispatcher, mainButtonController, clientWindowSizeRepository)
        {
            _characterInfoPanelFactory = characterInfoPanelFactory;
            _characterManagementController = characterManagementController;
            _accountController = accountController;
            _characterInfoPanels = new List<CharacterInfoPanel>();
        }

        protected override void InitializeControlsHelper(IControlSet currentControlSet)
        {
            base.InitializeControlsHelper(currentControlSet);

            _changePasswordButton = GetControl(currentControlSet, GameControlIdentifier.ChangePasswordButton, GetPasswordButton);
            _characterInfoPanels.AddRange(_characterInfoPanelFactory.CreatePanels());

            _allComponents.Add(_changePasswordButton);
            _allComponents.AddRange(_characterInfoPanels);
        }

        public override IGameComponent FindComponentByControlIdentifier(GameControlIdentifier control)
        {
            switch (control)
            {
                case GameControlIdentifier.Character1Panel: return _characterInfoPanels[0];
                case GameControlIdentifier.Character2Panel: return _characterInfoPanels[1];
                case GameControlIdentifier.Character3Panel: return _characterInfoPanels[2];
                case GameControlIdentifier.ChangePasswordButton: return _changePasswordButton;
                default: return base.FindComponentByControlIdentifier(control);
            }
        }

        private IXNAButton GetPasswordButton()
        {
            var button = new XNAButton(_secondaryButtonTexture,
                new Vector2(454, 417),
                new Rectangle(0, 120, 120, 40),
                new Rectangle(120, 120, 120, 40));
            button.OnClick += DoChangePassword;
            return button;
        }

        protected override IXNAButton GetCreateButton()
        {
            var button = base.GetCreateButton();
            button.OnClick += DoCreateCharacter;
            return button;
        }

        protected override void DoBackButtonClick(object sender, EventArgs e)
        {
            _mainButtonController.GoToInitialStateAndDisconnect();
        }

        private async void DoCreateCharacter(object sender, EventArgs e)
        {
            if (Interlocked.Increment(ref _createRequests) != 1)
                return;

            try
            {
                await _characterManagementController.CreateCharacter();
            }
            finally
            {
                Interlocked.Exchange(ref _createRequests, 0);
            }
        }

        private async void DoChangePassword(object sender, EventArgs e)
        {
            if (Interlocked.Increment(ref _changePasswordRequests) != 1)
                return;

            try
            {
                await _accountController.ChangePassword();
            }
            finally
            {
                Interlocked.Exchange(ref _changePasswordRequests, 0);
            }
        }
    }
}
