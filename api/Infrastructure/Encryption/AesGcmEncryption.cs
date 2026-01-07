using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;

namespace Scv.Api.Infrastructure.Encryption
{
    public class AesGcmEncryption
    {
        private readonly byte[] _key;

        public AesGcmEncryption(IServiceProvider services)
        {
            var options = services.GetRequiredService<AesGcmEncryptionOptions>();
            _key = Encoding.UTF8.GetBytes(options.Key);
            if (_key.Length != 32)
                throw new Exception("Key length not 32 bytes (256 bits)");
        }

        public string Encrypt(string content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            using var aesObj = new AesGcmService(_key);
            return aesObj.Encrypt(content);
        }

        //It's possible CryptographicException can be thrown if the keys are changed. 
        public string Decrypt(string content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            using var aesObj = new AesGcmService(_key);
            return aesObj.Decrypt(content);
        }
    }
}
