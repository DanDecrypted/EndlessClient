﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using System.Threading.Tasks;
using EOLib.Domain.Chat;
using EOLib.Localization;
using EOLib.Net;
using EOLib.Net.Handlers;

namespace EOLib.PacketHandlers
{
    public abstract class FindCommandHandlerBase : IPacketHandler
    {
        private readonly IChatRepository _chatRespository;
        private readonly ILocalizedStringService _localizedStringService;

        public PacketFamily Family { get { return PacketFamily.Players; } }

        public abstract PacketAction Action { get; }

        //todo: handle in-game only
        public bool CanHandle { get { return true; } }

        protected abstract EOResourceID ResourceIDForResponse { get; }

        protected FindCommandHandlerBase(IChatRepository chatRespository,
                                         ILocalizedStringService localizedStringService)
        {
            _chatRespository = chatRespository;
            _localizedStringService = localizedStringService;
        }

        public bool HandlePacket(IPacket packet)
        {
            var playerName = packet.ReadEndString();
            var message = string.Format("{0} {1}",
                char.ToUpper(playerName[0]) + playerName.Substring(1),
                _localizedStringService.GetString(ResourceIDForResponse));

            var chatData = new ChatData("System", message, ChatIcon.LookingDude);
            _chatRespository.AllChat[ChatTab.Local].Add(chatData);

            return true;
        }

        public async Task<bool> HandlePacketAsync(IPacket packet)
        {
            return await Task.Run(() => HandlePacket(packet));
        }
    }
}