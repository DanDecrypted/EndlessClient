﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using EndlessClient.Game;
using Microsoft.Xna.Framework;

namespace EndlessClient.Controls.ControlSets
{
	public class InitialControlSet : BaseControlSet
	{
		private IGameComponent _createAccount,
							   _login,
							   _viewCredits,
							   _exitGame,
							   _versionInfo;

		//todo: add some sort of picture box control for person one picture

		public override GameStates GameState { get { return GameStates.Initial; } }

		protected override void InitializeControlsHelper(IControlSet currentControlSet)
		{
			_createAccount = GetControl(currentControlSet, GameControlIdentifier.InitialCreateAccount, GetMainCreateAccountButton);
			_login = GetControl(currentControlSet, GameControlIdentifier.InitialLogin, GetMainLoginButton);
			_viewCredits = GetControl(currentControlSet, GameControlIdentifier.InitialViewCredits, GetViewCreditsButton);
			_exitGame = GetControl(currentControlSet, GameControlIdentifier.InitialExitGame, GetExitButton);
			_versionInfo = GetControl(currentControlSet, GameControlIdentifier.InitialVersionLabel, GetVersionInfoLabel);

			_allComponents.Add(_createAccount);
			_allComponents.Add(_login);
			_allComponents.Add(_viewCredits);
			_allComponents.Add(_exitGame);
			_allComponents.Add(_versionInfo);
		}

		public override IGameComponent FindComponentByControlIdentifier(GameControlIdentifier control)
		{
			switch (control)
			{
				case GameControlIdentifier.InitialCreateAccount: return _createAccount;
				case GameControlIdentifier.InitialLogin: return _login;
				case GameControlIdentifier.InitialViewCredits: return _viewCredits;
				case GameControlIdentifier.InitialExitGame: return _exitGame;
				case GameControlIdentifier.InitialVersionLabel: return _versionInfo;
				default: return null;
			}
		}
	}
}