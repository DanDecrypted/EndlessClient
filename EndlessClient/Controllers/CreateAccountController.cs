﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using System;
using System.Threading.Tasks;
using EndlessClient.Dialogs.Actions;
using EndlessClient.GameExecution;
using EOLib.Domain.Account;
using EOLib.Net;
using EOLib.Net.Communication;

namespace EndlessClient.Controllers
{
	public class CreateAccountController : ICreateAccountController
	{
		private readonly ICreateAccountDialogDisplayActions _createAccountDialogDisplayActions;
		private readonly IErrorDialogDisplayAction _errorDisplayAction;
		private readonly IAccountActions _accountActions;
		private readonly IGameStateActions _gameStateActions;
		private readonly ISafeInBandNetworkOperationFactory _networkOperationFactory;

		public CreateAccountController(ICreateAccountDialogDisplayActions createAccountDialogDisplayActions,
									   IErrorDialogDisplayAction errorDisplayAction,
									   IAccountActions accountActions,
									   IGameStateActions gameStateActions,
									   ISafeInBandNetworkOperationFactory networkOperationFactory)
		{
			_createAccountDialogDisplayActions = createAccountDialogDisplayActions;
			_errorDisplayAction = errorDisplayAction;
			_accountActions = accountActions;
			_gameStateActions = gameStateActions;
			_networkOperationFactory = networkOperationFactory;
		}

		public async Task CreateAccount(ICreateAccountParameters createAccountParameters)
		{
			var paramsValidationResult = _accountActions.CheckAccountCreateParameters(createAccountParameters);
			if (paramsValidationResult.FaultingParameter != WhichParameter.None)
			{
				_createAccountDialogDisplayActions.ShowParameterError(paramsValidationResult);
				return;
			}

			var checkNameOperation = _networkOperationFactory.CreateSafeOperation(
				async () => await _accountActions.CheckAccountNameWithServer(createAccountParameters.AccountName),
				SetInitialStateAndShowError,
				SetInitialStateAndShowError);
			if (!await checkNameOperation.Invoke())
				return;

			var nameResult = checkNameOperation.Result;
			if (nameResult != AccountReply.Continue)
			{
				_createAccountDialogDisplayActions.ShowServerError(nameResult);
				return;
			}

			if (!await ShowAccountCreationPendingDialog()) return;

			var createAccountOperation = _networkOperationFactory.CreateSafeOperation(
				async () => await _accountActions.CreateAccount(createAccountParameters),
				SetInitialStateAndShowError,
				SetInitialStateAndShowError);
			if (!await createAccountOperation.Invoke())
				return;

			var accountResult = createAccountOperation.Result;
			if (accountResult != AccountReply.Created)
			{
				_createAccountDialogDisplayActions.ShowServerError(accountResult);
				return;
			}

			_gameStateActions.ChangeToState(GameStates.Initial);
			_createAccountDialogDisplayActions.ShowSuccessMessage();
		}

		private async Task<bool> ShowAccountCreationPendingDialog()
		{
			try
			{
				await _createAccountDialogDisplayActions.ShowAccountCreatePendingDialog();
			}
			catch (OperationCanceledException) { return false; }

			return true;
		}

		private void SetInitialStateAndShowError(NoDataSentException ex)
		{
			_gameStateActions.ChangeToState(GameStates.Initial);
			_errorDisplayAction.ShowException(ex);
		}

		private void SetInitialStateAndShowError(EmptyPacketReceivedException ex)
		{
			_gameStateActions.ChangeToState(GameStates.Initial);
			_errorDisplayAction.ShowException(ex);
		}
	}
}