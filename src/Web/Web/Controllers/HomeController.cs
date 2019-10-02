using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

            var viewModel = GetRedirectData<ConversionModelViewModel>() ?? new ConversionModelViewModel();

            return View("Index", viewModel);
        }

        private static readonly Regex _certificateUrlMatcher = new Regex(
            @"^http(s)?://codeweek-s3.s3.amazonaws.com/certificates/(?'id'\d*)-[^\.]*.pdf",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture
        );

        private static readonly Regex _eventPageUrlMatcher = new Regex(
            @"^http(s)?://codeweek.eu/view/(?'id'\d*)/",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture
        );

        private static readonly Regex _eventPageExtractor = new Regex(
            @"geoposition""\s*:\s*""(?<lat>[\d.]*)\s*,\s*(?<long>[\d.]*)""",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture
        );

        private static readonly HttpClient _client = new HttpClient();

        static HomeController() {
            _client.DefaultRequestHeaders.Referrer = new Uri("https://wom.social");
            _client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("WomConverter", "1.0"));
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("en-US"));
        }

        [HttpPost("/convert")]
        public async Task<IActionResult> Process(string certificateUrl, string eventPageUrl) {
            Logger.LogInformation("Processing certificate {0}", certificateUrl);

            var redirectModel = new ConversionModelViewModel {
                CertificateUrl = certificateUrl,
                EventPageUrl = eventPageUrl
            };

            var certificateMatch = _certificateUrlMatcher.Match(certificateUrl ?? string.Empty);
            if(!certificateMatch.Success) {
                redirectModel.Error = ConversionModelViewModel.ConversionError.CertificateUrlInvalid;
                return RedirectToActionWithData(nameof(Index), redirectModel);
            }
            var eventPageMatch = _eventPageUrlMatcher.Match(eventPageUrl ?? string.Empty);
            if(!eventPageMatch.Success) {
                redirectModel.Error = ConversionModelViewModel.ConversionError.EventUrlInvalid;
                return RedirectToActionWithData(nameof(Index), redirectModel);
            }

            int certificateId = int.Parse(certificateMatch.Groups["id"].Value, CultureInfo.InvariantCulture);
            int eventId = int.Parse(eventPageMatch.Groups["id"].Value, CultureInfo.InvariantCulture);
            if(certificateId != eventId) {
                redirectModel.Error = ConversionModelViewModel.ConversionError.EventCertificateIdDifform;
                return RedirectToActionWithData(nameof(Index), redirectModel);
            }
            if(certificateId < 200000) {
                redirectModel.Error = ConversionModelViewModel.ConversionError.EventTooOld;
                return RedirectToActionWithData(nameof(Index), redirectModel);
            }

            if(Database.Certificates.Where(c => c.CertificateId == certificateId).Count() > 0) {
                Logger.LogError("Certificate {0} already converted", certificateId);

                redirectModel.Error = ConversionModelViewModel.ConversionError.CertificateAlreadyConverted;
                return RedirectToActionWithData(nameof(Index), redirectModel);
            }

            var certificateCheckResponse = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, certificateUrl));
            if(!certificateCheckResponse.IsSuccessStatusCode) {
                Logger.LogError("Certificate does not exist on server, status {0}", certificateCheckResponse.StatusCode);

                redirectModel.Error = ConversionModelViewModel.ConversionError.CertificateNotExisting;
                return RedirectToActionWithData(nameof(Index), redirectModel);
            }
            Logger.LogDebug("Certificate validated, HEAD request returns status OK");

            var eventUrlCheckResponse = await _client.GetAsync(eventPageUrl);
            if(!eventUrlCheckResponse.IsSuccessStatusCode) {
                Logger.LogError("Failed to check event page URL, status {0}", eventUrlCheckResponse.StatusCode);

                redirectModel.Error = ConversionModelViewModel.ConversionError.EventUrlFailedToCheck;
                return RedirectToActionWithData(nameof(Index), redirectModel);
            }
            var eventUrlCheckContents = await eventUrlCheckResponse.Content.ReadAsStringAsync();
            var eventUrlMatch = _eventPageExtractor.Match(eventUrlCheckContents);
            if(!eventUrlMatch.Success) {
                Logger.LogError("Failed to find geoposition values in event page");

                redirectModel.Error = ConversionModelViewModel.ConversionError.EventUrlFailedToCheck;
                return RedirectToActionWithData(nameof(Index), redirectModel);
            }
            double eventLat = double.Parse(eventUrlMatch.Groups["lat"].Value, CultureInfo.InvariantCulture);
            double eventLong = double.Parse(eventUrlMatch.Groups["long"].Value, CultureInfo.InvariantCulture);

            Logger.LogInformation("Generating vouchers for event {0} at location {1},{2}", eventId, eventLat, eventLong);

            try {
                var instrument = WomClient.CreateInstrument(KeyManager.SourceId, KeyManager.InstrumentPrivateKey);
                var (otc, password) = await instrument.RequestVouchers(new Connector.Models.VoucherCreatePayload.VoucherInfo[] {
                    new Connector.Models.VoucherCreatePayload.VoucherInfo {
                        Aim = "E",
                        Count = 60,
                        Latitude = eventLat,
                        Longitude = eventLong,
                        Timestamp = DateTime.UtcNow
                    }
                });

                // Register conversion
                await Database.Certificates.AddAsync(new DatabaseModels.Certificate {
                    CertificateId = certificateId,
                    CertificateUrl = certificateUrl,
                    EventPageUrl = eventPageUrl,
                    RegistrationDate = DateTime.UtcNow
                });
                int changed = await Database.SaveChangesAsync();
                if(changed != 1) {
                    Logger.LogError("Certificate registration returned number of changes {0} != 1", changed);
                }

                return View("Vouchers", new ConversionResult {
                    OtcCode = otc.ToString("N"),
                    Password = password
                });
            }
            catch(Exception ex) {
                Logger.LogError(0, ex, "Failed to generate vouchers");

                redirectModel.Error = ConversionModelViewModel.ConversionError.InternalWomGenerationError;
                return RedirectToActionWithData(nameof(Index), redirectModel);
            }
        }

        private const string RedirectTempViewModelKey = "redirectData";

        private IActionResult RedirectToActionWithData(string actionName, object data) {
            if(data != null) {
                TempData[RedirectTempViewModelKey] = JsonConvert.SerializeObject(data);
            }
            return RedirectToAction(actionName);
        }

        private T GetRedirectData<T>() where T : class {
            var data = TempData[RedirectTempViewModelKey]?.ToString();
            if(data == null) {
                return null;
            }
            return JsonConvert.DeserializeObject(data, typeof(T)) as T;
        }

    }

}
