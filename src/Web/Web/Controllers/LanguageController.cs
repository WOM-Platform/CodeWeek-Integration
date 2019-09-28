using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace WomPlatform.Web.Api.Controllers {

    [Route("language")]
    public class LanguageController : Controller {

        [HttpGet("set")]
        public IActionResult Set(string language) {
            var v = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language));
            Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, v);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

    }

}
