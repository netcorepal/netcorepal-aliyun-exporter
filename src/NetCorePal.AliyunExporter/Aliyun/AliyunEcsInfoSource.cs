using Aliyun.Acs.Core;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aliyun.Acs.Ecs.Model.V20140526;
using Aliyun.Acs.Core.Exceptions;
using static Aliyun.Acs.Ecs.Model.V20140526.DescribeInstancesResponse;
using System.Reflection;
using static Aliyun.Acs.Ecs.Model.V20140526.DescribeInstancesResponse.DescribeInstances_Instance;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;

namespace NetCorePal.AliyunExporter.Aliyun
{
    public class AliyunEcsInfoSource : AliyunInfoSourceBase
    {
        public AliyunEcsInfoSource(DefaultAcsClient client, ILogger<AliyunEcsInfoSource> logger, IMemoryCache cache) : base(client, logger, cache)
        {
        }

        const string MetricName = "aliyun_meta_ecs_info";
        #region
        static string[] labelNames = new string[] { "AutoReleaseTime", "ClusterId", "CreationTime", "CreditSpecification", "DeletionProtection",
            "DeploymentSetId", "Description", "DeviceAvailable", "ExpiredTime", "GPUAmount", "GPUSpec", "HostName", "ImageId", "InnerIpAddress",
            "InstanceChargeType", "InstanceId", "InstanceName", "InstanceNetworkType", "InstanceType", "InstanceTypeFamily", "InternetChargeType",
            "InternetMaxBandwidthIn", "InternetMaxBandwidthOut", "IoOptimized","Cpu", "Memory", "OSName", "OSNameEn", "OSType", "PublicIpAddress", "Recyclable",
            "RegionId", "ResourceGroupId", "SaleCycle", "SerialNumber", "SpotStrategy", "StartTime", "Status", "StoppedMode", "VlanId", "VpcAttributes",
            "ZoneId", "Tags", "ResourceType" };

        public override string ProjectName => "acs_ecs_dashboard";

        private string[] GetLabelValues(DescribeInstances_Instance instance)
        {
            return new string[] {
                instance.AutoReleaseTime.ToStringOrEmpty(),
                instance.ClusterId.ToStringOrEmpty(),
                instance.CreationTime.ToStringOrEmpty(),
                instance.CreditSpecification.ToStringOrEmpty(),
                instance.DeletionProtection.ToStringOrEmpty(),
                instance.DeploymentSetId.ToStringOrEmpty(),
                instance.Description.ToStringOrEmpty(),
                instance.DeviceAvailable.ToStringOrEmpty(),
                instance.ExpiredTime.ToStringOrEmpty(),
                instance.GPUAmount.ToStringOrEmpty(),
                instance.GPUSpec.ToStringOrEmpty(),
                instance.HostName.ToStringOrEmpty(),
                instance.ImageId.ToStringOrEmpty(),
                instance.InnerIpAddress.ToLabelValue(),
                instance.InstanceChargeType.ToStringOrEmpty(),
                instance.InstanceId.ToStringOrEmpty(),
                instance.InstanceName.ToStringOrEmpty(),
                instance.InstanceNetworkType.ToStringOrEmpty(),
                instance.InstanceType.ToStringOrEmpty(),
                instance.InstanceTypeFamily.ToStringOrEmpty(),
                instance.InternetChargeType.ToStringOrEmpty(),
                instance.InternetMaxBandwidthIn.ToStringOrEmpty(),
                instance.InternetMaxBandwidthOut.ToStringOrEmpty(),
                instance.IoOptimized.ToStringOrEmpty(),
                instance.Cpu.ToStringOrEmpty(),
                instance.Memory.ToStringOrEmpty(),
                instance.OSName.ToStringOrEmpty(),
                instance.OSNameEn.ToStringOrEmpty(),
                instance.OSType.ToStringOrEmpty(),
                instance.PublicIpAddress.ToLabelValue(),
                instance.Recyclable.ToStringOrEmpty(),
                instance.RegionId.ToStringOrEmpty(),
                instance.ResourceGroupId.ToStringOrEmpty(),
                instance.SaleCycle.ToStringOrEmpty(),
                instance.SerialNumber.ToStringOrEmpty(),
                instance.SpotStrategy.ToStringOrEmpty(),
                instance.StartTime.ToStringOrEmpty(),
                instance.Status.ToStringOrEmpty(),
                instance.StoppedMode.ToStringOrEmpty(),
                instance.VlanId.ToStringOrEmpty(),
                instance.VpcAttributes.PrivateIpAddress.ToLabelValue(),
                instance.ZoneId.ToStringOrEmpty(),
                TagsToLabelValue(instance.Tags), "ecs" };
        }
        #endregion

        private string TagsToLabelValue(List<DescribeInstances_Tag> tags)
        {
            if (tags == null) { return string.Empty; }

            return string.Join(",", tags.Select(t => $"{t.TagKey}-{t.TagValue}"));
        }

        public override void Load(MetricFactory metricFactory)
        {
            var instances = GetInstances();
            var gauge = metricFactory.CreateGauge(MetricName, MetricName, labelNames);
            //var commonInfo = GetCommonInfoGauge(metricFactory);
            instances.ForEach(p =>
                {
                    gauge.WithLabels(GetLabelValues(p)).Set(1);
                    //commonInfo.WithLabels(new string[] { "ecs", p.InstanceId, p.InstanceName, TagsToLabelValue(p.Tags) }).Set(1);
                });
        }

        private List<DescribeInstances_Instance> GetInstances()
        {
            return cache.GetOrCreate($"{MetricName}", cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = this.cacheTime;
                List<DescribeInstances_Instance> instances = new List<DescribeInstances_Instance>();
                var request = new DescribeInstancesRequest();
                request.PageNumber = 1;
                int pageNumber = 1;
                request.PageSize = 100;
                while (pageNumber < 100)
                {
                    var response = Policy.Handle<Exception>().Retry(3).Execute(() => this.client.GetAcsResponse(request));
                    logger.LogDebug("读取到资源数据{MetricName}TotalCount:{TotalCount},PageNumber:{pageNumber},InstancesCount:{InstancesCount}", MetricName, response.TotalCount, request.PageNumber, response.Instances?.Count);
                    instances.AddRange(response.Instances);
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
