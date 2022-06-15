using Aliyun.Acs.Core;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aliyun.Acs.R_kvstore.Model.V20150101;
using Aliyun.Acs.Core.Exceptions;
using static Aliyun.Acs.R_kvstore.Model.V20150101.DescribeInstancesResponse;
using System.Reflection;
using static Aliyun.Acs.R_kvstore.Model.V20150101.DescribeInstancesResponse.DescribeInstances_KVStoreInstance;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;

namespace NetCorePal.AliyunExporter.Aliyun
{
    public class AliyunRedisInfoSource : AliyunInfoSourceBase
    {
        public AliyunRedisInfoSource(DefaultAcsClient client, ILogger<AliyunRedisInfoSource> logger, IMemoryCache cache) : base(client, logger, cache)
        {
        }

        const string MetricName = "aliyun_meta_redis_info";
        #region
        static string[] labelNames = new string[] { "ArchitectureType", "Bandwidth", "Capacity", "ChargeType", "Config", "ConnectionDomain", "Connections", "CreateTime",
            "EndTime", "EngineVersion", "HasRenewChangeOrder", "InstanceClass", "InstanceId", "InstanceName", "InstanceStatus", "InstanceType", "IsRds", "NetworkType",
            "NodeType", "PackageType", "Port", "PrivateIp", "QPS", "RegionId", "UserName", "VSwitchId", "VpcId", "ZoneId", "Tags", "ResourceType" };

        public override string ProjectName => "acs_kvstore";

        private string[] GetLabelValues(DescribeInstances_KVStoreInstance instance)
        {
            return new string[] {
                instance.ArchitectureType.ToStringOrEmpty(),
                instance.Bandwidth.ToStringOrEmpty(),
                instance.Capacity.ToStringOrEmpty(),
                instance.ChargeType.ToStringOrEmpty(),
                instance.Config.ToStringOrEmpty(),
                instance.ConnectionDomain.ToStringOrEmpty(),
                instance.Connections.ToStringOrEmpty(),
                instance.CreateTime.ToStringOrEmpty(),
                instance.EndTime.ToStringOrEmpty(),
                instance.EngineVersion.ToStringOrEmpty(),
                instance.HasRenewChangeOrder.ToStringOrEmpty(),
                instance.InstanceClass.ToStringOrEmpty(),
                instance.InstanceId.ToStringOrEmpty(),
                instance.InstanceName.ToStringOrEmpty(),
                instance.InstanceStatus.ToStringOrEmpty(),
                instance.InstanceType.ToStringOrEmpty(),
                instance.IsRds.ToStringOrEmpty(),
                instance.NetworkType.ToStringOrEmpty(),
                instance.NodeType.ToStringOrEmpty(),
                instance.PackageType.ToStringOrEmpty(),
                instance.Port.ToStringOrEmpty(),
                instance.PrivateIp.ToStringOrEmpty(),
                instance.QPS.ToStringOrEmpty(),
                instance.RegionId.ToStringOrEmpty(),
                instance.UserName.ToStringOrEmpty(),
                instance.VSwitchId.ToStringOrEmpty(),
                instance.VpcId.ToStringOrEmpty(),
                instance.ZoneId.ToStringOrEmpty(),
                TagsToLabelValue(instance.Tags), "redis" };
        }
        #endregion

        private string TagsToLabelValue(List<DescribeInstances_Tag> tags)
        {
            if (tags == null) { return string.Empty; }

            return string.Join(",", tags.Select(t => $"{t.Key}-{t._Value}"));
        }

        public override void Load(MetricFactory metricFactory)
        {
            var instances = GetInstances();
            var gauge = metricFactory.CreateGauge(MetricName, MetricName, labelNames);
            instances.ForEach(p =>
            {
                gauge.WithLabels(GetLabelValues(p)).Set(1);
                //commonInfo.WithLabels(new string[] { "redis", p.InstanceId, p.InstanceName, TagsToLabelValue(p.Tags) }).Set(1);
            });
        }

        List<DescribeInstances_KVStoreInstance> GetInstances()
        {
            return cache.GetOrCreate($"{MetricName}", cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = this.cacheTime;
                List<DescribeInstances_KVStoreInstance> instances = new List<DescribeInstances_KVStoreInstance>();
                var request = new DescribeInstancesRequest();
                request.PageNumber = 1;
                int pageNumber = 1;
                request.PageSize = 100;
                while (pageNumber < 100)
                {
                    var response = Policy.Handle<Exception>().Retry(3).Execute(() => this.client.GetAcsResponse(request));
                    instances.AddRange(response.Instances);
                    logger.LogDebug("读取到资源数据{MetricName}TotalCount:{TotalCount},PageNumber:{pageNumber},InstancesCount:{InstancesCount}", MetricName, response.TotalCount, request.PageNumber, response.Instances?.Count);
                    if (HasMorePages(response.TotalCount, request.PageSize.Value, pageNumber))
                    {
                        pageNumber += 1;
                        request.PageNumber = pageNumber;
                    }
                    else
                    {
                        break;
                    }
                }
                return instances;
            });
        }

        public override void GetValues(string instanceId, out string instanceName, out string tags)
        {
            var r = GetInstances();
            var instance = r.FirstOrDefault(p => p.InstanceId == instanceId);
            instanceName = instance?.InstanceName.ToStringOrEmpty();
            tags = TagsToLabelValue(instance?.Tags);
        }
    }
}
