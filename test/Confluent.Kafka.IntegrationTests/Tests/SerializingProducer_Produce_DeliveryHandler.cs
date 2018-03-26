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
using System.Text;
using System.Collections.Generic;
using Confluent.Kafka.Serialization;
using Xunit;


namespace Confluent.Kafka.IntegrationTests
{
    /// <summary>
    ///     Test every Producer&lt;TKey,TValue&gt;.ProduceAsync method overload
    ///     that provides delivery reports via an Action callback.
    /// </summary>
    public static partial class Tests
    {
        [Theory, MemberData(nameof(KafkaParameters))]
        public static void SerializingProducer_Produce_DeliveryHandler(string bootstrapServers, string singlePartitionTopic, string partitionedTopic)
        {
            var producerConfig = new Dictionary<string, object> 
            { 
                { "bootstrap.servers", bootstrapServers }
            };

            int count = 0;
            Action<Message<string, string>> dh = (Message<string, string> dr) =>
            {
                Assert.Equal(ErrorCode.NoError, dr.Error.Code);
                Assert.Equal((Partition)0, dr.Partition);
                Assert.Equal(singlePartitionTopic, dr.Topic);
                Assert.True(dr.Offset >= 0);
                Assert.Equal($"test key {count}", dr.Key);
                Assert.Equal($"test val {count}", dr.Value);
                Assert.Equal(TimestampType.CreateTime, dr.Timestamp.Type);
                Assert.True(Math.Abs((DateTime.UtcNow - dr.Timestamp.UtcDateTime).TotalMinutes) < 1.0);
                count += 1;
            };

            using (var producer = new Producer<string, string>(producerConfig, new StringSerializer(Encoding.UTF8), new StringSerializer(Encoding.UTF8)))
            {
                producer.Produce(dh, singlePartitionTopic, 0, "test key 0", "test val 0", Timestamp.Default, null);
                producer.Produce(dh, singlePartitionTopic, "test key 1", "test val 1");
                producer.Flush(TimeSpan.FromSeconds(10));
            }

            Assert.Equal(2, count);
        }
    }
}