using Aliyun.Acs.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetCorePal.AliyunExporter.Aliyun
{
    public abstract class AliyunInfoSourceBase : AliyunSourceBase
    {
        protected IMemoryCache cache;

        protected TimeSpan cacheTime = TimeSpan.FromHours(15);


        public abstract string ProjectName { get; }


        public abstract void GetValues(string instanceId, out string instanceName, out string tags);
        public AliyunInfoSourceBase(DefaultAcsClient client, ILogger logger, IMemoryCache cache) : base(client, logger)
        {
            this.cache = cache;
        }


        protected Gauge GetCommonInfoGauge(MetricFactory metricFactory)
        {
            return metricFactory.CreateGauge("aliyun_meta_resource_info", "aliyun_meta_resource_info", new string[] { "ResourceType", "InstanceId", "InstanceName", "Tags" });
        }
    }
}
