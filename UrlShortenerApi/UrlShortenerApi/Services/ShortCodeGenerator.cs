namespace UrlShortenerApi.Services
{
    public static class ShortCodeGenerator
    {
        private static readonly Random _random = new();
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

        public static string Generate(int length = 6)
        {
            char[] code = new char[length];
            lock (_random)  
            {
                for (int i = 0; i < length; i++)
                {
                    code[i] = Alphabet[_random.Next(Alphabet.Length)];
                }
            }
            return new string(code);
        }
    }

}
