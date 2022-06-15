using Aliyun.Acs.Core;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
namespace NetCorePal.AliyunExporter.Aliyun
{
    public abstract class AliyunSourceBase : IMetricSource
    {
        protected DefaultAcsClient client;

        protected ILogger logger;

        public AliyunSourceBase(DefaultAcsClient client,ILogger logger)
        {
            this.client = client;
            this.logger = logger;
        }


        public abstract void Load(MetricFactory metricFactory);


        protected bool HasMorePages(int? totalCount, int pageSize, int pageNumber)
        {
            if (!totalCount.HasValue) { return false; }
            return pageSize * pageNumber < totalCount.Value;
        }
    }
}
