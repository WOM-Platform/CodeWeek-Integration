using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels {

    public class Certificate {

        public long CertificateId { get; set; }

        public string Url { get; set; }

        public DateTime RegistrationDate { get; set; }

    }

}
