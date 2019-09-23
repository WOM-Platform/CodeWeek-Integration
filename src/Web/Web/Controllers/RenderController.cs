using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WomPlatform.Web.Api.Controllers {

    [Route("render")]
    public class RenderController : Controller {

        public IActionResult Index(string url) {
            if(url == null || !url.StartsWith("https://wom.social/")) {
                return StatusCode(400);
            }

            var qrCodeData = QRCoder.PngByteQRCodeHelper.GetQRCode(url, QRCoder.QRCodeGenerator.ECCLevel.M, 15);
            return File(qrCodeData, "image/png");
        }

    }

}
