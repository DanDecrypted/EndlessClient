﻿using System;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using EOLib;
using XNAControls;

namespace EndlessClient
{
	public partial class EOGame
	{
		int charDeleteWarningShown = -1; //index of the character that we've shown a warning about deleting, set to -1 for no warning shown

		readonly GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;

		XNATextBox loginUsernameTextbox;
		XNATextBox loginPasswordTextbox;
		readonly Texture2D[] textBoxTextures = new Texture2D[4];

		public KeyboardDispatcher Dispatcher { get; private set; }

		readonly XNAButton[] mainButtons = new XNAButton[4];
		readonly XNAButton[] loginButtons = new XNAButton[2];
		readonly XNAButton[] createButtons = new XNAButton[2];

		readonly XNAButton[] loginCharButtons = new XNAButton[3];
		readonly XNAButton[] deleteCharButtons = new XNAButton[3];

		XNAButton passwordChangeBtn;

		XNAButton backButton;

		XNALabel lblCredits, lblVersionInfo;

		readonly XNATextBox[] accountCreateTextBoxes = new XNATextBox[6];

		public HUD Hud { get; private set; }

		private void InitializeControls(bool reinit = false)
		{
			//set up text boxes for login
			textBoxTextures[0] = Content.Load<Texture2D>("tbBack");
			textBoxTextures[1] = Content.Load<Texture2D>("tbLeft");
			textBoxTextures[2] = Content.Load<Texture2D>("tbRight");
			textBoxTextures[3] = Content.Load<Texture2D>("cursor");

			loginUsernameTextbox = new XNATextBox(new Rectangle(402, 322, 140, textBoxTextures[0].Height), textBoxTextures, "Microsoft Sans Serif", 8.0f)
			{
				MaxChars = 16,
				DefaultText = "Username",
				LeftPadding = 4
			};
			loginUsernameTextbox.OnTabPressed += OnTabPressed;
			loginUsernameTextbox.OnClicked += OnTextClicked;
			loginUsernameTextbox.OnEnterPressed += (s, e) => MainButtonPress(loginButtons[0], e);
			Dispatcher.Subscriber = loginUsernameTextbox;

			loginPasswordTextbox = new XNATextBox(new Rectangle(402, 358, 140, textBoxTextures[0].Height), textBoxTextures, "Microsoft Sans Serif", 8.0f)
			{
				MaxChars = 12,
				PasswordBox = true,
				LeftPadding = 4,
				DefaultText = "Password"
			};
			loginPasswordTextbox.OnTabPressed += OnTabPressed;
			loginPasswordTextbox.OnClicked += OnTextClicked;
			loginPasswordTextbox.OnEnterPressed += (s, e) => MainButtonPress(loginButtons[0], e);

			//set up primary four login buttons
			Texture2D mainButtonSheet = GFXLoader.TextureFromResource(GFXTypes.PreLoginUI, 13, true);
			for (int i = 0; i < mainButtons.Length; ++i)
			{
				int widthFactor = mainButtonSheet.Width / 2; //2: mouseOut and mouseOver textures
				int heightFactor = mainButtonSheet.Height / mainButtons.Length; //1 row per button
				Rectangle outSource = new Rectangle(0, i * heightFactor, widthFactor, heightFactor);
				Rectangle overSource = new Rectangle(widthFactor, i * heightFactor, widthFactor, heightFactor);
				mainButtons[i] = new XNAButton(mainButtonSheet, new Vector2(26, 278 + i * 40), outSource, overSource);
				mainButtons[i].OnClick += MainButtonPress;
			}

			//the button in the top-right for going back a screen
			Texture2D back = GFXLoader.TextureFromResource(GFXTypes.PreLoginUI, 24, true);
			backButton = new XNAButton(back, new Vector2(589, 0), new Rectangle(0, 0, back.Width, back.Height / 2),
				new Rectangle(0, back.Height / 2, back.Width, back.Height / 2));
			backButton.OnClick += MainButtonPress;
			backButton.ClickArea = new Rectangle(4, 16, 16, 16);

			//Login/Cancel buttons for logging in
			Texture2D smallButtonSheet = GFXLoader.TextureFromResource(GFXTypes.PreLoginUI, 15, true);
			loginButtons[0] = new XNAButton(smallButtonSheet, new Vector2(361, 389), new Rectangle(0, 0, 91, 29), new Rectangle(91, 0, 91, 29));
			loginButtons[1] = new XNAButton(smallButtonSheet, new Vector2(453, 389), new Rectangle(0, 29, 91, 29), new Rectangle(91, 29, 91, 29));
			loginButtons[0].OnClick += MainButtonPress;
			loginButtons[1].OnClick += MainButtonPress;

			//6 text boxes (by default) for creating a new account.
			for (int i = 0; i < accountCreateTextBoxes.Length; ++i)
			{
				//holy fuck! magic numbers!
				//basically, set the first  3 Y coord to start at 69  and move up by 51 each time
				//			 set the second 3 Y coord to start at 260 and move up by 51 each time
				int txtYCoord = (i < 3 ? 69 : 260) + (i < 3 ? i * 51 : (i - 3) * 51);
				XNATextBox txt = new XNATextBox(new Rectangle(358, txtYCoord, 240, textBoxTextures[0].Height), textBoxTextures, "Latha");

				switch (i)
				{
					case 0:
						txt.MaxChars = 16;
						break;
					case 1:
					case 2:
						txt.PasswordBox = true;
						txt.MaxChars = 12;
						break;
					default:
						txt.MaxChars = 35;
						break;
				}

				txt.DefaultText = " ";

				txt.OnTabPressed += OnTabPressed;
				txt.OnClicked += OnTextClicked;
				accountCreateTextBoxes[i] = txt;
			}


			//create account / cancel
			Texture2D secondaryButtons = GFXLoader.TextureFromResource(GFXTypes.PreLoginUI, 14, true);
			createButtons[0] = new XNAButton(secondaryButtons, new Vector2(359, 417), new Rectangle(0, 0, 120, 40), new Rectangle(120, 0, 120, 40));
			createButtons[1] = new XNAButton(secondaryButtons, new Vector2(481, 417), new Rectangle(0, 40, 120, 40), new Rectangle(120, 40, 120, 40));
			createButtons[0].OnClick += MainButtonPress;
			createButtons[1].OnClick += MainButtonPress;

			passwordChangeBtn = new XNAButton(secondaryButtons, new Vector2(454, 417), new Rectangle(0, 120, 120, 40), new Rectangle(120, 120, 120, 40));
			passwordChangeBtn.OnClick += MainButtonPress;

			lblCredits = new XNALabel(new Rectangle(300, 260, 1, 1))
			{
				Text = @"Endless Online - C# Client
Developed by Ethan Moffat
Based on Endless Online --
Copyright Vult-R

Thanks to :
--Sausage for eoserv + C# EO libs
--eoserv.net community
--Hotdog for Eodev client"
			};

			lblVersionInfo = new XNALabel(new Rectangle(30, 457, 1, 1))
			{
				Text =
					string.Format("{0}.{1:000}.{2:000} - {3}:{4}", Constants.MajorVersion, Constants.MinorVersion,
						Constants.ClientVersion, host, port),
				Font = new System.Drawing.Font("Microsoft Sans Serif", 7.0f),
				ForeColor = System.Drawing.Color.FromArgb(0xFF, 0xb4, 0xa0, 0x8c)
			};

			//login/delete buttons for each character
			for (int i = 0; i < 3; ++i)
			{
				loginCharButtons[i] = new XNAButton(smallButtonSheet, new Vector2(495, 93 + i * 124), new Rectangle(0, 58, 91, 29), new Rectangle(91, 58, 91, 29));
				loginCharButtons[i].OnClick += CharModButtonPress;
				deleteCharButtons[i] = new XNAButton(smallButtonSheet, new Vector2(495, 121 + i * 124), new Rectangle(0, 87, 91, 29), new Rectangle(91, 87, 91, 29));
				deleteCharButtons[i].OnClick += CharModButtonPress;
			}

			//hide all the components to start with
			foreach (IGameComponent iGameComp in Components)
			{
				DrawableGameComponent component = iGameComp as DrawableGameComponent;
				//don't hide dialogs if reinitializing
				if (reinit && (XNAControl.Dialogs.Contains(component as XNAControl) || 
					(component as XNAControl != null && XNAControl.Dialogs.Contains((component as XNAControl).TopParent))))
					continue;

				//...except for the four main buttons
				if (component != null && !mainButtons.Contains(component as XNAButton))
					component.Visible = false;
			}
			lblVersionInfo.Visible = true;

#if DEBUG
			//testinggame will login as testuser and login as the first character
			XNAButton testingame = new XNAButton(new Vector2(5, 5), "in-game");
			testingame.OnClick += (s, e) => new Thread(() =>
			{
				MainButtonPress(mainButtons[1], e); //press login
				Thread.Sleep(500);
				if (!World.Instance.Client.ConnectedAndInitialized)
					return;
				loginUsernameTextbox.Text = "testuser";
				loginPasswordTextbox.Text = "testuser";

				MainButtonPress(loginButtons[0], e); //login as acc testuser
				Thread.Sleep(500);
				CharModButtonPress(loginCharButtons[0], e); //login as char testuser
			}).Start();
#endif
		}

