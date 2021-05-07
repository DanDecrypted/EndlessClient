using AutomaticTypeMapper;
using EndlessClient.GameExecution;
using EndlessClient.Rendering.Character;
using EndlessClient.Rendering.Chat;
using EndlessClient.Rendering.Map;
using EndlessClient.Rendering.MapEntityRenderers;
using EndlessClient.Rendering.NPC;
using EOLib.Config;
using EOLib.Domain.Character;
using EOLib.Domain.Map;

namespace EndlessClient.Rendering.Factories
{
    [MappedType(BaseType = typeof(IMapRendererFactory))]
    public class MapRendererFactory : IMapRendererFactory
    {
        private readonly IEndlessGameProvider _endlessGameProvider;
        private readonly IRenderTargetFactory _renderTargetFactory;
        private readonly IMapEntityRendererProvider _mapEntityRendererProvider;
        private readonly ICharacterProvider _characterProvider;
        private readonly ICurrentMapProvider _currentMapProvider;
        private readonly IMapRenderDistanceCalculator _mapRenderDistanceCalculator;
        private readonly ICharacterRendererUpdater _characterRendererUpdater;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IMouseCursorRendererFactory _mouseCursorRendererFactory;
        private readonly IRenderOffsetCalculator _renderOffsetCalculator;
        private readonly IClientWindowSizeRepository _clientWindowSizeRepository;
        private readonly INPCRendererUpdater _npcRendererUpdater;
        private readonly IDynamicMapObjectUpdater _dynamicMapObjectUpdater;
        private readonly IChatBubbleUpdater _chatBubbleUpdater;

        public MapRendererFactory(IEndlessGameProvider endlessGameProvider,
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
            IMouseCursorRendererFactory mouseCursorRendererFactory,
            IRenderOffsetCalculator renderOffsetCalculator,
            IClientWindowSizeRepository clientWindowSizeRepository)
        {
            _endlessGameProvider = endlessGameProvider;
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
            _mouseCursorRendererFactory = mouseCursorRendererFactory;
            _renderOffsetCalculator = renderOffsetCalculator;
            _clientWindowSizeRepository = clientWindowSizeRepository;
        }

        public IMapRenderer CreateMapRenderer()
        {
            return new MapRenderer(_endlessGameProvider.Game,
                                   _renderTargetFactory,
                                   _mapEntityRendererProvider,
                                   _characterProvider,
                                   _currentMapProvider,
                                   _mapRenderDistanceCalculator,
                                   _characterRendererUpdater,
                                   _npcRendererUpdater,
                                   _dynamicMapObjectUpdater,
                                   _chatBubbleUpdater,
                                   _configurationProvider,
                                   _mouseCursorRendererFactory.Create(),
                                   _renderOffsetCalculator,
                                   _clientWindowSizeRepository);
        }
    }
}