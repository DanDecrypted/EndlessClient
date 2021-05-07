﻿using System;
using System.Linq;
using EndlessClient.Rendering.Chat;
using EndlessClient.Rendering.Map;
using EndlessClient.Rendering.NPC;
using EOLib.Domain.Character;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.Rendering.MapEntityRenderers
{
    public class NPCEntityRenderer : BaseMapEntityRenderer
    {
        private readonly INPCRendererProvider _npcRendererProvider;
        private readonly IChatBubbleProvider _chatBubbleProvider;

        public NPCEntityRenderer(ICharacterProvider characterProvider,
                                 IRenderOffsetCalculator renderOffsetCalculator,
                                 IClientWindowSizeProvider clientWindowSizeProvider,
                                 INPCRendererProvider npcRendererProvider,
                                 IChatBubbleProvider chatBubbleProvider)
            : base(characterProvider, renderOffsetCalculator, clientWindowSizeProvider)
        {
            _npcRendererProvider = npcRendererProvider;
            _chatBubbleProvider = chatBubbleProvider;
        }

        public override MapRenderLayer RenderLayer => MapRenderLayer.Npc;

        protected override int RenderDistance => 16;

        protected override bool ElementExistsAt(int row, int col)
        {
            return _npcRendererProvider.NPCRenderers.Values
                .Count(n => n.NPC.X == col && n.NPC.Y == row) > 0;
        }

        public override void RenderElementAt(SpriteBatch spriteBatch, int row, int col, int alpha, Vector2 additionalOffset = default)
        {
            var indicesToRender = _npcRendererProvider.NPCRenderers.Values
                .Where(n => n.NPC.X == col && n.NPC.Y == row)
                .Select(n => n.NPC.Index);

            foreach (var index in indicesToRender)
            {
                if (!_npcRendererProvider.NPCRenderers.ContainsKey(index) ||
                    _npcRendererProvider.NPCRenderers[index] == null)
                    throw new InvalidOperationException(
                        $"NPC renderer for ID {index} is null or missing! Did you call MapRenderer.Update() before calling MapRenderer.Draw()?");

                var renderer = _npcRendererProvider.NPCRenderers[index];
                renderer.DrawToSpriteBatch(spriteBatch);

                if (_chatBubbleProvider.NPCChatBubbles.TryGetValue(index, out var chatBubble))
                    chatBubble.DrawToSpriteBatch(spriteBatch);
            }
        }
    }
}