		//Pretty much controls how states transition between one another
		private void MainButtonPress(object sender, EventArgs e)
		{
			if (!IsActive)
				return;

			//switch on sender
			if (sender == mainButtons[0])
			{
				//try connect
				//if successful go to account creation state
				TryConnectToServer(() =>
				{
					doStateChange(GameStates.CreateAccount);

					EOScrollingDialog createAccountDlg = new EOScrollingDialog("");
					string message = "It is very important that you enter your correct, real name, location and email address when creating an account. ";
					message += "Our system will ask you to enter your real name and email address in case you have forgotten your password.\n\n";
					message += "A lot of players who forgot their password, and signed up using fake details, have been unsuccessful in gaining access to their account. ";
					message += "So please do not make the same mistake; use real details to sign up for an account.\n\n";
					message += "Your information will only be used for recovering lost passwords. Your privacy is important to us.";
					createAccountDlg.MessageText = message;
				});
			}
			else if (sender == mainButtons[1])
			{
				//try connect
				//if successful go to account login state
				TryConnectToServer(() => doStateChange(GameStates.Login));
			}
			else if (sender == mainButtons[2])
			{
				currentState = GameStates.ViewCredits;
			}
			else if (sender == mainButtons[3])
			{
				if (World.Instance.Client.ConnectedAndInitialized)
					World.Instance.Client.Disconnect();
				Exit();
			}
			else if ((sender == backButton && currentState != GameStates.PlayingTheGame) || sender == createButtons[1] || sender == loginButtons[1])
			{
				Dispatcher.Subscriber = null;
				LostConnectionDialog();
				//disabled warning: in case I add code later below, need to remember that this should immediately return
// ReSharper disable once RedundantJumpStatement
				return;
			}
			else if (sender == backButton && currentState == GameStates.PlayingTheGame)
			{
				EODialog dlg = new EODialog("Are you sure you want to exit the game?", "Exit game", XNADialogButtons.OkCancel, true);
				dlg.DialogClosing += (ss, ee) =>
					{
						if(ee.Result == XNADialogResult.OK)
						{
							Dispatcher.Subscriber = null;
							World.Instance.ResetGameElements();
							if (World.Instance.Client.ConnectedAndInitialized)
								World.Instance.Client.Disconnect();
							doStateChange(GameStates.Initial);
						}
					};
			}
			else if (sender == loginButtons[0])
			{
				if (loginUsernameTextbox.Text == "" || loginPasswordTextbox.Text == "")
					return;

				if (!Handlers.Login.LoginRequest(loginUsernameTextbox.Text, loginPasswordTextbox.Text))
				{
					LostConnectionDialog();
					return;
				}

				if (!Handlers.Login.CanProceed)
				{
					string caption, msg = Handlers.Login.ResponseMessage(out caption);
					EODialog errDlg = new EODialog(msg, caption);
					return;
				}

				doStateChange(GameStates.LoggedIn);
			}
			else if (sender == createButtons[0])
			{
				switch (currentState)
				{
					case GameStates.CreateAccount:
					{
						if (accountCreateTextBoxes.Any(txt => txt.Text.Length == 0))
						{
							EODialog errDlg = new EODialog("Some of the fields are still empty. Fill in all the fields and try again.", "Wrong input");
							return;
						}

						if (accountCreateTextBoxes[1].Text != accountCreateTextBoxes[2].Text)
						{
							//Make sure passwords match
							EODialog errDlg = new EODialog("The two passwords you provided are not the same, please try again.", "Wrong password");
							return;
						}

						if (accountCreateTextBoxes[1].Text.Length < 6)
						{
							//Make sure passwords are good enough
							EODialog errDlg = new EODialog("For your own safety use a longer password (try 6 or more characters)", "Wrong password");
							return;
						}

						if (!System.Text.RegularExpressions.Regex.IsMatch(accountCreateTextBoxes[5].Text, //filter emails using regex
							@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+(?:[A-Z]{2}|com|org|net|edu|gov|mil|biz|info|mobi|name|aero|asia|jobs|museum)\b"))
						{
							EODialog errDlg = new EODialog("Enter a valid email address.", "Wrong input");
							return;
						}

						if (!Handlers.Account.AccountCheckName(accountCreateTextBoxes[0].Text))
						{
							LostConnectionDialog();
							return;
						}

						if (!Handlers.Account.CanProceed)
						{
							string caption, msg = Handlers.Account.ResponseMessage(out caption);
							EODialog errDlg = new EODialog(msg, caption);
							return;
						}

						//show progress bar for account creation pending and THEN create the account
						EOProgressDialog dlg = new EOProgressDialog("Please wait a few minutes for creation.", "Account accepted");
						dlg.DialogClosing += (dlg_S, dlg_E) =>
						{
							if (dlg_E.Result != XNADialogResult.NO_BUTTON_PRESSED) return;

							if (!Handlers.Account.AccountCreate(accountCreateTextBoxes[0].Text,
								accountCreateTextBoxes[1].Text,
								accountCreateTextBoxes[3].Text,
								accountCreateTextBoxes[4].Text,
								accountCreateTextBoxes[5].Text))
							{
								LostConnectionDialog();
								return;
							}

							string _caption, _msg = Handlers.Account.ResponseMessage(out _caption);
							if (!Handlers.Account.CanProceed)
							{
								EODialog errDlg = new EODialog(_msg, _caption);
								return;
							}

							doStateChange(GameStates.Initial);
							EODialog success = new EODialog(_msg, _caption);
						};

					}
						break;
					case GameStates.LoggedIn:
					{
						//Character_request: show create character dialog
						//Character_create: clicked ok in create character dialog
						if (!Handlers.Character.CharacterRequest())
						{
							LostConnectionDialog();
							return;
						}

						if (!Handlers.Character.CanProceed)
						{
							EODialog errDlg = new EODialog("Server is not allowing you to create a character right now. This could be a bug.", "Server error");
							return;
						}

						EOCreateCharacterDialog createCharacter = new EOCreateCharacterDialog(textBoxTextures[3], Dispatcher);
						createCharacter.DialogClosing += (dlg_S, dlg_E) =>
						{
							if (dlg_E.Result != XNADialogResult.OK) return;

							if (!Handlers.Character.CharacterCreate(createCharacter.Gender, createCharacter.HairType, createCharacter.HairColor, createCharacter.SkinColor, createCharacter.Name))
							{
								doStateChange(GameStates.Initial);
								EODialog errDlg = new EODialog("The connection to the game server was lost, please try again at a later time.", "Lost connection");
								if (World.Instance.Client.ConnectedAndInitialized)
									World.Instance.Client.Disconnect();
								return;
							}

							if (!Handlers.Character.CanProceed)
							{
								if (!Handlers.Character.TooManyCharacters)
									dlg_E.CancelClose = true;
								string caption, message = Handlers.Character.ResponseMessage(out caption);
								EODialog fail = new EODialog(message, caption);
								return;
							}

							EODialog dlg = new EODialog("Your character has been created and is ready to explore a new world.", "Character created");
							doShowCharacters();
						};
					}
						break;
				}
			}
			else if (sender == passwordChangeBtn)
			{
				EOChangePasswordDialog dlg = new EOChangePasswordDialog(textBoxTextures[3], Dispatcher);
				dlg.DialogClosing += (dlg_S, dlg_E) =>
				{
					if (dlg_E.Result != XNADialogResult.OK) return;

					if (!Handlers.Account.AccountChangePassword(dlg.Username, dlg.OldPassword, dlg.NewPassword))
					{
						doStateChange(GameStates.Initial);
						EODialog errDlg = new EODialog("The connection to the game server was lost, please try again at a later time.", "Lost connection");
						if (World.Instance.Client.ConnectedAndInitialized)
							World.Instance.Client.Disconnect();
						return;
					}

					string caption, msg = Handlers.Account.ResponseMessage(out caption);
					EODialog response = new EODialog(msg, caption);

					if (Handlers.Account.CanProceed) return;
					dlg_E.CancelClose = true;
				};
			}
		}

		private void CharModButtonPress(object sender, EventArgs e)
		{
			//click delete once: pop up initial dialog, set that initial dialog has been shown
			//Character_take: delete clicked, then dialog pops up
			//Character_remove: click ok in yes/no dialog

			//click login: send WELCOME_REQUEST, get WELCOME_REPLY
			//Send WELCOME_AGREE for map/pubs if needed
			//Send WELCOME_MSG, get WELCOME_REPLY
			//log in if all okay

			int index;
			if (loginCharButtons.Contains(sender))
			{
				index = loginCharButtons.ToList().FindIndex(x => x == sender);
				if (World.Instance.MainPlayer.CharData.Length <= index)
					return;

				if (!Handlers.Welcome.SelectCharacter(World.Instance.MainPlayer.CharData[index].id))
				{
					LostConnectionDialog();
					return;
				}

				//shows the connecting window
				EOConnectingDialog dlg = new EOConnectingDialog();
				dlg.DialogClosing += (dlgS, dlgE) =>
				{
					switch (dlgE.Result)
					{
						case XNADialogResult.OK:
							doStateChange(GameStates.PlayingTheGame);
							break;
						case XNADialogResult.NO_BUTTON_PRESSED:
						{
							EODialog dlg2 = new EODialog("Login Failed.", "Error");
							if (World.Instance.Client.ConnectedAndInitialized)
								World.Instance.Client.Disconnect();
							doStateChange(GameStates.Initial);
						}
							break;
					}
				};
			}
			else if (deleteCharButtons.Contains(sender))
			{
				index = deleteCharButtons.ToList().FindIndex(x => x == sender);
				if (World.Instance.MainPlayer.CharData.Length <= index)
					return;

				if (charDeleteWarningShown != index)
				{
					EODialog warn = new EODialog("Character \'" + World.Instance.MainPlayer.CharData[index].name + "\' is going to be deleted. Delete again to confirm.", "Delete character");
					charDeleteWarningShown = index;
					return;
				}

				//delete character at that index, if it exists
				if (!Handlers.Character.CharacterTake(World.Instance.MainPlayer.CharData[index].id))
				{
					LostConnectionDialog();
					return;
				}

				if (Handlers.Character.CharacterTakeID != World.Instance.MainPlayer.CharData[index].id)
				{
					EODialog warn = new EODialog("The server did not respond properly for deleting the character. Try again.", "Server error");
					return;
				}

				EODialog promptDialog = new EODialog("Character \'" + World.Instance.MainPlayer.CharData[index].name + "\' is going to be deleted. Are you sure?", "Delete character", XNADialogButtons.OkCancel);
				promptDialog.DialogClosing += (dlgS, dlgE) =>
				{
					if (dlgE.Result == XNADialogResult.OK) //user clicked ok to delete their character. do the delete here.
					{
						if (!Handlers.Character.CharacterRemove(World.Instance.MainPlayer.CharData[index].id))
						{
							LostConnectionDialog();
							return;
						}

						doShowCharacters();
					}
				};
			}
		}

		private void OnTabPressed(object sender, EventArgs e)
		{
			if (!IsActive)
				return;
			//for loginClickedGameState
			switch (currentState)
			{
				case GameStates.Login:
					if (sender == loginUsernameTextbox)
					{
						loginUsernameTextbox.Selected = false;
						Dispatcher.Subscriber = loginPasswordTextbox;
						loginPasswordTextbox.Selected = true;
					}
					else
					{
						loginUsernameTextbox.Selected = true;
						Dispatcher.Subscriber = loginUsernameTextbox;
						loginPasswordTextbox.Selected = false;
					}
					break;
				case GameStates.CreateAccount:
					for (int i = 0; i < accountCreateTextBoxes.Length; ++i)
					{
						if (sender == accountCreateTextBoxes[i])
						{
							accountCreateTextBoxes[i].Selected = false;
							int next = (i == accountCreateTextBoxes.Length - 1) ? 0 : i + 1;
							Dispatcher.Subscriber = accountCreateTextBoxes[next];
							accountCreateTextBoxes[next].Selected = true;
							break;
						}
					}
					break;
			}
		}

		private void OnTextClicked(object sender, EventArgs e)
		{
			switch (currentState)
			{
				case GameStates.Login:
					if (sender == loginUsernameTextbox)
					{
						OnTabPressed(loginPasswordTextbox, null);
					}
					else if (sender == loginPasswordTextbox)
					{
						OnTabPressed(loginUsernameTextbox, null);
					}
					break;
				case GameStates.CreateAccount:
					for (int i = 0; i < accountCreateTextBoxes.Length; ++i)
					{
						if (sender == accountCreateTextBoxes[i])
						{
							int prev = (i == 0) ? accountCreateTextBoxes.Length - 1 : i - 1;
							OnTabPressed(accountCreateTextBoxes[prev], null);
							break;
						}
					}
					break;
			}
		}
	}
}
