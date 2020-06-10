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
using DwFramework.Core.Extensions;

namespace DwFramework.Web
{
    public class WebService : BaseService
    {
        public class Config
        {
            public string ContentRoot { get; set; }
            public Dictionary<string, string> HttpListen { get; set; }
            public Dictionary<string, string> WebSocketListen { get; set; }
            public int BufferSize { get; set; } = 1024 * 4;
        }

        private readonly Config _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="environment"></param>
        public WebService(IServiceProvider provider, IEnvironment environment) : base(provider, environment)
        {
            _config = _environment.GetConfiguration().GetConfig<Config>("Web");
        }

        #region Socket
        private Dictionary<string, SocketConnection> _socketConnections;

        /// <summary>
        /// 开启Socket服务
        /// </summary>
        /// <returns></returns>
        public Task OpenSocketServiceAsync()
        {
            return Task.Run(() => { });
        }

        /// <summary>
        /// 检查客户端
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private void SocketRequireClient(string id)
        {
            if (!_socketConnections.ContainsKey(id))
                throw new Exception("该客户端不存在");
            var client = _socketConnections[id];
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
            var connection = _webSocketConnections[id];
            return connection.SendAsync(buffer)
                .ContinueWith(a => OnWebSocketSend?.Invoke(connection, new OnWebSocketSendEventargs(Encoding.UTF8.GetString(buffer))));
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
            return Task.Run(() =>
            {
                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                foreach (var item in _socketConnections.Values)
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
            var connection = _socketConnections[id];
            return connection.CloseAsync();
        }

        /// <summary>
        /// 断开所有连接
        /// </summary>
        /// <returns></returns>
        public Task SocketCloseAllAsync()
        {
            return Task.Run(() =>
            {
                foreach (var item in _socketConnections.Values)
                {
                    item.CloseAsync();
                }
            });
        }
        #endregion

        #region Http
        /// <summary>
        /// 开启Http服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public Task OpenHttpServiceAsync<T>() where T : class
        {
            var builder = new WebHostBuilder()
                .UseDwServiceProvider(_provider)
                // https证书路径
                .UseContentRoot($"{AppDomain.CurrentDomain.BaseDirectory}{_config.ContentRoot}")
                .UseKestrel(options =>
                {
                    // 监听地址及端口
                    if (_config.HttpListen == null || _config.HttpListen.Count <= 0)
                        options.Listen(IPAddress.Parse("0.0.0.0"), 5080);
                    else
                    {
                        if (_config.HttpListen.ContainsKey("http"))
                        {
                            string[] ipAndPort = _config.HttpListen["http"].Split(":");
                            options.Listen(IPAddress.Parse(ipAndPort[0]), int.Parse(ipAndPort[1]));
                        }
                        if (_config.HttpListen.ContainsKey("https"))
                        {
                            string[] addrAndCert = _config.HttpListen["https"].Split(";");
                            string[] ipAndPort = addrAndCert[0].Split(":");
                            options.Listen(IPAddress.Parse(ipAndPort[0]), int.Parse(ipAndPort[1]), listenOptions =>
                            {
                                string[] certAndPassword = addrAndCert[1].Split(",");
                                listenOptions.UseHttps(certAndPassword[0], certAndPassword[1]);
                            });
                        }
                    }
                })
                .UseStartup<T>();
            return Task.Run(() => builder.Build().Run());
        }
        #endregion

        #region WebSocket
        private Dictionary<string, WebSocketConnection> _webSocketConnections;

        public event OnWebSocketConnectHandler OnWebSocketConnect;
        public event OnWebSocketSendHandler OnWebSocketSend;
        public event OnWebSocketReceiveHandler OnWebSocketReceive;
        public event OnWebSocketCloseHandler OnWebSocketClose;
        public event OnWebSocketErrorHandler OnWebSocketError;

        /// <summary>
        /// 开启WebSocket服务
        /// </summary>
        /// <returns></returns>
        public Task OpenWebSocketServiceAsync()
        {
            _webSocketConnections = new Dictionary<string, WebSocketConnection>();
            var builder = new WebHostBuilder()
                .UseDwServiceProvider(_provider)
                // wss证书路径
                .UseContentRoot($"{AppDomain.CurrentDomain.BaseDirectory}{_config.ContentRoot}")
                .UseKestrel(options =>
                {
                    // 监听地址及端口
                    if (_config.WebSocketListen == null || _config.WebSocketListen.Count <= 0)
                        options.Listen(IPAddress.Parse("0.0.0.0"), 5088);
                    else
                    {
                        if (_config.WebSocketListen.ContainsKey("ws"))
                        {
                            string[] ipAndPort = _config.WebSocketListen["ws"].Split(":");
                            options.Listen(IPAddress.Parse(ipAndPort[0]), int.Parse(ipAndPort[1]));
                        }
                        if (_config.WebSocketListen.ContainsKey("wss"))
                        {
                            string[] addrAndCert = _config.WebSocketListen["wss"].Split(";");
                            string[] ipAndPort = addrAndCert[0].Split(":");
                            options.Listen(IPAddress.Parse(ipAndPort[0]), int.Parse(ipAndPort[1]), listenOptions =>
                            {
                                string[] certAndPassword = addrAndCert[1].Split(",");
                                listenOptions.UseHttps(certAndPassword[0], certAndPassword[1]);
                            });
                        }
                    }
                })
                .Configure(app =>
                {
                    app.UseWebSockets();
                    // 请求预处理
                    app.Use(async (context, next) =>
                    {
                        if (!context.WebSockets.IsWebSocketRequest)
                        {
                            await context.Response.WriteAsync(ResultInfo.Fail("非WebSocket请求").ToJson());
                            return;
                        }
                        await next();
                    });
                    // 自定义处理
                    app.Run(async context =>
                    {
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        var connection = new WebSocketConnection(webSocket);
                        _webSocketConnections[connection.ID] = connection;
                        OnWebSocketConnect?.Invoke(connection, new OnWebSocketConnectEventargs() { });
                        WebSocketReceiveResult result = null;
                        while (true)
                        {
                            try
                            {
                                var buffer = new byte[_config.BufferSize];
                                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                                if (result.CloseStatus.HasValue)
                                    break;
                                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                                OnWebSocketReceive?.Invoke(connection, new OnWebSocketReceiveEventargs(msg));
                            }
                            catch (Exception ex)
                            {
                                OnWebSocketError?.Invoke(connection, new OnWebSocketErrorEventargs(ex));
                                break;
                            }
                        }
                        OnWebSocketClose?.Invoke(connection, new OnWebSocketCloceEventargs() { });
                        connection.Dispose();
                        _webSocketConnections.Remove(connection.ID);
                    });
                });
            return Task.Run(() => builder.Build().Run());
        }

        /// <summary>
        /// 检查客户端
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private void WebSocketRequireClient(string id)
        {
            if (!_webSocketConnections.ContainsKey(id))
                throw new Exception("该客户端不存在");
            var client = _webSocketConnections[id];
            if (client.WebSocket.State != WebSocketState.Open)
                throw new Exception("该客户端状态错误");
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Task WebSocketSendAsync(string id, byte[] buffer)
        {
            WebSocketRequireClient(id);
            var connection = _webSocketConnections[id];
            return connection.SendAsync(buffer)
                .ContinueWith(a => OnWebSocketSend?.Invoke(connection, new OnWebSocketSendEventargs(Encoding.UTF8.GetString(buffer))));
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task WebSocketSendAsync(string id, string msg)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            return WebSocketSendAsync(id, buffer);
        }

        /// <summary>
        /// 广播消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task WebSocketBroadCastAsync(string msg)
        {
            return Task.Run(() =>
            {
                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                foreach (var item in _webSocketConnections.Values)
                {
                    WebSocketSendAsync(item.ID, buffer);
                }
            });
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task WebSocketCloseAsync(string id)
        {
            WebSocketRequireClient(id);
            var connection = _webSocketConnections[id];
            return connection.CloseAsync();
        }

        /// <summary>
        /// 断开所有连接
        /// </summary>
        /// <returns></returns>
        public Task WebSocketCloseAllAsync()
        {
            return Task.Run(() =>
            {
                foreach (var item in _webSocketConnections.Values)
                {
                    item.CloseAsync();
                }
            });
        }
        #endregion
    }
}
