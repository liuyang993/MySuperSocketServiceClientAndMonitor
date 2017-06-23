using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections;
using System.Text.RegularExpressions;

namespace MyLib
{
    static public class StringUtility
    {
        public static bool IsSameText(this string a, string b)
        {
            return a.Equals(b, StringComparison.CurrentCultureIgnoreCase);
        }

        public static void SplitString(this string s, string seperator, ref string left, ref string right)
        {
            int n = s.IndexOf(seperator);
            if (n >= 0)
            {
                left = s.Substring(0, n);
                right = s.Substring(n + seperator.Length, s.Length - n - seperator.Length);
            }
            else
            {
                left = s;
                right = "";
            }
        }

        public static void SplitString3(this string s, string seperator, ref string s1, ref string s2, ref string s3)
        {
            SplitString(s, seperator, ref s1, ref s);
            SplitString(s, seperator, ref s2, ref s3);
        }

        public static void SplitString4(this string s, string seperator, ref string s1, ref string s2, ref string s3, ref string s4)
        {
            SplitString(s, seperator, ref s1, ref s);
            SplitString(s, seperator, ref s2, ref s);
            SplitString(s, seperator, ref s3, ref s4);
        }

        public static void SplitString5(this string s, string seperator, ref string s1, ref string s2, ref string s3, ref string s4, ref string s5)
        {
            SplitString(s, seperator, ref s1, ref s);
            SplitString(s, seperator, ref s2, ref s);
            SplitString(s, seperator, ref s3, ref s);
            SplitString(s, seperator, ref s4, ref s5);
        }

        public static void SplitString6(this string s, string seperator, ref string s1, ref string s2, ref string s3, ref string s4, ref string s5, ref string s6)
        {
            SplitString(s, seperator, ref s1, ref s);
            SplitString(s, seperator, ref s2, ref s);
            SplitString(s, seperator, ref s3, ref s);
            SplitString(s, seperator, ref s4, ref s);
            SplitString(s, seperator, ref s5, ref s6);
        }

        public static string[] MySplit(this string s, char seperator)
        {
            if (s == "")
                return new string[0];
            else
                return s.Split(new char[] { seperator });
        }

        public static string[] MySplit(this string s, char seperator, char quote)
        {
            if (s == "")
                return new string[0];
            ArrayList ss = new ArrayList();
            int index = 0;
            bool inQuote = false;
            string temp = "";
            while (index < s.Length)
            {
                if (inQuote)
                {
                    temp += s[index];
                    if (s[index] == quote)
                    {
                        if (index == s.Length - 1)
                            break;
                        else if (s[index + 1] == seperator)
                        {
                            inQuote = false;
                            ss.Add(temp);
                            temp = "";
                            index++;
                        }
                    }
                }
                else
                {
                    if (s[index] == seperator)
                    {
                        if ((seperator != ' ') || ((seperator == ' ') && (temp != "")))
                        {
                            ss.Add(temp);
                            temp = "";
                        }

                        if (seperator == ' ')
                        {
                            //skip space
                            while (index + 1 < s.Length)
                            {
                                if (s[index + 1] == seperator)
                                    index++;
                                else
                                    break;
                            }
                        }
                    }
                    else
                        temp += s[index];

                    if (s[index] == quote)
                        inQuote = true;
                }
                index++;
            }
            if ((seperator != ' ') || ((seperator == ' ') && (temp != "")))
                ss.Add(temp);
            if (ss.Count == 0)
                return new string[0];

            string[] sss = new string[ss.Count];
            for (int i = 0; i < ss.Count; i++)
                sss[i] = (string)ss[i];
            return sss;
        }

        public static int ToInt(this string s)
        {
            return int.Parse(s);
        }

        public static int ToIntDef(this string s, int defaultValue)
        {
            int n = 0;
            if (int.TryParse(s, out n))
                return n;
            else
                return defaultValue;
        }

        public static long ToLongDef(this string s, long defaultValue)
        {
            long n = 0;
            if (long.TryParse(s, out n))
                return n;
            else
                return defaultValue;
        }

        public static bool IsInt(this string s)
        {
            int n = 0;
            return int.TryParse(s, out n);
        }

        public static bool IsDouble(this string s)
        {
            double n = 0;
            return double.TryParse(s, out n);
        }

        public static bool IsDateTime(this string s)
        {
            DateTime n = DateTime.MinValue;
            return DateTime.TryParse(s, out n);
        }

        public static string RemoveQuote(this string s)
        {
            if ((s.Length >= 2) && ((LeftCompare(s, "'") && RightCompare(s, "'")) || (LeftCompare(s, "\"") && RightCompare(s, "\""))))
                return s.Substring(1, s.Length - 2);
            else
                return s;
        }

        public static string RemoveQuote(this string s, string quote)
        {
            if (LeftCompare(s, quote) && RightCompare(s, quote))
                return s.Substring(1, s.Length - 2);
            else
                return s;
        }

        public static string LeftStr(this string s, int length)
        {
            return LeftStr(s, 0, length);
        }

        public static string LeftStr(this string s, int start, int length)
        {
            if (s.Length > start + length)
                return s.Substring(start, length);
            else
                return s;
        }

