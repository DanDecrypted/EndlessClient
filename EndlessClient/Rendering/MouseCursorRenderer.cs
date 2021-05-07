﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EndlessClient.Controllers;
using EndlessClient.HUD;
using EndlessClient.Input;
using EOLib;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using EOLib.Domain.Item;
using EOLib.Domain.Map;
using EOLib.Graphics;
using EOLib.IO;
using EOLib.IO.Map;
using EOLib.IO.Pub;
using EOLib.IO.Repositories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using XNAControls;

namespace EndlessClient.Rendering
{
    public class MouseCursorRenderer : XNAControl, IMouseCursorRenderer
    {
        private enum CursorIndex
        {
            Standard = 0,
            HoverNormal = 1,
            HoverItem = 2,
            ClickFirstFrame = 3,
            ClickSecondFrame = 4,
            NumberOfFramesInSheet = 5
        }

        private readonly Rectangle SingleCursorFrameArea;

        private readonly Texture2D _mouseCursorTexture;
        private readonly ICharacterProvider _characterProvider;
        private readonly IRenderOffsetCalculator _renderOffsetCalculator;
        private readonly IMapCellStateProvider _mapCellStateProvider;
        private readonly IItemStringService _itemStringService;
        private readonly IEIFFileProvider _eifFileProvider;
        private readonly ICurrentMapProvider _currentMapProvider;
        private readonly IMapInteractionController _mapInteractionController;
        private readonly IUserInputProvider _userInputProvider;
        private readonly IClientWindowSizeProvider _clientWindowSizeProvider;

        private readonly XNALabel _mapItemText;

        private Rectangle _drawArea;
        private int _gridX, _gridY;
        private CursorIndex _cursorIndex;
        private bool _shouldDrawCursor;

        private DateTime? _startClickTime;
        private CursorIndex _clickFrame;
        private int _clickAlpha;
        private Rectangle _clickPositionArea;

        public MouseCursorRenderer(INativeGraphicsManager nativeGraphicsManager,
                                   ICharacterProvider characterProvider,
                                   IRenderOffsetCalculator renderOffsetCalculator,
                                   IMapCellStateProvider mapCellStateProvider,
                                   IItemStringService itemStringService,
                                   IEIFFileProvider eifFileProvider,
                                   ICurrentMapProvider currentMapProvider,
                                   IMapInteractionController mapInteractionController,
                                   IUserInputProvider userInputProvider,
                                   IClientWindowSizeProvider clientWindowSizeProvider)
        {
            _mouseCursorTexture = nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 24, true);
            _characterProvider = characterProvider;
            _renderOffsetCalculator = renderOffsetCalculator;
            _mapCellStateProvider = mapCellStateProvider;
            _itemStringService = itemStringService;
            _eifFileProvider = eifFileProvider;
            _currentMapProvider = currentMapProvider;
            _mapInteractionController = mapInteractionController;
            _userInputProvider = userInputProvider;
            _clientWindowSizeProvider = clientWindowSizeProvider;
            SingleCursorFrameArea = new Rectangle(0, 0,
                                                  _mouseCursorTexture.Width/(int) CursorIndex.NumberOfFramesInSheet,
                                                  _mouseCursorTexture.Height);
            _drawArea = SingleCursorFrameArea;

            _mapItemText = new XNALabel(Constants.FontSize08pt75)
            {
                Visible = false,
                Text = string.Empty,
                ForeColor = Color.White,
                AutoSize = false,
                DrawOrder = 10 //todo: make a better provider for draw orders (see also HudControlsFactory)
            };
        }

        public override void Initialize()
        {
            _mapItemText.AddControlToDefaultGame();
        }

        #region Update and Helpers

        public override async void Update(GameTime gameTime)
        {
            // prevents updates if there is a dialog
            if (!ShouldUpdate()) return;

            // todo: don't do anything if there is a context menu and mouse is over context menu

            var offsetX = MainCharacterOffsetX();
            var offsetY = MainCharacterOffsetY();

            SetGridCoordsBasedOnMousePosition(offsetX, offsetY);
            UpdateDrawPostionBasedOnGridPosition(offsetX, offsetY);

            var cellState = _mapCellStateProvider.GetCellStateAt(_gridX, _gridY);
            UpdateCursorSourceRectangle(cellState);

            await CheckForClicks(cellState);
        }

