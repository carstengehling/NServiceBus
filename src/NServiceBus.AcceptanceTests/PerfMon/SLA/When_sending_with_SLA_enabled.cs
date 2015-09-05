﻿namespace NServiceBus.AcceptanceTests.PerfMon.SLA
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_sending_with_SLA_enabled : NServiceBusAcceptanceTest
    {
        float counterValue;

        [Test]
        [Explicit("Since perf counters need to be enabled with powershell")]
        public void Should_have_perf_counter_set()
        {
            using (var counter = new PerformanceCounter("NServiceBus", "SLA violation countdown", "PerformanceMonitoring.Endpoint.WhenSendingWithSLAEnabled." + Transports.Default.Key, true))
            using (new Timer(state => CheckPerfCounter(counter), null, 0, 100))
            {
                var context = new Context();
                Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) =>
                    {
                        bus.SendLocal(new MyMessage());
                        return Task.FromResult(0);
                    }))
                    .Done(c => c.WasCalled)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.WasCalled, "The message handler should be called"))
                    .Run();
            }
            Assert.Greater(counterValue, 0);
        }

        void CheckPerfCounter(PerformanceCounter counter)
        {
            float rawValue = counter.RawValue;
            if (rawValue > 0)
            {
                counterValue = rawValue;
            }
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(builder => builder.EnableSLAPerformanceCounter(TimeSpan.FromMinutes(10)));
            }
        }


        public class MyMessage : IMessage
        {
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public void Handle(MyMessage message)
            {
                Context.WasCalled = true;
            }
        }
    }
}