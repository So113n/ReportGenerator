using System.Security.Cryptography;
using System.Text;

namespace ReportGenerator.Services.Crypt
{
    public sealed class SimpleEncryptionService
    {
        private static SimpleEncryptionService INSTANCE;

        private readonly byte[] _salt = Encoding.UTF8.GetBytes("SimpleSalt123"); // Простая соль для учебных целей
        private static object _padlock = new object();

        public static SimpleEncryptionService Instance
        {
            get
            {
                lock (_padlock)
                {
                    if (INSTANCE == null)
                        INSTANCE = new();
                }

                return INSTANCE; 
            }
        }

        /// <summary>
        /// Запись зашифрованного текста в файл
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="text"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task WriteEncryptedFileAsync(string filePath, string text, string password)
        {
            using (var aes = Aes.Create())
            {
                // Генерируем ключ из пароля
                var key = new Rfc2898DeriveBytes(password, _salt, 1000);
                aes.Key = key.GetBytes(32); // 256-bit ключ
                aes.IV = key.GetBytes(16);  // 128-bit IV

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                using (var cryptoStream = new CryptoStream(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cryptoStream))
                {
                    await writer.WriteAsync(text);
                }
            }
        }

        /// <summary>
        /// Чтение зашифрованного файла
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<string> ReadEncryptedFileAsync(string filePath, string password)
        {
            using (var aes = Aes.Create())
            {
                // Генерируем ключ из пароля
                var key = new Rfc2898DeriveBytes(password, _salt, 1000);
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16);

                using (var fileStream = new FileStream(filePath, FileMode.Open))
                using (var cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var reader = new StreamReader(cryptoStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        /// <summary>
        /// Проверка существования файла
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool IsFileExists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}
