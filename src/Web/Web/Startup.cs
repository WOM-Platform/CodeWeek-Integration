using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace WomPlatform.Web.Api {

    public class Startup {

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services) {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddMvcOptions(opts => {
                    opts.AllowEmptyInputInBodyModelBinding = true;
                })
                .AddJsonOptions(opts => {
                    opts.SerializerSettings.Culture = CultureInfo.InvariantCulture;
                    opts.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                    opts.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    opts.SerializerSettings.DateParseHandling = DateParseHandling.DateTime;
                    opts.SerializerSettings.Formatting = Formatting.None;
                    opts.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

            services.AddDbContext<DataContext>(o => {
                var dbSection = Configuration.GetSection("Database");
                var host = dbSection["Host"];
                var port = Convert.ToInt32(dbSection["Port"]);
                var username = dbSection["Username"];
                var password = dbSection["Password"];
                var schema = dbSection["Schema"];
                var connectionString = string.Format(
                    "server={0};port={1};uid={2};pwd={3};database={4};Old Guids=false",
                    host, port, username, password, schema
                );

                o.UseMySQL(connectionString);
            });

            // Add services to dependency registry
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<KeyManager>();
            services.AddScoped<DataContext>();
            services.AddScoped(provider => {
                var keyManager = provider.GetRequiredService<KeyManager>();
                Console.WriteLine("Private {0}", keyManager.InstrumentPrivateKey);
                Console.WriteLine("Public {0}", keyManager.RegistryPublicKey);
                return new Connector.Client(provider.GetRequiredService<ILoggerFactory>(), keyManager.RegistryPublicKey) {
                    TestMode = true
                };
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseMvc();
        }
    }
}
