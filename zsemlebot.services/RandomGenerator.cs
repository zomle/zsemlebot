using System.Security.Cryptography;
using System.Text;

namespace zsemlebot.services
{
    internal static class RandomGenerator
    {
        //some characters are missing to avoid confusion with weird font types (e.g.: 1,I,l and O,0).
        private const string ValidCodeCharacters = "23456789qwertyuiopasdfghjkzxcvbnmQWERTYUPASDFGHJKLZXCVBNM";

        public static string GenerateCode(int length = 5)
        {
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                var ix = RandomNumberGenerator.GetInt32(ValidCodeCharacters.Length);
                sb.Append(ValidCodeCharacters[ix]);
            }
            return sb.ToString();
        }
    }
}
