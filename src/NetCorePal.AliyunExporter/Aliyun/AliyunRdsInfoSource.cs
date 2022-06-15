using Aliyun.Acs.Core;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aliyun.Acs.Rds.Model.V20140815;
using Aliyun.Acs.Core.Exceptions;
using static Aliyun.Acs.Rds.Model.V20140815.DescribeDBInstancesResponse;
using System.Reflection;
using static Aliyun.Acs.Rds.Model.V20140815.DescribeDBInstancesResponse.DescribeDBInstances_DBInstance;
using static Aliyun.Acs.Rds.Model.V20140815.DescribeTagsResponse;
using Aliyun.Acs.Core.Profile;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;

namespace NetCorePal.AliyunExporter.Aliyun
{
    public class AliyunRdsInfoSource : AliyunInfoSourceBase
    {
        IClientProfile clientProfile;
        public AliyunRdsInfoSource(DefaultAcsClient client, ILogger<AliyunRdsInfoSource> logger, IMemoryCache cache, IClientProfile clientProfile) : base(client, logger, cache)
        {
            this.clientProfile = clientProfile;
        }
        const string MetricName = "aliyun_meta_rds_info";
        #region
        static string[] labelNames = new string[] { "ConnectionMode", "CreateTime", "DBInstanceClass", "DBInstanceDescription", "DBInstanceId", "DBInstanceNetType", "DBInstanceStatus",
            "DBInstanceType", "Engine", "EngineVersion", "ExpireTime", "InsId", "InstanceNetworkType", "LockMode", "LockReason", "MasterInstanceId", "MutriORsignle", "RegionId",
            "ResourceGroupId", "VpcCloudInstanceId", "ZoneId", "Tags", "ResourceType" };

        public override string ProjectName => "acs_rds_dashboard";

        private string[] GetLabelValues(DescribeDBInstances_DBInstance instance, List<DescribeTags_TagInfos> allTags)
        {
            return new string[] {
                instance.ConnectionMode.ToStringOrEmpty(),
                instance.CreateTime.ToStringOrEmpty(),
                instance.DBInstanceClass.ToStringOrEmpty(),
                instance.DBInstanceDescription.ToStringOrEmpty(),
                instance.DBInstanceId.ToStringOrEmpty(),
                instance.DBInstanceNetType.ToStringOrEmpty(),
                instance.DBInstanceStatus.ToStringOrEmpty(),
                instance.DBInstanceType.ToStringOrEmpty(),
                instance.Engine.ToStringOrEmpty(),
                instance.EngineVersion.ToStringOrEmpty(),
                instance.ExpireTime.ToStringOrEmpty(),
                instance.InsId.ToStringOrEmpty(),
                instance.InstanceNetworkType.ToStringOrEmpty(),
                instance.LockMode.ToStringOrEmpty(),
                instance.LockReason.ToStringOrEmpty(),
                instance.MasterInstanceId.ToStringOrEmpty(),
                instance.MutriORsignle.ToStringOrEmpty(),
                instance.RegionId.ToStringOrEmpty(),
                instance.ResourceGroupId.ToStringOrEmpty(),
                instance.VpcCloudInstanceId.ToStringOrEmpty(),
                instance.ZoneId.ToStringOrEmpty(),
                TagsToLabelValue(allTags.Where(p => p.DBInstanceIds.Contains(instance.DBInstanceId))), "rds" };
        }
        #endregion

        private string TagsToLabelValue(IEnumerable<DescribeTags_TagInfos> tags)
        {
            if (tags == null) { return string.Empty; }

            return string.Join(",", tags.Select(t => $"{t.TagKey}-{t.TagValue}"));
        }

        public override void Load(MetricFactory metricFactory)
        {
            var instances = GetInstances();
            var tags = GetAllTags();
            var gauge = metricFactory.CreateGauge(MetricName, MetricName, labelNames);
            //var commonInfo = GetCommonInfoGauge(metricFactory);
            instances.ForEach(p =>
            {
                gauge.WithLabels(GetLabelValues(p, tags)).Set(1);
                //commonInfo.WithLabels(new string[] { "rds", p.DBInstanceId.ToStringOrEmpty(), p.DBInstanceDescription.ToStringOrEmpty(), TagsToLabelValue(tagsResponse.Items.Where(t => t.DBInstanceIds.Contains(p.DBInstanceId))) }).Set(1);
            });

        }

        List<DescribeDBInstances_DBInstance> GetInstances()
        {
            return cache.GetOrCreate($"{MetricName}", cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = this.cacheTime;
                List<DescribeDBInstances_DBInstance> instances = new List<DescribeDBInstances_DBInstance>();
                var request = new DescribeDBInstancesRequest();
                int pageNumber = 1;
                request.PageSize = 100;
                while (pageNumber < 100)
                {
                    request.PageNumber = pageNumber;

                    var response = Policy.Handle<Exception>().Retry(3).Execute(() => this.client.GetAcsResponse(request));
                    instances.AddRange(response.Items);
                    logger.LogDebug("读取到资源数据{MetricName}TotalCount:{TotalCount},PageNumber:{pageNumber},InstancesCount:{InstancesCount}", MetricName, response.TotalRecordCount, request.PageNumber, response.Items?.Count);
                    if (HasMorePages(response.TotalRecordCount, request.PageSize.Value, pageNumber))
                    {
                        pageNumber += 1;
                    }
                    else
                    {
                        break;
                    }
                }
                return instances;
            });
        }

        List<DescribeTags_TagInfos> GetAllTags()
        {
            return cache.GetOrCreate($"{MetricName}_tags", cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = this.cacheTime;
                var tagsRequest = new DescribeTagsRequest();
                tagsRequest.RegionId = this.clientProfile.GetRegionId();
                var response = Policy.Handle<Exception>().Retry(3).Execute(() => this.client.GetAcsResponse(tagsRequest));
                return response.Items;
            });
        }

        public override void GetValues(string instanceId, out string instanceName, out string tags)
        {
            var r = GetInstances();
            var instance = r.FirstOrDefault(p => p.DBInstanceId == instanceId);
            instanceName = instance?.DBInstanceDescription.ToStringOrEmpty();
            var allTags = GetAllTags();
            tags = TagsToLabelValue(allTags.Where(p => p.DBInstanceIds.Contains(instanceId)));
        }
    }
}
