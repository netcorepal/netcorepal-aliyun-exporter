using System.Diagnostics;
using Aliyun.Acs.Cms.Model.V20180308;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NetCorePal.AliyunExporter.Aliyun;
using Prometheus;
using Metric = Prometheus.Metrics;

namespace NetCorePal.AliyunExporter.Controllers;

public class ExporterController : Controller
{
    ///metrics

    public IActionResult Index([FromServices] DefaultAcsClient client, [FromServices] IClientProfile clientProfile)
    {
        var request = new QueryProjectMetaRequest();
        request.PageSize = 100;
        //request.RegionId = clientProfile.GetRegionId(); //这里不需要限定区域
        var response = client.GetAcsResponse(request);
        //var r = System.Text.Encoding.Default.GetString(response.HttpResponse.Content);
        return View(response.Resources);
    }
    public IActionResult Projects([FromServices] DefaultAcsClient client, [FromRoute] string id)
    {
        var request = new QueryMetricMetaRequest();
        request.Project = id;
        request.PageSize = 100;
        var response = client.GetAcsResponse(request);
        //var r = System.Text.Encoding.Default.GetString(response.HttpResponse.Content);
        return View(response.Resources);
    }

    public Task<string> ClearInfoCache([FromServices] IMemoryCache cache)
    {
        string[] keys = new string[] { "aliyun_meta_ecs_info", "aliyun_meta_slb_info", "aliyun_meta_rds_info", "aliyun_meta_rds_info_tags", "aliyun_meta_redis_info" };
        foreach (var item in keys)
        {
            cache.Remove(item);
        }
        return Task.FromResult("已清除keys: " + string.Join(',', keys));
    }

    public async Task Metrics([FromServices] IEnumerable<AliyunSourceBase> sources, [FromServices] ILogger<ExporterController> logger)
    {
        Stopwatch watch = Stopwatch.StartNew();
        var r = Metric.NewCustomRegistry();
        MetricFactory f = Metric.WithCustomRegistry(r);
        r.AddBeforeCollectCallback(() =>
        {
            foreach (var item in sources)
            {
                item.Load(f);
            }
        });
        Response.ContentType = PrometheusConstants.ExporterContentType;
        Response.StatusCode = 200;
        await r.CollectAndExportAsTextAsync(Response.Body, HttpContext.RequestAborted);
        watch.Stop();
        logger.LogInformation("Metrics接口耗时(不包含网络传输时间):{0}ms", watch.ElapsedMilliseconds);
    }

    
}