        private void SetGridCoordsBasedOnMousePosition(int offsetX, int offsetY)
        {
            //need to solve this system of equations to get x, y on the grid
            //(x * 32) - (y * 32) + 288 - c.OffsetX, => pixX = 32x - 32y + 288 - c.OffsetX
            //(y * 16) + (x * 16) + 144 - c.OffsetY  => 2pixY = 32y + 32x + 288 - 2c.OffsetY
            //                                         => 2pixY + pixX = 64x + 576 - c.OffsetX - 2c.OffsetY
            //                                         => 2pixY + pixX - 576 + c.OffsetX + 2c.OffsetY = 64x
            //                                         => _gridX = (pixX + 2pixY - 576 + c.OffsetX + 2c.OffsetY) / 64; <=
            //pixY = (_gridX * 16) + (_gridY * 16) + 144 - c.OffsetY =>
            //(pixY - (_gridX * 16) - 144 + c.OffsetY) / 16 = _gridY

            var mouseState = _userInputProvider.CurrentMouseState;

            var msX = mouseState.X - SingleCursorFrameArea.Width / 2;
            var msY = mouseState.Y - SingleCursorFrameArea.Height / 2;

            var widthFactor = _clientWindowSizeProvider.Width * 9 / 10; // 288 = 640 * .45, 576 = 640 * .9
            var heightFactor = _clientWindowSizeProvider.Height * 3 / 10;

            _gridX = (int)Math.Round((msX + 2 * msY - widthFactor + offsetX + 2 * offsetY) / 64.0);
            _gridY = (int)Math.Round((msY - _gridX * 16 - heightFactor + offsetY) / 16.0);
        }

        private void UpdateDrawPostionBasedOnGridPosition(int offsetX, int offsetY)
        {
            var drawPosition = GetDrawCoordinatesFromGridUnits(_gridX, _gridY, offsetX, offsetY);
            _drawArea = new Rectangle((int)drawPosition.X,
                                      (int)drawPosition.Y,
                                      _drawArea.Width,
                                      _drawArea.Height);
        }

        private void UpdateCursorSourceRectangle(IMapCellState cellState)
        {
            _shouldDrawCursor = true;
            _cursorIndex = CursorIndex.Standard;
            if (cellState.Character.HasValue || cellState.NPC.HasValue)
                _cursorIndex = CursorIndex.HoverNormal;
            else if (cellState.Sign.HasValue)
                _shouldDrawCursor = false;
            else if (cellState.Items.Any())
            {
                _cursorIndex = CursorIndex.HoverItem;
                UpdateMapItemLabel(new Optional<IItem>(cellState.Items.Last()));
            }
            else if (cellState.TileSpec != TileSpec.None)
                UpdateCursorIndexForTileSpec(cellState.TileSpec);

            if (!cellState.Items.Any())
                UpdateMapItemLabel(Optional<IItem>.Empty);

            if (_startClickTime.HasValue && (DateTime.Now - _startClickTime.Value).TotalMilliseconds > 350)
            {
                _startClickTime = DateTime.Now;
                _clickFrame = _clickFrame + 1;

                if (_clickFrame != CursorIndex.ClickFirstFrame && _clickFrame != CursorIndex.ClickSecondFrame)
                {
                    _clickFrame = CursorIndex.Standard;
                    _startClickTime = null;
                }
            }
        }

        private int MainCharacterOffsetX()
        {
            return _renderOffsetCalculator.CalculateOffsetX(_characterProvider.MainCharacter.RenderProperties);
        }

        private int MainCharacterOffsetY()
        {
            return _renderOffsetCalculator.CalculateOffsetY(_characterProvider.MainCharacter.RenderProperties);
        }

        private Vector2 GetDrawCoordinatesFromGridUnits(int x, int y, int cOffX, int cOffY)
        {
            var widthFactor = _clientWindowSizeProvider.Width * 45 / 100;
            var heightFactor = _clientWindowSizeProvider.Height * 3 / 10;

            return new Vector2(x*32 - y*32 + widthFactor - cOffX, y*16 + x*16 + heightFactor - cOffY);
        }

        private void UpdateMapItemLabel(Optional<IItem> item)
        {
            if (!item.HasValue)
            {
                _mapItemText.Visible = false;
                _mapItemText.Text = string.Empty;
            }
            else if (!_mapItemText.Visible)
            {
                _mapItemText.Visible = true;
                _mapItemText.Text = _itemStringService.GetStringForMapDisplay(
                    _eifFileProvider.EIFFile[item.Value.ItemID], item.Value.Amount);
                _mapItemText.ResizeBasedOnText();
                _mapItemText.ForeColor = GetColorForMapDisplay(_eifFileProvider.EIFFile[item.Value.ItemID]);

                //relative to cursor DrawPosition, since this control is a parent of MapItemText
                _mapItemText.DrawPosition = new Vector2(_drawArea.X + 32 - _mapItemText.ActualWidth/2f,
                                                        _drawArea.Y + -_mapItemText.ActualHeight - 4);
            }
        }

