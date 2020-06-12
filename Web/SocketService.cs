﻿using System;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;

using DwFramework.Core;
using DwFramework.Core.Helper;
using DwFramework.Core.Extensions;

namespace DwFramework.Web
{
    public class SocketService : BaseService
    {
        public class Config
        {
            public string Listen { get; set; }
            public int BackLog { get; set; } = 100;
            public int BufferSize { get; set; } = 1024 * 4;
        }

        public class OnConnectEventargs : EventArgs
        {

        }

        public class OnSendEventargs : EventArgs
        {
            public string Message { get; private set; }

            public OnSendEventargs(string msg)
            {
                Message = msg;
            }
        }

        public class OnReceiveEventargs : EventArgs
        {
            public string Message { get; private set; }

            public OnReceiveEventargs(string msg)
            {
                Message = msg;
            }
        }

        public class OnCloceEventargs : EventArgs
        {

        }

        public class OnErrorEventargs : EventArgs
        {
            public Exception Exception { get; private set; }

            public OnErrorEventargs(Exception exception)
            {
                Exception = exception;
            }
        }

        private readonly Config _config;
        private readonly Dictionary<string, SocketConnection> _connections;
        private byte[] _buffer;
        private Socket _server;

        public event Action<SocketConnection, OnConnectEventargs> OnConnect;
        public event Action<SocketConnection, OnSendEventargs> OnSend;
        public event Action<SocketConnection, OnReceiveEventargs> OnReceive;
        public event Action<SocketConnection, OnCloceEventargs> OnClose;
        public event Action<SocketConnection, OnErrorEventargs> OnError;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="environment"></param>
        public SocketService(IServiceProvider provider, IEnvironment environment) : base(provider, environment)
        {
            _config = _environment.GetConfiguration().GetConfig<Config>("Web:Socket");
            _connections = new Dictionary<string, SocketConnection>();
        }

        /// <summary>
        /// 开启服务
        /// </summary>
        /// <returns></returns>
        public Task OpenServiceAsync()
        {
            return ThreadHelper.CreateTask(() =>
            {
                _buffer = new byte[_config.BufferSize];
                _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (string.IsNullOrEmpty(_config.Listen))
                    _server.Bind(new IPEndPoint(IPAddress.Any, 10085));
                else
                {
                    string[] ipAndPort = _config.Listen.Split(":");
                    _server.Bind(new IPEndPoint(string.IsNullOrEmpty(ipAndPort[0]) ? IPAddress.Any : IPAddress.Parse(ipAndPort[0]), int.Parse(ipAndPort[1])));
                }
                _server.Listen(_config.BackLog);
            });
        }

        /// <summary>
        /// 检查客户端
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private void SocketRequireClient(string id)
        {
            if (!_connections.ContainsKey(id))
                throw new Exception("该客户端不存在");
            var client = _connections[id];
            if (client.Socket.Poll(1000, SelectMode.SelectRead))
                throw new Exception("该客户端状态错误");
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Task SocketSendAsync(string id, byte[] buffer)
        {
            SocketRequireClient(id);
            var connection = _connections[id];
            return connection.SendAsync(buffer)
                .ContinueWith(a => OnSend?.Invoke(connection, new OnSendEventargs(Encoding.UTF8.GetString(buffer))));
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task SocketSendAsync(string id, string msg)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            return SocketSendAsync(id, buffer);
        }

        /// <summary>
        /// 广播消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task SocketBroadCastAsync(string msg)
        {
            return ThreadHelper.CreateTask(() =>
            {
                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                foreach (var item in _connections.Values)
                {
                    SocketSendAsync(item.ID, buffer);
                }
            });
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task SocketCloseAsync(string id)
        {
            SocketRequireClient(id);
            var connection = _connections[id];
            return connection.CloseAsync();
        }

        /// <summary>
        /// 断开所有连接
        /// </summary>
        /// <returns></returns>
        public Task SocketCloseAllAsync()
        {
            return ThreadHelper.CreateTask(() =>
            {
                foreach (var item in _connections.Values)
                {
                    item.CloseAsync();
                }
            });
        }
    }
}