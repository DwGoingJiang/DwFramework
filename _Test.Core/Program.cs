﻿using System;

using DwFramework.Core;
using DwFramework.Core.Plugins;
using Microsoft.Extensions.Logging;

namespace _Test.Core
{
    class Program
    {
        static void Main(string[] args)
        {
            Timer.SetStartTime(DateTime.Parse("1970-01-01"));
            Console.WriteLine(Timer.GetTotalSeconds());

            //ServiceHost host = new ServiceHost(configFilePath: $"{AppDomain.CurrentDomain.BaseDirectory}Config.json");
            //host.RegisterLog();
            //host.RegisterType<CTest>();
            //var provider = host.Build();
            //provider.GetService<CTest>().M();
            //Console.Read();
        }
    }

    public interface ITest
    {
        void M();
    }

    public class CTest : ITest
    {
        private readonly ILogger<CTest> _logger;

        public CTest(ILogger<CTest> logger, IEnvironment environment)
        {
            _logger = logger;
        }

        public void M()
        {
            _logger.LogInformation("Helo");
        }
    }
}
