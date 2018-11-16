﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Couchbase.Core;
using Couchbase.Core.Transcoders;
using Couchbase.IO.Converters;
using Couchbase.IO.Operations;
using NUnit.Framework;
using Wintellect;

namespace Couchbase.Tests.IO.Operations
{
    [TestFixture]
    public sealed class GetSetPerformanceTests : OperationTestBase
    {
        [Test]
        public void Test_Timed_Execution()
        {
            var converter = new DefaultConverter();
            var transcoder = new DefaultTranscoder(converter);
            var vbucket = GetVBucket();
            int n = 1000; //set to a higher # if needed

            using (new OperationTimer())
            {
                var key = string.Format("key{0}", 111);

                for (var i = 0; i < n; i++)
                {
                    var set = new Set<int?>(key, 111, vbucket, transcoder, OperationLifespanTimeout);
                    var get = new Get<int?>(key, vbucket, transcoder, OperationLifespanTimeout);

                    var result = IOService.Execute(set);
                    Assert.IsTrue(result.Success);

                    var result1 = IOService.Execute(get);
                    Assert.IsTrue(result1.Success);
                    Assert.AreEqual(111, result1.Value);
                }
            }
        }

       [Test]
        public void Test_Timed_Execution_Parallel()
        {
            var options = new ParallelOptions { MaxDegreeOfParallelism = 4 };
            var converter = new DefaultConverter();
            var transcoder = new DefaultTranscoder(converter);
            var vbucket = GetVBucket();
            var n = 1000;//set to a higher # if needed

            using (new OperationTimer())
            {
                Parallel.For(0, n, options, i =>
                {
                    var key = string.Format("key{0}", i);
                    var set = new Set<int?>(key, i, vbucket, transcoder, OperationLifespanTimeout);
                    var result = IOService.Execute(set);
                    Assert.IsTrue(result.Success);

                    var get = new Get<int?>(key, vbucket, transcoder, OperationLifespanTimeout);
                    var result1 = IOService.Execute(get);
                    Assert.IsTrue(result1.Success);
                    Assert.AreEqual(i, result1.Value);
                });
            }
        }

#if NET452
        [Test]
        public void Test_Timed_Execution_Parallel_Client()
        {
            var options = new ParallelOptions { MaxDegreeOfParallelism = 4 };
            var n = 1000;//set to a higher # if needed

            using (var cluster = new Cluster("couchbaseClients/couchbase"))
            {
                using (var bucket = cluster.OpenBucket())
                {
                    using (new OperationTimer())
                    {
                        var temp = bucket;
                        Parallel.For(0, n, options, i =>
                        {
                            var key = string.Format("key{0}", i);
                            var value = (int?) i;
                            var result = temp.Upsert(key, value);
                            Assert.IsTrue(result.Success);

                            var result1 = temp.Get<int?>(key);
                            Assert.IsTrue(result1.Success);
                            Assert.AreEqual(i, result1.Value);
                        });
                    }
                }
            }
        }
#endif
    }
}
