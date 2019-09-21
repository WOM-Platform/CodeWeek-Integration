using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    [Route("")]
    public class HomeController : Controller {

        public HomeController(
            IConfiguration configuration,
            KeyManager keyManager,
            DataContext database,
            ILogger<HomeController> logger
        ) {
            Configuration = configuration;
            KeyManager = keyManager;
            Database = database;
            Logger = logger;
        }

        protected IConfiguration Configuration { get; }

        protected KeyManager KeyManager { get; }

        protected DataContext Database { get; }

        protected ILogger<HomeController> Logger { get; }

        [HttpGet()]
        public IActionResult Index() {
            Logger.LogDebug("Index view");

            return View("Index");
        }

    }

}
