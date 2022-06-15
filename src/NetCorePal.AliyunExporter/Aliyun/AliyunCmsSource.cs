using Aliyun.Acs.Core;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Text;
using Aliyun.Acs.Cms.Model.V20180308;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Polly;
using Microsoft.Extensions.Caching.Memory;

namespace NetCorePal.AliyunExporter.Aliyun
{
    public class AliyunCmsSource : AliyunSourceBase
    {
        AliyunCmsSourceOptions options;
        IEnumerable<AliyunInfoSourceBase> aliyunInfoSources;
        IMemoryCache cache;
        public AliyunCmsSource(DefaultAcsClient client, ILogger<AliyunCmsSource> logger, IOptionsSnapshot<AliyunCmsSourceOptions> options, IEnumerable<AliyunInfoSourceBase> aliyunInfoSources, IMemoryCache cache) : base(client, logger)
        {
            this.options = options.Value;
            this.aliyunInfoSources = aliyunInfoSources;
            this.cache = cache;
        }

        readonly string[] names = new string[] { "timestamp", "Maximum", "Minimum", "Average", "Sum" };
        const string MetricNameTemplate = "aliyun_{0}_{1}";


        public override void Load(MetricFactory metricFactory)
        {
            Parallel.ForEach(options.Metrics, new ParallelOptions { MaxDegreeOfParallelism = options.ParallelCount }, item =>
            {
                foreach (var metricsOptions in item.Value)
                {
                    Load(metricFactory, item.Key, metricsOptions);
                }
            });
        }
        void Load(MetricFactory metricFactory, string project, MetricsOptions options)
        {
            var metricName = string.Format(MetricNameTemplate, project, options.Name);
            var infoSource = aliyunInfoSources.First(p => p.ProjectName == project);
            var points = GetDataPoints(project, options);

            var cacheKey = $"{project}_{metricName}_count";
            if (cache.TryGetValue(cacheKey, out int lastCount))
            {
                if (lastCount != points.Count)
                {
                    logger.LogWarning("读取到指标数据条数与上次不一致:{project}_{Name},CurrentCount:{CurrentCount},LastCount:{LastCount}", project, options.Name, points.Count, lastCount);
                }
            }
            cache.Set(cacheKey, points.Count);

            if (points.Count > 1)
            {
                var labelNames = points[0].Keys.Where(p => !names.Contains(p)).ToList();
                labelNames.AddRange(new string[] { "instanceName", "tags" });
                var measure = points[0].Keys.Contains("measure") ? points[0]["measure"].ToString() : "Average";
                var gauge = metricFactory.CreateGauge(metricName, metricName, labelNames.ToArray());
                points.ForEach(p =>
                {
                    infoSource.GetValues(p["instanceId"].ToStringOrEmpty(), out var instanceName, out var tags);
                    p.Add("instanceName", instanceName);
                    p.Add("tags", tags);
                    var r = GetLabelValues(p, labelNames.ToArray());
                    gauge.WithLabels(r.ToArray()).Set(Convert.ToDouble(p[measure]));
                });
            }
        }

        List<Dictionary<string, object>> GetDataPoints(string project, MetricsOptions options)
        {
            List<Dictionary<string, object>> dataPoints = new List<Dictionary<string, object>>();
            var request = new QueryMetricLastRequest();
            request.Metric = options.Name;
            request.Project = project;
            request.Length = "1000";
            request.Period = options.Period.ToString();
            int pageNumber = 1;
            while (pageNumber < 100)
            {
                var response = Policy.Handle<Exception>().Retry(3).Execute(() => this.client.GetAcsResponse(request));
                if (!string.IsNullOrEmpty(response.Datapoints))
                {
                    var points = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response.Datapoints);
                    dataPoints.AddRange(points);
                    logger.LogDebug("读取到指标数据:{project}_{Name},PageNumber:{pageNumber},InstancesCount:{InstancesCount},Cursor:{Cursor}", project, options.Name, pageNumber, points.Count, response.Cursor);
                }
                else
                {
                    logger.LogWarning("没有指标数据:{project}_{Name},PageNumber:{pageNumber},Datapoints为空,Cursor:{Cursor}", project, options.Name, pageNumber, response.Cursor);
                }
                if (!string.IsNullOrEmpty(response.Cursor))
                {
                    request.Cursor = response.Cursor;
                    pageNumber += 1;
                }
                else
                {
                    break;
                }
            }
            return dataPoints;
        }


        List<string> GetLabelValues(Dictionary<string, object> data, string[] labelNames)
        {
            var s = new List<string>();
            for (int i = 0; i < labelNames.Length; i++)
            {
                s.Add(data[labelNames[i]].ToStringOrEmpty());
            }
            return s;
        }
    }
}
