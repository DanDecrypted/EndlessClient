﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using System.Linq;
using EndlessClient.Rendering.CharacterProperties;
using EndlessClient.Rendering.Sprites;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using EOLib.Graphics;
using EOLib.IO.Repositories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.Rendering
{
    public class CharacterRenderer : DrawableGameComponent, ICharacterRenderer
    {
        private readonly INativeGraphicsManager _graphicsManager;
        private readonly IEIFFileProvider _eifFileProvider;
        private readonly ICharacterProvider _characterProvider;
        private readonly ICharacterRenderOffsetCalculator _characterRenderOffsetCalculator;
        private readonly ICharacterPropertyRendererBuilder _characterPropertyRendererBuilder;

        private ICharacterSpriteCalculator _characterSpriteCalculator;
        private ICharacterRenderProperties _characterRenderPropertiesPrivate, _lastRenderProperties;
        private ICharacterTextures _characterTextures;
        private bool _textureUpdateRequired;

        private SpriteBatch _sb;
        private RenderTarget2D _charRenderTarget;

        public ICharacterRenderProperties RenderProperties
        {
            get { return _characterRenderPropertiesPrivate; }
            set
            {
                if (_characterRenderPropertiesPrivate == value) return;
                _characterRenderPropertiesPrivate = value;
                _textureUpdateRequired = true;
            }
        }

        public Rectangle DrawArea { get; private set; }

        public int TopPixel { get; private set; }

        public CharacterRenderer(Game game,
                                 INativeGraphicsManager graphicsManager,
                                 IEIFFileProvider eifFileProvider,
                                 ICharacterProvider characterProvider,
                                 ICharacterRenderOffsetCalculator characterRenderOffsetCalculator,
                                 ICharacterPropertyRendererBuilder characterPropertyRendererBuilder,
                                 ICharacterRenderProperties renderProperties)
            : base(game)
        {
            _graphicsManager = graphicsManager;
            _eifFileProvider = eifFileProvider;
            _characterProvider = characterProvider;
            _characterRenderOffsetCalculator = characterRenderOffsetCalculator;
            _characterPropertyRendererBuilder = characterPropertyRendererBuilder;
            RenderProperties = renderProperties;
        }

        #region Game Component

        public override void Initialize()
        {
            _charRenderTarget = new RenderTarget2D(Game.GraphicsDevice,
                Game.GraphicsDevice.PresentationParameters.BackBufferWidth,
                Game.GraphicsDevice.PresentationParameters.BackBufferHeight,
                false,
                SurfaceFormat.Color,
                DepthFormat.None);
            _sb = new SpriteBatch(Game.GraphicsDevice);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            ReloadTextures();
            FigureOutTopPixel();

            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            if (!Game.IsActive || !Visible)
                return;

            if (RenderProperties != _lastRenderProperties && RenderProperties.IsActing(CharacterActionState.Walking))
                SetGridCoordinatePosition();

            if (_textureUpdateRequired)
            {
                ReloadTextures();
                DrawToRenderTarget();

                _textureUpdateRequired = false;
            }

            _lastRenderProperties = RenderProperties;

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || _sb.IsDisposed)
                return;

            //todo: check if this is the renderer for the main player
            //        if hidden, draw if: they are not active character and active character is admin

            _sb.Begin();
            DrawToSpriteBatch(_sb);
            
            if (_sb.IsDisposed)
                return;
            _sb.End();

            //todo: draw effect over character

            base.Draw(gameTime);
        }

        #endregion

        #region ICharacterRenderer

        public void SetAbsoluteScreenPosition(int xPosition, int yPosition)
        {
            var skinRect = _characterTextures.Skin.SourceRectangle;
            DrawArea = new Rectangle(xPosition, yPosition, skinRect.Width, skinRect.Height);
            _textureUpdateRequired = true;
        }

        public void DrawToSpriteBatch(SpriteBatch spriteBatch)
        {
            _sb.Draw(_charRenderTarget, Vector2.Zero, GetAlphaColor());
        }

        #endregion

        #region Texture Loading Helpers

        private void FigureOutTopPixel()
        {
            var spriteForSkin = _characterSpriteCalculator.GetSkinTexture(isBow: false);
            var skinData = spriteForSkin.GetSourceTextureData<Color>();

            int i = 0;
            while (i < skinData.Length && skinData[i].A == 0) i++;

            var firstPixelHeight = i == skinData.Length - 1 ? 0 : i/spriteForSkin.SourceRectangle.Height;
            var genderOffset = RenderProperties.Gender == 0 ? 12 : 13;

            TopPixel = genderOffset + firstPixelHeight;
        }

        private void ReloadTextures()
        {
            _characterSpriteCalculator = new CharacterSpriteCalculator(_graphicsManager, _characterRenderPropertiesPrivate);
            _characterTextures = new CharacterTextures(_eifFileProvider, _characterSpriteCalculator, _characterRenderPropertiesPrivate);

            _characterTextures.Refresh();
        }

        #endregion

        #region Drawing Helpers

        private void DrawToRenderTarget()
        {
            GraphicsDevice.SetRenderTarget(_charRenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            var characterPropertyRenderers = _characterPropertyRendererBuilder
                .BuildList(_characterTextures, RenderProperties)
                .Where(x => x.CanRender);
            foreach (var renderer in characterPropertyRenderers)
                renderer.Render(_sb, DrawArea);

            _sb.End();
            GraphicsDevice.SetRenderTarget(null);
        }

        private Color GetAlphaColor()
        {
            return RenderProperties.IsHidden || RenderProperties.IsDead
                ? Color.FromNonPremultiplied(255, 255, 255, 128)
                : Color.White;
        }

        private void SetGridCoordinatePosition()
        {
            //todo: the constants here should be dynamically configurable to support window resizing
            var screenX = _characterRenderOffsetCalculator.CalculateOffsetX(RenderProperties) + 304 - GetMainCharacterOffsetX();
            var screenY = _characterRenderOffsetCalculator.CalculateOffsetY(RenderProperties) + 91 - GetMainCharacterOffsetY();

            SetAbsoluteScreenPosition(screenX, screenY);
        }

        private int GetMainCharacterOffsetX()
        {
            return _characterRenderOffsetCalculator.CalculateOffsetX(_characterProvider.ActiveCharacter.RenderProperties);
        }

        private int GetMainCharacterOffsetY()
        {
            return _characterRenderOffsetCalculator.CalculateOffsetY(_characterProvider.ActiveCharacter.RenderProperties);
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sb.Dispose();
                _charRenderTarget.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