        public static string RightStr(this string s, int length)
        {
            if (s.Length > length)
                return s.Substring(s.Length - length, length);
            else
                return s;
        }

        public static bool LeftCompare(this string s, string left)
        {
            return LeftStr(s, left.Length) == left;
        }

        public static bool LeftCompare(this string s, int start, string left)
        {
            return LeftStr(s, start, left.Length) == left;
        }

        public static bool RightCompare(this string s, string right)
        {
            return RightStr(s, right.Length) == right;
        }

        public static string LeftRemove(this string s, string left)
        {
            if (LeftCompare(s, left))
                return s.Remove(0, left.Length);
            else
                return s;
        }

        public static string RightRemove(this string s, string right)
        {
            if (RightCompare(s, right))
                return s.Remove(s.Length - right.Length, right.Length);
            else
                return s;
        }

        public static string Substring2(this string s, int start, int length)
        {
            if (start + length <= s.Length)
                return s.Substring(start, length);
            else
                return s.Substring(start, s.Length - start);
        }

        public static string MarkCtrl(this string s)
        {
            //marks any characters that are less than chr(27) to be seen.
            for (int i = 1; i <= 26; i++)
                s = s.Replace(i.Chr(), "{" + i.ToString() + "}");
            return s;
        }

        public static string TrimCtrl(this string s)
        {
            //trims all ctrl canracters from origstr except for ctrl-Z or higher so that +CMGS and others can react accordingly
            for (int i = 1; i <= 25; i++)
                s = s.Replace(i.Chr(), "");
            return s;
        }

        public static string AddString(this string main, string sub)
        {
            return AddString(main, sub, "\r\n");
        }

        public static string AddString(this string main, string sub, string delimiter)
        {
            if (main == "")
                return sub;
            else
                return main + delimiter + sub;
        }

        public static int Asc(this string character)
        {
            if (character.Length == 1)
            {
                System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
                return (int)asciiEncoding.GetBytes(character)[0];
            }
            else
            {
                throw new Exception("Character is not valid.");
            }
        }

        public static string Chr(this int asciiCode)
        {
            if (asciiCode >= 0 && asciiCode <= 255)
            {
                System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
                byte[] byteArray = new byte[] { (byte)asciiCode };
                string strCharacter = asciiEncoding.GetString(byteArray);
                return (strCharacter);
            }
            else
            {
                throw new Exception("ASCII Code is not valid.");
            }
        }


        public static string TestRegEx(this string s, string pattern)
        {
            string result = String.Format("String: {0}\r\nPattern: {1}\r\n", s, pattern);
            Match match = Regex.Match(s, pattern);
            while (match.Success)
            {
                result += String.Format("match.Value={0}\r\n", match.Value);
                for (int i = 0; i < match.Groups.Count; i++)
                {
                    result += String.Format("match.Groups[{0}]={1}\r\n", i, match.Groups[i].Value);
                    for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                        result += String.Format("match.Groups[{0}].Captures[{1}]={2}\r\n", i, j, match.Groups[i].Captures[j].Value);
                }

                for (int i = 0; i < match.Captures.Count; i++)
                    result += String.Format("match.Captures[{0}]={1}\r\n", i, match.Captures[i].Value);

                match = match.NextMatch();
            }

            return result;
        }

        public static bool MatchString(this string s, string pattern, ref string section1)
        {
            section1 = "";
            Match match = Regex.Match(s, pattern);
            if (match.Groups.Count == 2)
            {
                section1 = match.Groups[1].Value;
                return true;
            }
            else
                return false;
        }

        public static bool MatchString(this string s, string pattern, ref string section1, ref string section2)
        {
            section1 = "";
            section2 = "";
            Match match = Regex.Match(s, pattern);
            if (match.Groups.Count == 3)
            {
                section1 = match.Groups[1].Value;
                section2 = match.Groups[2].Value;
                return true;
            }
            else
                return false;
        }

        public static bool MatchString(this string s, string pattern, ref string section1, ref string section2, ref string section3)
        {
            section1 = "";
            section2 = "";
            section3 = "";
            Match match = Regex.Match(s, pattern);
            if (match.Groups.Count == 4)
            {
                section1 = match.Groups[1].Value;
                section2 = match.Groups[2].Value;
                section3 = match.Groups[3].Value;
                return true;
            }
            else
                return false;
        }

        public static bool MatchString(this string s, string pattern, ref string section1, ref string section2, ref string section3, ref string section4)
        {
            section1 = "";
            section2 = "";
            section3 = "";
            section4 = "";
            Match match = Regex.Match(s, pattern);
            if (match.Groups.Count == 5)
            {
                section1 = match.Groups[1].Value;
                section2 = match.Groups[2].Value;
                section3 = match.Groups[3].Value;
                section4 = match.Groups[4].Value;
                return true;
            }
            else
                return false;
        }

        public static string ReadLine2(this System.IO.StreamReader stream)
        {
            StringBuilder buff;
            try
            {
                buff = new StringBuilder();
                int ch;
                while ((ch = stream.Read()) != -1)
                {
                    if (ch == 13)
                        continue;
                    if (ch == 10)
                        return buff.ToString();
                    buff.Append(Convert.ToChar(ch));
                }
                if (buff.Length > 0)
                    return buff.ToString();
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
