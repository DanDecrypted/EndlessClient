﻿using System;
using System.Threading.Tasks;

namespace EOLib.Net.Communication
{
    public interface INetworkClient : IDisposable
    {
        bool Connected { get; }

        Task<ConnectResult> ConnectToServer(string host, int port);

        void Disconnect();

        Task RunReceiveLoopAsync();

        void CancelBackgroundReceiveLoop();

        int Send(IPacket packet);

        Task<int> SendAsync(IPacket packet, int timeout = Constants.SendTimeout);

        Task<int> SendRawPacketAsync(IPacket packet, int timeout = Constants.SendTimeout);
    }
}
