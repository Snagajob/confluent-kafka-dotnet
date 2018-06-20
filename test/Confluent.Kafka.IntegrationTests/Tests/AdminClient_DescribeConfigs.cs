// Copyright 2016-2017 Confluent Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Refer to LICENSE for more information.

#pragma warning disable xUnit1026

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Confluent.Kafka.Admin;
using Confluent.Kafka.Serialization;
using Xunit;


namespace Confluent.Kafka.IntegrationTests
{
    public static partial class Tests
    {
        /// <summary>
        ///     Test functionality of AdminClient.DescribeConfigs.
        /// </summary>
        [Theory, MemberData(nameof(KafkaParameters))]
        public static void AdminClient_DescribeConfigs(string bootstrapServers, string singlePartitionTopic, string partitionedTopic)
        {
            using (var adminClient = new AdminClient(new Dictionary<string, object> { { "bootstrap.servers", bootstrapServers } }))
            {
                // broker configs
                // ---
                var configResource = new ConfigResource { Name = "0", ResourceType = ConfigType.Broker };
                var results = adminClient.DescribeConfigsAsync(new List<ConfigResource> { configResource }).Result;

                Assert.Single(results);
                Assert.False(results[0].Error.IsError);
                Assert.True(results[0].Entries.Count > 50);
                // note: unlike other parts of the api, Entries is kept as a dictionary since it's convenient for
                // the most typical use case.
                Assert.Single(results[0].Entries.Where(e => e.Key == "advertised.listeners"));
                Assert.Single(results[0].Entries.Where(e => e.Key == "num.network.threads"));

                var a = results.Select(aa => aa.Entries.Where(b => b.Value.Synonyms.Count > 0).ToList()).ToList();

                // topic configs, more than one.
                // ---
                results = adminClient.DescribeConfigsAsync(new List<ConfigResource> { 
                    new ConfigResource { Name = singlePartitionTopic, ResourceType = ConfigType.Topic },
                    new ConfigResource { Name = partitionedTopic, ResourceType = ConfigType.Topic }
                }).Result;

                Assert.Equal(2, results.Count);
                Assert.False(results[0].Error.IsError);
                Assert.False(results[0].Error.IsError);
                Assert.True(results[0].Entries.Count > 20);
                Assert.True(results[1].Entries.Count > 20);
                Assert.Single(results[0].Entries.Where(e => e.Key == "compression.type"));
                Assert.Single(results[0].Entries.Where(e => e.Key == "flush.ms"));

                // options are specified.
                // ---
                results = adminClient.DescribeConfigsAsync(new List<ConfigResource> { configResource }, new DescribeConfigsOptions { Timeout = TimeSpan.FromSeconds(10) }).Result;
                Assert.Single(results);
                Assert.True(results[0].Entries.Count > 20);

                // invalid config resource
                // --- 
                try
                {
                    // TODO: this actually segfaults.
                    results = adminClient.DescribeConfigsAsync(new List<ConfigResource> { new ConfigResource() }).Result;
                    Assert.True(false);
                }
                catch (KafkaException)
                {
                    // expected.
                }

                // invalid topic.
                // ---
                //
                // TODO: this creates the topic, then describes what it just created. what we want? does java explicitly not do this?
                // 
                // results = adminClient.DescribeConfigsAsync(new List<ConfigResource> {
                //     new ConfigResource { Name = "my-nonsense-topic", ResourceType = ConfigType.Topic }
                // }).Result;
            }
        }

    }
}