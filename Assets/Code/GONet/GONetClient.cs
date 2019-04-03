﻿using NetcodeIO.NET;
using ReliableNetcode;
using System;

namespace GONet
{
    public class GONetClient
    {
        GONetConnection_ClientToServer connectionToServer;

        private Client client;

        public GONetClient(Client client)
        {
            this.client = client;

            connectionToServer = new GONetConnection_ClientToServer(client);

            client.OnStateChanged += OnStateChanged;
        }

        public void ConnectToServer(string serverIP, int serverPort)
        {
            connectionToServer.Connect(serverIP, serverPort);
        }

        public void SendBytesToServer(byte[] bytes, int bytesUsedCount, QosType qualityOfService = QosType.Reliable)
        {
            connectionToServer.SendMessage(bytes, bytesUsedCount, qualityOfService);
        }

        /// <summary>
        /// Call this every frame (from the main Unity thread!) in order to process all network traffic in a timely manner.
        /// </summary>
        public void Update()
        {
            connectionToServer.Update();
        }

        public void Disconnect()
        {
            connectionToServer.Disconnect();
        }

        private void OnStateChanged(ClientState state)
        {
            UnityEngine.Debug.Log("state changed to: " + Enum.GetName(typeof(ClientState), state)); // TODO remove unity references from this code base!
        }

    }
}
