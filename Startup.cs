using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;

namespace Deestone
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
      services
        .AddMvc()
        .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
        .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());
    }

    public static string ConnectionStrings(string conn = "")
    {
      try
      {
        var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

        var configuration = builder.Build();

        if (conn == "")
        {
          return configuration["ConnectionStrings:Default"];
        }
        else
        {
          // if (conn == "BOM")
          // {
          //   return configuration["ConnectionStrings:BOM_DEV"];
          // }

          return configuration["ConnectionStrings:" + conn];
        }
      }
      catch (Exception)
      {
        return "";
      }
    }

    public static string GetTokenSecret()
    {
      return "FjM3VNkXdptUaTS44BC6gUHfPvkHHkKCrxG3mesDDnHfpSzZb7";
    }

    public static string GetTokenName()
    {
      return "deestone-token";
    }

    public static string GetConfig(string config)
    {
      try
      {
        var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

        var configuration = builder.Build();

        if (config == "")
        {
          throw new Exception("CONFIG_NOT_FOUND");
        }
        else
        {
          config = "BOM_DEV";
          return configuration[config];
        }
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
      }

      app.UseStaticFiles();
      app.UseMvc(routes =>
      {
        routes.MapRoute(
                  name: "default",
                  template: "{controller=Dashboard}/{action=Index}/{id?}"
              );
      });
    }
  }
}
