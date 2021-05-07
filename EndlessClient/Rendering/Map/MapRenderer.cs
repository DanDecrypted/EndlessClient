﻿using System;
using System.Collections.Generic;
using System.Linq;
using EndlessClient.GameExecution;
using EndlessClient.Rendering.Character;
using EndlessClient.Rendering.Chat;
using EndlessClient.Rendering.Factories;
using EndlessClient.Rendering.MapEntityRenderers;
using EndlessClient.Rendering.NPC;
using EOLib;
using EOLib.Config;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using EOLib.Domain.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EndlessClient.Rendering.Map
{
    public class MapRenderer : DrawableGameComponent, IMapRenderer
    {
        private const double TRANSITION_TIME_MS = 125.0;

        private readonly object _rt_locker_ = new object();

        private readonly IRenderTargetFactory _renderTargetFactory;
        private readonly IMapEntityRendererProvider _mapEntityRendererProvider;
        private readonly ICharacterProvider _characterProvider;
        private readonly ICurrentMapProvider _currentMapProvider;
        private readonly IMapRenderDistanceCalculator _mapRenderDistanceCalculator;
        private readonly ICharacterRendererUpdater _characterRendererUpdater;
        private readonly INPCRendererUpdater _npcRendererUpdater;
        private readonly IDynamicMapObjectUpdater _dynamicMapObjectUpdater;
        private readonly IChatBubbleUpdater _chatBubbleUpdater;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IMouseCursorRenderer _mouseCursorRenderer;
        private readonly IRenderOffsetCalculator _renderOffsetCalculator;
        private readonly IClientWindowSizeRepository _clientWindowSizeRepository;

        private RenderTarget2D _mapBaseTarget, _mapObjectTarget;
        private SpriteBatch _sb;
        private MapTransitionState _mapTransitionState = MapTransitionState.Default;
        private int? _lastMapChecksum;

        private Optional<MapQuakeState> _quakeState;

        private bool MouseOver
        {
            get
            {
                var ms = Mouse.GetState();
                //todo: turn magic numbers into meaningful values
                return Game.IsActive && ms.X > 0 && ms.Y > 0 && ms.X < 640 && ms.Y < 320;
            }
        }

        public MapRenderer(IEndlessGame endlessGame,
                           IRenderTargetFactory renderTargetFactory,
                           IMapEntityRendererProvider mapEntityRendererProvider,
                           ICharacterProvider characterProvider,
                           ICurrentMapProvider currentMapProvider,
                           IMapRenderDistanceCalculator mapRenderDistanceCalculator,
                           ICharacterRendererUpdater characterRendererUpdater,
                           INPCRendererUpdater npcRendererUpdater,
                           IDynamicMapObjectUpdater dynamicMapObjectUpdater,
                           IChatBubbleUpdater chatBubbleUpdater,
                           IConfigurationProvider configurationProvider,
                           IMouseCursorRenderer mouseCursorRenderer,
                           IRenderOffsetCalculator renderOffsetCalculator,
                           IClientWindowSizeRepository clientWindowSizeRepository)
            : base((Game)endlessGame)
        {
            _renderTargetFactory = renderTargetFactory;
            _mapEntityRendererProvider = mapEntityRendererProvider;
            _characterProvider = characterProvider;
            _currentMapProvider = currentMapProvider;
            _mapRenderDistanceCalculator = mapRenderDistanceCalculator;
            _characterRendererUpdater = characterRendererUpdater;
            _npcRendererUpdater = npcRendererUpdater;
            _dynamicMapObjectUpdater = dynamicMapObjectUpdater;
            _chatBubbleUpdater = chatBubbleUpdater;
            _configurationProvider = configurationProvider;
            _mouseCursorRenderer = mouseCursorRenderer;
            _renderOffsetCalculator = renderOffsetCalculator;
            _clientWindowSizeRepository = clientWindowSizeRepository;
        }

        public override void Initialize()
        {
            _clientWindowSizeRepository.GameWindowSizeChanged += ResizeGameWindow;

            _mapBaseTarget = _renderTargetFactory.CreateRenderTarget();
            _mapObjectTarget = _renderTargetFactory.CreateRenderTarget();
            _sb = new SpriteBatch(Game.GraphicsDevice);

            _mouseCursorRenderer.Initialize();

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (!_lastMapChecksum.HasValue || _lastMapChecksum != _currentMapProvider.CurrentMap.Properties.ChecksumInt)
            {
                // The dimensions of the map are 0-based in the properties. Adjust to 1-based for RT creation
                var widthPlus1 = _currentMapProvider.CurrentMap.Properties.Width + 1;
                var heightPlus1 = _currentMapProvider.CurrentMap.Properties.Height + 1;

                lock (_rt_locker_)
                {
                    _mapBaseTarget.Dispose();
                    _mapBaseTarget = _renderTargetFactory.CreateRenderTarget(
                        (widthPlus1 + heightPlus1) * 32,
                        (widthPlus1 + heightPlus1) * 16);
                }
            }

            if (Visible)
            {
                _characterRendererUpdater.UpdateCharacters(gameTime);
                _npcRendererUpdater.UpdateNPCs(gameTime);
                _dynamicMapObjectUpdater.UpdateMapObjects(gameTime);
                _chatBubbleUpdater.UpdateChatBubbles(gameTime);

                if (MouseOver)
                    _mouseCursorRenderer.Update(gameTime);

                UpdateQuakeState();
            }

            _lastMapChecksum = _currentMapProvider.CurrentMap.Properties.ChecksumInt;

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible)
                return;

            DrawGroundLayerToRenderTarget();
            DrawMapToRenderTarget();
            DrawToSpriteBatch(_sb, gameTime);

            base.Draw(gameTime);
        }

        public void StartMapTransition()
        {
            _mapTransitionState = new MapTransitionState(DateTime.Now, 1);
        }

        public void StartEarthquake(byte strength)
        {
            _quakeState = new MapQuakeState(strength);
        }

        private void UpdateQuakeState()
        {
            // when quake:
            // 1. determine offset target
            // 2. incrementally make offset approach closer to the target offset
            // 3. when offset target reached, determine new target (random based on magnitude)
            // 4. flip direction
            // 5. keep going until specific number of "direction flips" has elapsed

            if (!_quakeState.HasValue)
                return;

            _quakeState = _quakeState.Value.NextOffset();

            var quakeState = _quakeState.Value;
            if (quakeState.OffsetReached)
            {
                _quakeState = quakeState.NextState();
            }

            if (quakeState.Done)
            {
                _quakeState = Optional<MapQuakeState>.Empty;
            }
        }

        private void DrawGroundLayerToRenderTarget()
        {
            if (!_mapTransitionState.StartTime.HasValue && _lastMapChecksum == _currentMapProvider.CurrentMap.Properties.ChecksumInt)
                return;

            lock (_rt_locker_)
            {
                GraphicsDevice.SetRenderTarget(_mapBaseTarget);
                _sb.Begin();

                var renderBounds = new MapRenderBounds(0, _currentMapProvider.CurrentMap.Properties.Height,
                                                       0, _currentMapProvider.CurrentMap.Properties.Width);

                var transitionComplete = true;
                for (var row = renderBounds.FirstRow; row <= renderBounds.LastRow; row++)
                {
                    for (var col = renderBounds.FirstCol; col <= renderBounds.LastCol; ++col)
                    {
                        var alpha = GetAlphaForCoordinates(col, row, _characterProvider.MainCharacter);
                        transitionComplete &= alpha == 255;

                        if (_mapEntityRendererProvider.GroundRenderer.CanRender(row, col))
                            _mapEntityRendererProvider.GroundRenderer.RenderElementAt(_sb, row, col, alpha);
                    }
                }

                if (transitionComplete)
                    _mapTransitionState = new MapTransitionState(Optional<DateTime>.Empty, 0);

                _sb.End();
                GraphicsDevice.SetRenderTarget(null);
            }
        }

        private void DrawMapToRenderTarget()
        {
            var immutableCharacter = _characterProvider.MainCharacter;

            lock (_rt_locker_)
            {
                GraphicsDevice.SetRenderTarget(_mapObjectTarget);
                GraphicsDevice.Clear(ClearOptions.Target, Color.Transparent, 0, 0);

                var gfxToRenderLast = new SortedList<Point, List<MapRenderLayer>>(new PointComparer());

                _sb.Begin();

                var renderBounds = _mapRenderDistanceCalculator.CalculateRenderBounds(immutableCharacter, _currentMapProvider.CurrentMap);
                for (var row = renderBounds.FirstRow; row <= renderBounds.LastRow; row++)
                {
                    for (var col = renderBounds.FirstCol; col <= renderBounds.LastCol; col++)
                    {
                        var alpha = GetAlphaForCoordinates(col, row, immutableCharacter);

                        foreach (var renderer in _mapEntityRendererProvider.MapEntityRenderers)
                        {
                            if (!renderer.CanRender(row, col))
                                continue;

                            if (renderer.ShouldRenderLast)
                            {
                                var renderLaterKey = new Point(col, row);
                                if (gfxToRenderLast.ContainsKey(renderLaterKey))
                                    gfxToRenderLast[renderLaterKey].Add(renderer.RenderLayer);
                                else
                                    gfxToRenderLast.Add(renderLaterKey, new List<MapRenderLayer> { renderer.RenderLayer });
                            }
                            else
                                renderer.RenderElementAt(_sb, row, col, alpha);
                        }
                    }
                }

                foreach (var kvp in gfxToRenderLast)
                {
                    var pointKey = kvp.Key;
                    var alpha = GetAlphaForCoordinates(pointKey.X, pointKey.Y, immutableCharacter);

                    foreach (var layer in kvp.Value)
                    {
                        _mapEntityRendererProvider.MapEntityRenderers
                                                  .Single(x => x.RenderLayer == layer)
                                                  .RenderElementAt(_sb, pointKey.Y, pointKey.X, alpha);
                    }
                }

                _sb.End();
                GraphicsDevice.SetRenderTarget(null);
            }
        }

        private void DrawToSpriteBatch(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Begin();

            var offset = _quakeState.HasValue ? _quakeState.Value.Offset : 0;

            lock (_rt_locker_)
            {
                spriteBatch.Draw(_mapBaseTarget, GetGroundLayerDrawPosition() + new Vector2(offset, 0), Color.White);
                DrawBaseLayers(spriteBatch);

                _mouseCursorRenderer.Draw(spriteBatch, new Vector2(offset, 0));

                spriteBatch.Draw(_mapObjectTarget, new Vector2(offset, 0), Color.White);

                spriteBatch.End();
            }
        }

        private void DrawBaseLayers(SpriteBatch spriteBatch)
        {
            var offset = _quakeState.HasValue ? _quakeState.Value.Offset : 0;
            var renderBounds = _mapRenderDistanceCalculator.CalculateRenderBounds(_characterProvider.MainCharacter, _currentMapProvider.CurrentMap);

            for (var row = renderBounds.FirstRow; row <= renderBounds.LastRow; row++)
            {
                for (var col = renderBounds.FirstCol; col <= renderBounds.LastCol; ++col)
                {
                    var alpha = GetAlphaForCoordinates(col, row, _characterProvider.MainCharacter);

                    foreach (var renderer in _mapEntityRendererProvider.BaseRenderers)
                    {
                        if (renderer.CanRender(row, col))
                            renderer.RenderElementAt(spriteBatch, row, col, alpha, new Vector2(offset, 0));
                    }
                }
            }
        }

        private Vector2 GetGroundLayerDrawPosition()
        {
            var ViewportWidthFactor = _clientWindowSizeRepository.Width / 2; // 640 * (1/2)
            var ViewportHeightFactor = _clientWindowSizeRepository.Height * 3 / 10; // 480 * (3/10)

            var props = _characterProvider.MainCharacter.RenderProperties;
            var charOffX = _renderOffsetCalculator.CalculateWalkAdjustX(props);
            var charOffY = _renderOffsetCalculator.CalculateWalkAdjustY(props);

            var mapHeightPlusOne = _currentMapProvider.CurrentMap.Properties.Height + 1;

            // X coordinate: +32 per Y, -32 per X
            // Y coordinate: -16 per Y, -16 per X
            // basically the opposite of the algorithm for rendering the ground tiles
            return new Vector2(ViewportWidthFactor - (mapHeightPlusOne * 32) + (props.MapY * 32) - (props.MapX * 32) - charOffX,
                               ViewportHeightFactor - (props.MapY * 16) - (props.MapX * 16) - charOffY);
        }

        private int GetAlphaForCoordinates(int objX, int objY, ICharacter character)
        {
            if (!_configurationProvider.ShowTransition)
            {
                _mapTransitionState = new MapTransitionState(Optional<DateTime>.Empty, 0);
                return 255;
            }

            //get the farther away of X or Y coordinate for the map object
            var metric = Math.Max(Math.Abs(objX - character.RenderProperties.MapX),
                                  Math.Abs(objY - character.RenderProperties.MapY));

            int alpha;
            if (!_mapTransitionState.StartTime.HasValue ||
                metric < _mapTransitionState.TransitionMetric ||
                _mapTransitionState.TransitionMetric == 0)
            {
                alpha = 255;
            }
            else if (metric == _mapTransitionState.TransitionMetric)
            {
                var ms = (DateTime.Now - _mapTransitionState.StartTime).TotalMilliseconds;
                alpha = (int)Math.Round(ms / TRANSITION_TIME_MS * 255);

                if (ms / TRANSITION_TIME_MS >= 1)
                    _mapTransitionState = new MapTransitionState(DateTime.Now, _mapTransitionState.TransitionMetric + 1);
            }
            else
                alpha = 0;

            return alpha;
        }

        private void ResizeGameWindow(object sender, EventArgs e)
        {
            lock (_rt_locker_)
            {
                _mapObjectTarget.Dispose();
                _mapObjectTarget = _renderTargetFactory.CreateRenderTarget(_clientWindowSizeRepository.Width, _clientWindowSizeRepository.Height);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_rt_locker_)
                {
                    _mapBaseTarget.Dispose();
                    _mapObjectTarget.Dispose();
                }
                _sb.Dispose();
                _mouseCursorRenderer.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    internal struct MapTransitionState
    {
        internal static MapTransitionState Default => new MapTransitionState(Optional<DateTime>.Empty, 0);

        internal Optional<DateTime> StartTime { get; }

        internal int TransitionMetric { get; }

        internal MapTransitionState(Optional<DateTime> startTime, int transitionMetric)
            : this()
        {
            StartTime = startTime;
            TransitionMetric = transitionMetric;
        }
    }

    internal struct MapQuakeState
    {
        private static readonly Random _random = new Random();

        internal static MapQuakeState Default => new MapQuakeState();

        internal int Magnitude { get; }

        internal float Offset { get; }

        internal float OffsetTarget { get; }

        internal bool OffsetReached => Math.Abs(Offset) >= Math.Abs(OffsetTarget);

        internal int Flips { get; }

        internal int FlipsMax => Magnitude == 0 ? 0 : 10 + Magnitude * 2;

        internal bool Done => Flips >= FlipsMax;

        internal MapQuakeState(int magnitude)
            : this(magnitude, 0, 0) { }

        private MapQuakeState(int magnitude, float offset, int flips)
            : this(magnitude, offset, NewOffsetTarget(magnitude), flips) { }

        private MapQuakeState(int magnitude, float offset, float offsetTarget, int flips)
        {
            Magnitude = magnitude;
            Offset = offset;
            OffsetTarget = offsetTarget;
            Flips = flips;
        }

        internal MapQuakeState NextOffset()
        {
            var nextOffset = Offset + OffsetTarget / 4f;
            return new MapQuakeState(Magnitude, nextOffset, OffsetTarget, Flips);
        }

        internal MapQuakeState NextState()
        {
            var flip = -OffsetTarget / Math.Abs(OffsetTarget);
            var offset = OffsetTarget + 1*flip;
            var nextOffsetTarget = NewOffsetTarget(Magnitude) * flip;

            return new MapQuakeState(Magnitude, offset, nextOffsetTarget, Flips + 1);
        }

        private static float NewOffsetTarget(int magnitude) => 16 + 3 * _random.Next(0, (int)(magnitude * 1.5));
    }
}