        private void UpdateCursorIndexForTileSpec(TileSpec tileSpec)
        {
            switch (tileSpec)
            {
                case TileSpec.Wall:
                case TileSpec.JammedDoor:
                case TileSpec.MapEdge:
                case TileSpec.FakeWall:
                case TileSpec.NPCBoundary:
                case TileSpec.VultTypo:
                    _shouldDrawCursor = false;
                    break;
                case TileSpec.Chest:
                case TileSpec.BankVault:
                case TileSpec.ChairDown:
                case TileSpec.ChairLeft:
                case TileSpec.ChairRight:
                case TileSpec.ChairUp:
                case TileSpec.ChairDownRight:
                case TileSpec.ChairUpLeft:
                case TileSpec.ChairAll:
                case TileSpec.Board1:
                case TileSpec.Board2:
                case TileSpec.Board3:
                case TileSpec.Board4:
                case TileSpec.Board5:
                case TileSpec.Board6:
                case TileSpec.Board7:
                case TileSpec.Board8:
                case TileSpec.Jukebox:
                    _cursorIndex = CursorIndex.HoverNormal;
                    break;
                case TileSpec.Jump:
                case TileSpec.Water:
                case TileSpec.Arena:
                case TileSpec.AmbientSource:
                case TileSpec.SpikesStatic:
                case TileSpec.SpikesTrap:
                case TileSpec.SpikesTimed:
                case TileSpec.None:
                    _cursorIndex = CursorIndex.Standard;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tileSpec), tileSpec, null);
            }
        }

        //todo: extract this into a service (also used by inventory)
        private static Color GetColorForMapDisplay(EIFRecord record)
        {
            switch (record.Special)
            {
                case ItemSpecial.Lore:
                case ItemSpecial.Unique:
                    return Color.FromNonPremultiplied(0xff, 0xf0, 0xa5, 0xff);
                case ItemSpecial.Rare:
                    return Color.FromNonPremultiplied(0xf5, 0xc8, 0x9c, 0xff);
            }

            return Color.White;
        }

        private async Task CheckForClicks(IMapCellState cellState)
        {
            var currentMouseState = _userInputProvider.CurrentMouseState;
            var previousMouseState = _userInputProvider.PreviousMouseState;

            if (currentMouseState.LeftButton == ButtonState.Released &&
                previousMouseState.LeftButton == ButtonState.Pressed)
            {
                await _mapInteractionController.LeftClickAsync(cellState, this);
            }
            else if (currentMouseState.RightButton == ButtonState.Released &&
                     previousMouseState.RightButton == ButtonState.Pressed)
            {
                _mapInteractionController.RightClick(cellState);
            }
        }

        #endregion

        public void Draw(SpriteBatch spriteBatch, Vector2 additionalOffset)
        {
            //todo: don't draw if context menu is visible and mouse is over the context menu

            if (_shouldDrawCursor && _gridX >= 0 && _gridY >= 0 &&
                _gridX <= _currentMapProvider.CurrentMap.Properties.Width &&
                _gridY <= _currentMapProvider.CurrentMap.Properties.Height)
            {
                spriteBatch.Draw(_mouseCursorTexture,
                                 _drawArea.Location.ToVector2() + additionalOffset,
                                 new Rectangle(SingleCursorFrameArea.Width*(int) _cursorIndex,
                                               0,
                                               SingleCursorFrameArea.Width,
                                               SingleCursorFrameArea.Height),
                                 Color.White);

                if (_startClickTime.HasValue)
                {
                    spriteBatch.Draw(_mouseCursorTexture,
                                     _clickPositionArea.Location.ToVector2() + additionalOffset,
                                     SingleCursorFrameArea.WithPosition(new Vector2(SingleCursorFrameArea.Width * (int)_clickFrame, 0)),
                                     Color.FromNonPremultiplied(255, 255, 255, _clickAlpha-=5));
                }
            }
        }

        public void AnimateClick()
        {
            if (_startClickTime.HasValue)
                return;

            _startClickTime = DateTime.Now;
            _clickFrame = CursorIndex.ClickFirstFrame;
            _clickAlpha = 200;
            _clickPositionArea = _drawArea;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _spriteBatch.Dispose();
                _mapItemText.Dispose();
            }
        }
    }

    public interface IMouseCursorRenderer : IDisposable
    {
        void Initialize();

        void Update(GameTime gameTime);

        void Draw(SpriteBatch spriteBatch, Vector2 additionalOffset);

        void AnimateClick();
    }
}
