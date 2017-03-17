﻿using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamCoding.Extensions;

namespace TeamCoding.VisualStudio.Models.ChangePersisters.RedisPersister
{
    public class RedisWrapper : IDisposable
    {
        private Task ConnectTask;
        private ConnectionMultiplexer RedisClient;
        private Dictionary<string, List<Action<RedisChannel, RedisValue>>> SubscribedActions = new Dictionary<string, List<Action<RedisChannel, RedisValue>>>();
        public RedisWrapper()
        {
            ConnectTask = ConnectRedisAsync();
            TeamCodingPackage.Current.Settings.SharedSettings.RedisServerChanged += SharedSettings_RedisServerChanged;
        }
        private async Task ChangeRedisServerAsync()
        {
            // We don't worry about the result of the task as any exceptions are already handled
            await ConnectTask;
            ResetRedis();
            await ConnectRedisAsync();
        }
        private void SharedSettings_RedisServerChanged(object sender, EventArgs e)
        {
            ConnectTask = ChangeRedisServerAsync();
        }
        private readonly static SemaphoreSlim GetServerStringErrorTextSemaphore = new SemaphoreSlim(1, 1);
        public static async Task<string> GetServerStringErrorTextAsync(string serverString)
        {            
            if (string.IsNullOrWhiteSpace(serverString))
            {
                return "Server cannot be purely whitespace";
            }
            await GetServerStringErrorTextSemaphore.WaitAsync();
            try
            {
                using (var redisClient = await ConnectionMultiplexer.ConnectAsync(serverString))
                {
                    if (!redisClient.IsConnected)
                    {
                        return "Could not connect to redis server";
                    }

                    var subscribeTriggerEvent = new ManualResetEventSlim();
                    const string testChannel = "TeamCoding.RedisWrapper.Test";
                    var testValue = Guid.NewGuid().ToString();
                    string receivedValue = null;
                    Action<RedisChannel, RedisValue> testHandler = (c, v) =>
                        {
                            if (v.ToString() != testValue)
                            {
                                receivedValue = v.ToString();
                            }
                            subscribeTriggerEvent.Set();
                        };
                    var subscriber = redisClient.GetSubscriber();
                    await subscriber.SubscribeAsync(testChannel, testHandler);
                    await subscriber.PublishAsync(testChannel, testValue);

                    if (await subscribeTriggerEvent.WaitHandle.AsTask(TimeSpan.FromSeconds(10)))
                    {
                        await subscriber.UnsubscribeAsync(testChannel, testHandler);
                        if (receivedValue != null)
                        {
                            return $"Value recieved did not match value sent.{Environment.NewLine}Sent: {testValue}{Environment.NewLine}Received {receivedValue}";
                        }
                    }
                    else
                    {
                        // TODO: Why does the redis property checker intermittently fail (locks?); does it still fail now we're using a task?
                        await subscriber.UnsubscribeAsync(testChannel, testHandler);
                        return "Could not send and receive test message after 10 seconds";
                    }
                }
            }
            finally
            {
                GetServerStringErrorTextSemaphore.Release();
            }

            return null;
        }
        private async Task ConnectRedisAsync()
        {
            var redisServer = TeamCodingPackage.Current.Settings.SharedSettings.RedisServer;
            if (!string.IsNullOrWhiteSpace(redisServer))
            {
                TeamCodingPackage.Current.Logger.WriteInformation($"Connecting to Redis using config string: \"{redisServer}\"");
                RedisClient = await ConnectionMultiplexer.ConnectAsync(redisServer)
                    .HandleExceptionAsync((ex) => TeamCodingPackage.Current.Logger.WriteError($"Failed to connect to redis server using config string: {redisServer}"));

                if (RedisClient != null)
                {
                    TeamCodingPackage.Current.Logger.WriteInformation($"Connected to Redis using config string: \"{redisServer}\"");
                    IEnumerable<Task> tasks;
                    lock (SubscribedActions)
                    {
                        var subscriber = RedisClient.GetSubscriber();
                        tasks = SubscribedActions.Keys.SelectMany(key => SubscribedActions[key].Select((a) => subscriber.SubscribeAsync(key, a)?.HandleException()));
                    }

                    await Task.WhenAll(tasks);
                }
            }
        }

        internal async Task PublishAsync(string channel, byte[] data)
        {
            await ConnectTask; // Wait to be connected first

            if (RedisClient != null)
            {
                await RedisClient?.GetSubscriber()?.PublishAsync(channel, data)?.HandleException();
                TeamCodingPackage.Current.Logger.WriteInformation("Sent data");
            }
            else if(!string.IsNullOrEmpty(TeamCodingPackage.Current.Settings.SharedSettings.RedisServer))
            {
                TeamCodingPackage.Current.Logger.WriteInformation("Redisclient == null, didn't send data");
            }
        }
        internal async Task SubscribeAsync(string channel, Action<RedisChannel, RedisValue> action)
        {
            lock (SubscribedActions)
            {
                if (!SubscribedActions.ContainsKey(channel))
                {
                    SubscribedActions.Add(channel, new List<Action<RedisChannel, RedisValue>>());
                }
                SubscribedActions[channel].Add(action);
            }

            await ConnectTask;
            if (RedisClient != null)
            {
                await RedisClient?.GetSubscriber()?.SubscribeAsync(channel, action)?.HandleException();
                TeamCodingPackage.Current.Logger.WriteInformation("Subscribed");
            }
            else
            {
                TeamCodingPackage.Current.Logger.WriteInformation("Redisclient == null, didn't subscribe");
            }
        }
        private void ResetRedis()
        {
            RedisClient?.Dispose();
            RedisClient = null;
        }
        public void Dispose()
        {
            ResetRedis();
        }
    }
}
