﻿using System.Threading;
using System.Threading.Tasks;
using AutomaticTypeMapper;

namespace EOLib.Net.Communication
{
    [AutoMappedType]
    public class PacketSendService : IPacketSendService
    {
        private readonly INetworkClientProvider _networkClientProvider;
        private readonly IPacketQueueProvider _packetQueueProvider;

        public PacketSendService(INetworkClientProvider networkClientProvider,
            IPacketQueueProvider packetQueueProvider)
        {
            _networkClientProvider = networkClientProvider;
            _packetQueueProvider = packetQueueProvider;
        }

        public void SendPacket(IPacket packet)
        {
            var bytes = Client.Send(packet);
            if (bytes == 0)
                throw new NoDataSentException();
        }

        public async Task SendPacketAsync(IPacket packet)
        {
            var bytes = await Client.SendAsync(packet);
            if (bytes == 0)
                throw new NoDataSentException();
        }

        public async Task<IPacket> SendRawPacketAndWaitAsync(IPacket packet)
        {
            var bytes = await Client.SendRawPacketAsync(packet);
            if (bytes == 0)
                throw new NoDataSentException();

            var responsePacket = await InBandQueue.WaitForPacketAndDequeue(Timeout.InfiniteTimeSpan);
            if (responsePacket is EmptyPacket)
                throw new EmptyPacketReceivedException();

            return responsePacket;
        }

        public async Task<IPacket> SendEncodedPacketAndWaitAsync(IPacket packet)
        {
            var bytes = await Client.SendAsync(packet);
            if (bytes == 0)
                throw new NoDataSentException();

            var responsePacket = await InBandQueue.WaitForPacketAndDequeue(Timeout.InfiniteTimeSpan);
            if (responsePacket is EmptyPacket)
                throw new EmptyPacketReceivedException();

            return responsePacket;
        }

        private INetworkClient Client => _networkClientProvider.NetworkClient;

        private IWaitablePacketQueue InBandQueue => _packetQueueProvider.HandleInBandPacketQueue;
    }
}