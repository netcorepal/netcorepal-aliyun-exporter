using Aliyun.Acs.Core;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aliyun.Acs.Core.Exceptions;
using static Aliyun.Acs.Slb.Model.V20140515.DescribeLoadBalancersResponse;
using System.Reflection;
using static Aliyun.Acs.Slb.Model.V20140515.DescribeLoadBalancersResponse.DescribeLoadBalancers_LoadBalancer;
using Aliyun.Acs.Slb.Model.V20140515;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;

namespace NetCorePal.AliyunExporter.Aliyun
{
    public class AliyunLoadBalancerInfoSource : AliyunInfoSourceBase
    {
        public AliyunLoadBalancerInfoSource(DefaultAcsClient client, ILogger<AliyunLoadBalancerInfoSource> logger, IMemoryCache cache) : base(client, logger, cache)
        {
        }

        const string MetricName = "aliyun_meta_slb_info";
        #region
        static string[] labelNames = new string[] { "Address", "AddressIPVersion", "AddressType", "CreateTime", "CreateTimeStamp", "InternetChargeType", "LoadBalancerId",
            "LoadBalancerName", "LoadBalancerStatus", "MasterZoneId", "NetworkType", "PayType", "RegionId", "RegionIdAlias", "ResourceGroupId", "SlaveZoneId", "VSwitchId", "VpcId", "Tags", "ResourceType" };

        public override string ProjectName => "acs_slb_dashboard";

        private string[] GetLabelValues(DescribeLoadBalancers_LoadBalancer instance)
        {
            return new string[] {
                instance.Address.ToStringOrEmpty(),
                instance.AddressIPVersion.ToStringOrEmpty(),
                instance.AddressType.ToStringOrEmpty(),
                instance.CreateTime.ToStringOrEmpty(),
                instance.CreateTimeStamp.ToStringOrEmpty(),
                instance.InternetChargeType.ToStringOrEmpty(),
                instance.LoadBalancerId.ToStringOrEmpty(),
                instance.LoadBalancerName.ToStringOrEmpty(),
                instance.LoadBalancerStatus.ToStringOrEmpty(),
                instance.MasterZoneId.ToStringOrEmpty(),
                instance.NetworkType.ToStringOrEmpty(),
                instance.PayType.ToStringOrEmpty(),
                instance.RegionId.ToStringOrEmpty(),
                instance.RegionIdAlias.ToStringOrEmpty(),
                instance.ResourceGroupId.ToStringOrEmpty(),
                instance.SlaveZoneId.ToStringOrEmpty(),
                instance.VSwitchId.ToStringOrEmpty(),
                instance.VpcId.ToStringOrEmpty(),
                TagsToLabelValue(instance.Tags), "slb" };
        }

        #endregion
        private string TagsToLabelValue(List<DescribeLoadBalancers_Tag> tags)
        {
            if (tags == null) { return string.Empty; }
            return string.Join(",", tags.Select(t => $"{t.TagKey}-{t.TagValue}"));
        }

        public override void Load(MetricFactory metricFactory)
        {
            var instances = GetInstances();
            var gauge = metricFactory.CreateGauge(MetricName, MetricName, labelNames);
            instances.ForEach(p =>
                {
                    gauge.WithLabels(GetLabelValues(p)).Set(1);
                    //commonInfo.WithLabels(new string[] { "slb", p.LoadBalancerId, p.LoadBalancerName, TagsToLabelValue(p.Tags) }).Set(1);
                });
        }

        List<DescribeLoadBalancers_LoadBalancer> GetInstances()
        {
            return cache.GetOrCreate($"{MetricName}", cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = this.cacheTime;
                List<DescribeLoadBalancers_LoadBalancer> instances = new List<DescribeLoadBalancers_LoadBalancer>();
                var request = new DescribeLoadBalancersRequest();
                request.PageNumber = 1;
                int pageNumber = 1;
                request.PageSize = 100;
                while (pageNumber < 100)
                {
                    var response = Policy.Handle<Exception>().Retry(3).Execute(() => this.client.GetAcsResponse(request));
                    logger.LogDebug("读取到资源数据{MetricName}TotalCount:{TotalCount},PageNumber:{pageNumber},InstancesCount:{InstancesCount}", MetricName, response.TotalCount, request.PageNumber, response.LoadBalancers?.Count);
                    instances.AddRange(response.LoadBalancers);
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
            var instance = r.FirstOrDefault(p => p.LoadBalancerId == instanceId);
            instanceName = instance?.LoadBalancerName.ToStringOrEmpty();
            tags = TagsToLabelValue(instance?.Tags);
        }
    }
}
