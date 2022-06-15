using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using NetCorePal.AliyunExporter.Aliyun;
using static Aliyun.Acs.Ecs.Model.V20140526.ModifyReservedInstancesRequest;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

builder.Configuration.AddJsonFile("config.json", optional: true);
builder.Services.AddSingleton<IClientProfile>(DefaultProfile.GetProfile(builder.Configuration.GetValue<string>("AliyunAcs:RegionId"), builder.Configuration.GetValue<string>("AliyunAcs:AccessKeyId"), builder.Configuration.GetValue<string>("AliyunAcs:Secret")));
builder.Services.AddSingleton(p => new DefaultAcsClient(p.GetService<IClientProfile>()));
builder.Services.Configure<AliyunCmsSourceOptions>(builder.Configuration.GetSection("AliyunCmsSourceOptions"));
builder.Services.AddScoped<AliyunInfoSourceBase, AliyunEcsInfoSource>();
builder.Services.AddScoped<AliyunInfoSourceBase, AliyunLoadBalancerInfoSource>();
builder.Services.AddScoped<AliyunInfoSourceBase, AliyunRdsInfoSource>();
builder.Services.AddScoped<AliyunInfoSourceBase, AliyunRedisInfoSource>();
builder.Services.AddScoped<AliyunSourceBase, AliyunEcsInfoSource>();
builder.Services.AddScoped<AliyunSourceBase, AliyunLoadBalancerInfoSource>();
builder.Services.AddScoped<AliyunSourceBase, AliyunRdsInfoSource>();
builder.Services.AddScoped<AliyunSourceBase, AliyunRedisInfoSource>();
builder.Services.AddScoped<AliyunSourceBase, AliyunCmsSource>();

builder.Services.AddHealthChecks();
var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/healthz");
    endpoints.MapControllerRoute(
        name: "metrics",
        pattern: "/metrics",
        defaults: new {controller="Exporter",action= "Metrics" });
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});
app.Run();
