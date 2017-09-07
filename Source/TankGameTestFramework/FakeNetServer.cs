﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Lidgren.Network;
using System.Linq;

namespace TankGameTestFramework
{
    public class FakeNetServer : FakeNetPeer, INetServer
    {
        public void SendToAll(INetOutgoingMessage msg, NetDeliveryMethod method)
        {
            SendMessage(msg, ((INetServer)this).Connections, method, 0);
        }

        public void SendToAll(INetOutgoingMessage msg, INetConnection except, NetDeliveryMethod method, int sequenceChannel)
        {
            SendMessage(msg, ((INetServer)this).Connections.Where(item => item != except).ToList(), method, 0);
        }

        public FakeNetServer(long uniqueIdentifier) : base(uniqueIdentifier)
        {
        }
    }
}