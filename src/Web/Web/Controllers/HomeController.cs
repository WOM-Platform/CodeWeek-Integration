using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.Controllers {

    [Route("")]
    public class HomeController : Controller {

        public HomeController(
            IConfiguration configuration,
            KeyManager keyManager,
            DataContext database,
            Connector.Client womClient,
            ILogger<HomeController> logger
        ) {
            Configuration = configuration;
            KeyManager = keyManager;
            Database = database;
            WomClient = womClient;
            Logger = logger;
        }

        protected IConfiguration Configuration { get; }

        protected KeyManager KeyManager { get; }

        protected DataContext Database { get; }

        protected Connector.Client WomClient { get; }

        protected ILogger<HomeController> Logger { get; }

        [HttpGet()]
        public IActionResult Index() {
            Logger.LogDebug("Index view");

            return View("Index");
        }

        private static readonly Regex _urlMatcher = new Regex(
            @"http(s)?://codeweek-s3.s3.amazonaws.com/certificates/(?'id'\d*)-[^\.]*.pdf",
            RegexOptions.Compiled | RegexOptions.Singleline
        );

        private static readonly HttpClient _client = new HttpClient();

        [HttpPost("/convert")]
        public async Task<IActionResult> Process(string certificateUrl) {
            Logger.LogInformation("Processing certificate {0}", certificateUrl);

            var match = _urlMatcher.Match(certificateUrl);
            if(!match.Success) {
                return StatusCode(400);
            }

            int certificateId = int.Parse(match.Groups["id"].Value);
            if(Database.Certificates.Where(c => c.CertificateId == certificateId).Count() > 0) {
                Logger.LogError("Certificate {0} already converted", certificateId);
                return StatusCode(400);
            }

            var certificateCheckResponse = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, certificateUrl));
            if(!certificateCheckResponse.IsSuccessStatusCode) {
                Logger.LogError("Certificate does not exist on server, status {0}", certificateCheckResponse.StatusCode);
                return StatusCode(400);
            }
            Logger.LogDebug("Certificate validated, HEAD request returns status OK");

            var instrument = WomClient.CreateInstrument(1, KeyManager.InstrumentPrivateKey);
            var womResult = await instrument.RequestVouchers(new Connector.Models.VoucherCreatePayload.VoucherInfo[] {
                new Connector.Models.VoucherCreatePayload.VoucherInfo {
                    Aim = "C",
                    Count = 1,
                    Latitude = 42.934872,
                    Longitude = 12.609390,
                    Timestamp = DateTime.UtcNow
                }
            });

            return View("Vouchers", new ConversionResult {
                OtcCode = womResult.Otc.ToString("N"),
                Password = womResult.Password
            });
        }

    }

}
