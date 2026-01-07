using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using System.Xml.Linq;

namespace Scv.Api.Infrastructure.Encryption
{
    public class AesGcmXmlDecryptor : IXmlDecryptor
    {
        private readonly byte[] _key;

        public AesGcmXmlDecryptor(IServiceProvider services)
        {
            var options = services.GetRequiredService<AesGcmEncryptionOptions>();
            _key = Encoding.UTF8.GetBytes(options.Key);
            if (_key.Length != 32)
                throw new Exception("Key length not 32 bytes (256 bits)");
        }

        //It's possible CryptographicException can be thrown if the keys are changed. 
        public XElement Decrypt(XElement encryptedElement)
        {
            if (encryptedElement == null)
                throw new ArgumentNullException(nameof(encryptedElement));

            using var aesObj = new AesGcmService(_key);
            return XElement.Parse(aesObj.Decrypt(encryptedElement.Element("value")?.Value));
        }
    }
}
