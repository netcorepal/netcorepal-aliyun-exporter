using System;
using System.Collections.Generic;
using System.Text;

namespace NetCorePal.AliyunExporter.Aliyun
{
    public class AliyunCmsSourceOptions
    {
        /// <summary>
        /// 并发请求数，默认3
        /// </summary>
        public int ParallelCount { get; set; } = 3;
        public Dictionary<string, List<MetricsOptions>> Metrics { get; set; }
    }
    public class MetricsOptions
    {
        public string Name { get; set; }
        public int Period { get; set; }
    }
}
