using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;

namespace WomPlatform.Web.Api {

    public class KeyManager {

        protected IConfiguration Configuration { get; }
        protected ILogger<KeyManager> Logger { get; }

        public KeyManager(
            IConfiguration configuration,
            ILogger<KeyManager> logger
        ) {
            Configuration = configuration;
            Logger = logger;

            Logger.LogDebug(LoggingEvents.KeyManagement, "Loading registry keys");

            var keysConf = configuration.GetSection("RegistryKeys");

            if (!string.IsNullOrEmpty(keysConf["PrivateInstrumentPath"])) {
                InstrumentPrivateKey = LoadKeyFromPem<AsymmetricCipherKeyPair>(keysConf["PrivateInstrumentPath"]).Private;
                Logger.LogTrace(LoggingEvents.KeyManagement, "Private key loaded: {0}", InstrumentPrivateKey);
            }
            else {
                Logger.LogError(LoggingEvents.KeyManagement, "Private key not loaded");
            }

            if(!string.IsNullOrEmpty(keysConf["PublicRegistryPath"])) {
                RegistryPublicKey = LoadKeyFromPem<AsymmetricKeyParameter>(keysConf["PublicRegistryPath"]);
                Logger.LogTrace(LoggingEvents.KeyManagement, "Public key loaded: {0}", InstrumentPrivateKey);
            }
            else {
                Logger.LogError(LoggingEvents.KeyManagement, "Public key not loaded");
            }

            Logger.LogTrace(LoggingEvents.KeyManagement, "Registry keys loaded");
        }

        public static T LoadKeyFromPem<T>(string path) where T : class {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                using(var txReader = new StreamReader(fs)) {
                    var reader = new PemReader(txReader);
                    return reader.ReadObject() as T;
                }
            }
        }

        public static T LoadKeyFromString<T>(string pem) where T : class {
            if(string.IsNullOrWhiteSpace(pem)) {
                throw new ArgumentException("PEM cannot be empty or null", nameof(pem));
            }

            using(var sr = new StringReader(pem)) {
                var reader = new PemReader(sr);
                return reader.ReadObject() as T;
            }
        }

        public AsymmetricKeyParameter InstrumentPrivateKey { get; }

        public AsymmetricKeyParameter RegistryPublicKey { get; }

        public static string ConvertToString(AsymmetricKeyParameter keyParameter) {
            using(var stringWriter = new StringWriter()) {
                var writer = new PemWriter(stringWriter);
                writer.WriteObject(keyParameter);
                stringWriter.Flush();

                return stringWriter.GetStringBuilder().ToString();
            }
        }

    }

}
