﻿using Mubox.Model.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Mubox.Control.Network
{
    public static class Server
    {
        private static TcpListener Listener { get; set; }

        private static bool IsListening { get; set; }

        private static List<ClientBase> Clients { get; set; }

        public static void Start(int portNumber)
        {
            if (Clients == null)
            {
                Clients = new List<ClientBase>();
            }
            if ((Listener == null) || ((Listener.LocalEndpoint as IPEndPoint).Port != portNumber))
            {
                if (Listener != null)
                {
                    try
                    {
                        Listener.Stop();
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                }
                Listener = new TcpListener(IPAddress.Any, portNumber);
            }
            if (!IsListening)
            {
                Listener.Start();
                Listener.BeginAcceptSocket(AcceptSocketCallback, null);
                IsListening = true;
            }
        }

        public static void Stop()
        {
            if (Listener != null)
            {
                if (IsListening)
                {
                    try
                    {
                        IsListening = false;
                        Listener.Stop();
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                }
            }
        }

        public static void AcceptSocketCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = Listener.EndAcceptSocket(ar);
                try
                {
                    if (socket != null)
                    {
                        socket.NoDelay = true;
                        socket.LingerState.Enabled = false;
                        NetworkClient client = null;

                        (System.Windows.Application.Current).Dispatcher.Invoke((Action)delegate()
                        {
                            try
                            {
                                client = new NetworkClient(socket, Guid.NewGuid().ToString());
                            }
                            catch (Exception ex)
                            {
                                client = null;
                                ex.Log();
                            }
                        });

                        if (client != null)
                        {
                            Clients.Add(client);
                            OnClientAccepted(client);
                            client.Attach();
                        }
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    Listener.BeginAcceptSocket(AcceptSocketCallback, null);
                }
            }
            catch (Exception)
            {
                IsListening = false;
            }
        }

        public sealed class ServerEventArgs : EventArgs
        {
            public ClientBase Client { get; set; }
        }

        public static event EventHandler<ServerEventArgs> ClientAccepted;

        private static void OnClientAccepted(ClientBase client)
        {
            if (ClientAccepted != null)
            {
                ClientAccepted(Listener, new ServerEventArgs
                {
                    Client = client
                });
            }
        }

        public static event EventHandler<ServerEventArgs> ClientRemoved;

        public static void RemoveClient(ClientBase client)
        {
            Clients.Remove(client);
            if (ClientRemoved != null)
            {
                ClientRemoved(Listener, new ServerEventArgs
                {
                    Client = client
                });
            }
        }
    }
}