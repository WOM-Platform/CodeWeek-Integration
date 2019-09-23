using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.ViewModel {

    public class ConversionModelViewModel {

        public enum ConversionError {
            EventUrlInvalid,
            CertificateUrlInvalid,
            CertificateAlreadyConverted,
            CertificateNotExisting,
            EventUrlFailedToCheck,
            InternalWomGenerationError,
            EventCertificateIdDifform
        }

        public ConversionError? Error { get; set; } = null;

        public string CertificateUrl { get; set; }

        public string EventPageUrl { get; set; }

    }

}
