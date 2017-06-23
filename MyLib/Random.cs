using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyLib
{
    public class Rand
    {
        public enum CharType
        {
            digit, lowercase, uppercase
        }

        public static string RandomDigit(int length)
        {
            return RandomString(length, CharType.digit);
        }

        public static string RandomLowercase(int length)
        {
            return RandomString(length, CharType.lowercase);
        }

        public static string RandomDigitLowercase(int length)
        {
            return RandomString(length, CharType.digit, CharType.lowercase);
        }

        public static string RandomDigitLowercaseUppercase(int length)
        {
            return RandomString(length, CharType.digit, CharType.lowercase, CharType.uppercase);
        }

        private static string RandomString(int length, params CharType[] types)
        {
            int digit = '0';
            int lowercase = 'a';
            int uppercase = 'A';

            char[] charset = null;

            if (types.Length == 0)
                types = new CharType[] { CharType.digit };

            int n = 0;
            for (int i = 0; i < types.Length; i++)
                if (types[i] == CharType.digit)
                    n += 10;
                else if (types[i] == CharType.lowercase)
                    n += 26;
                else if (types[i] == CharType.uppercase)
                    n += 26;
            charset = new char[n];
            int index = 0;
            for (int i = 0; i < types.Length; i++)
                if (types[i] == CharType.digit)
                    for (int x = 0; x < 10; x++)
                        charset[index++] = (char)(digit + x);
                else if (types[i] == CharType.lowercase)
                    for (int x = 0; x < 26; x++)
                        charset[index++] = (char)(lowercase + x);
                else if (types[i] == CharType.uppercase)
                    for (int x = 0; x < 26; x++)
                        charset[index++] = (char)(uppercase + x);

            return RandomChar(length, charset);
        }

        public static string RandomChar(int length, char[] charset)
        {
            string s = "";
            for (int i = 0; i < length; i++)
            {
                int x = Math.Abs(Guid.NewGuid().GetHashCode()) % charset.Length;
                s += charset[x];
            }
            return s;
        }

        public static int RandomInt(int min, int max)
        {
            if (min == max)
                return min;
            else if (min < max)
                return min + Math.Abs(Guid.NewGuid().GetHashCode()) % (max - min + 1);
            else
                return max + Math.Abs(Guid.NewGuid().GetHashCode()) % (min - max + 1);
        }
    }
}
