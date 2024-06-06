using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection.Emit;

namespace Utils
{


    public static class ValueTypesUtils
    {

        private static Random random = new Random();

        public static string ToTitleCase(this string str)
        {
            TextInfo textInfo = new CultureInfo("es-AR", false).TextInfo;
            return textInfo.ToTitleCase(str);
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static char GetNextChar(this char c)
        {
            // convert char to ascii
            int ascii = (int)c;
            // get the next ascii
            int nextAscii = ascii + 1;
            // convert ascii to char
            char nextChar = (char)nextAscii;

            return nextChar;
        }

        public static string RemoveLastChar(this string s, char c)
        {
            int index = s.LastIndexOf(c); //remover ultima coma
            if (index > -1)
                return s.Remove(index, 1);
            return s;
        }

        public static string RemoveDigits(this string key)
        {
            return Regex.Replace(key, @"\d", "");
        }
        public static string ReplaceFirst(this string @this, string oldValue, string newValue)
        {
            int startindex = @this.IndexOf(oldValue);

            if (startindex == -1)
            {
                return @this;
            }

            return @this.Remove(startindex, oldValue.Length).Insert(startindex, newValue);
        }

        public static bool ToBool(this string @this)
        {
            string s = @this.Substring(0, 1).ToLower();
            if (s == "t" || s == "1" || s == "s" || s == "y" || s == "o") return true;
            return false;
        }

        public static bool ToBool(this int @this)
        {
            return @this.ToString().ToBool();
        }

        public static char ToChar(this string @this)
        {
            return @this.ToCharArray()[0];
        }

        public static string RemoveMultipleSpaces(this string @this)
        {
            return Regex.Replace(@this, @"\s+", " ");
        }

        /// <summary>
        /// https://www.dotnetperls.com/between-before-after
        /// </summary>
        /// <param name="value"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string SubstringBetween(this string value, string a, string b)
        {
            int posA = value.IndexOf(a);
            
            if (posA == -1)
            {
                return "";
            }

            string _value = value.Substring(posA + a.Length);
            int posB = _value.IndexOf(b);
            if (posB == -1)
            {
                return "";
            }
            
            return _value.Substring(0, posB);
        }

        /// <summary>
        /// Acronym from string
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        /// <remarks>https://stackoverflow.com/questions/4000304/get-an-acronym-from-a-string-in-c-sharp-using-linq</remarks>
        public static string Acronym(this string @this)
        {
            return string.Join(string.Empty,
                @this.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s[0])
            );
        }

        /// <summary>
        /// Open url in default browser
        /// </summary>
        /// <param name="url"></param>
        /// <remarks>
        /// - https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/<br/>
        /// - https://stackoverflow.com/questions/7888871/hyperlink-keeps-failing
        /// </remarks>
        /// <example>
        /// private void DescargarAdjunto_Click(object sender, RoutedEventArgs e)
        /// {
        ///     e.Handled = true;
        ///     var url = ((Hyperlink)e.OriginalSource).NavigateUri.OriginalString;
        ///     ValueTypesUtils.OpenBrowser(url);
        /// }
        /// </example>
        public static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Quita los caracteres no numericos de un string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <remarks>https://stackoverflow.com/questions/19167669/keep-only-numeric-value-from-a-string</remarks>
        public static string? CleanStringOfDigits(this string? s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            StringBuilder sb = new StringBuilder(s);
            int j = 0, i = 0;
            while (i < sb.Length)
            {
                if (!char.IsDigit(sb[i]))
                    sb[j++] = sb[i++];
                else
                    ++i;
            }
            sb.Length = j;
            return sb.ToString();
        }

        /// <summary>
        /// Quita los caracteres no numericos de un string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <remarks>https://stackoverflow.com/questions/19167669/keep-only-numeric-value-from-a-string</remarks>
        public static string? CleanStringOfNonDigits(this string? s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            StringBuilder sb = new StringBuilder(s);
            int j = 0, i = 0;
            while (i < sb.Length)
            {
                if (char.IsDigit(sb[i]))
                    sb[j++] = sb[i++];
                else
                    ++i;
            }
            sb.Length = j;
            return sb.ToString();
        }

        /// <summary>
        /// Quita los caracteres no numericos de un string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <remarks>https://stackoverflow.com/questions/19167669/keep-only-numeric-value-from-a-string</remarks>
        public static (string, string)? DivideStringNonDigitsDigits(this string? s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            StringBuilder sb = new StringBuilder(s);
            string nondig = "";
            string dig = "";
            for(int i = 0; i < sb.Length; i++)
            {
                if (char.IsDigit(sb[i]))
                    dig += sb[i];
                else
                    nondig += sb[i];
            }

            return (nondig, dig);
        }


        /// <summary>
        /// Recibe un string y verifica si tiene caracteres que no sean numeros
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool HasNonDigits(this string? s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            StringBuilder sb = new StringBuilder(s);
            for (var i = 0; i < sb.Length; i++){
                if (!char.IsDigit(sb[i]))
                    return false;
            }
            return true;
        }

        public static bool similarTo(this string name1, string name2, int len = 4)
        {
            string[] n1 = name1.Trim().RemoveMultipleSpaces().ToUpper().Split(" ");
            string[] n2 = name2.Trim().RemoveMultipleSpaces().ToUpper().Split(" ");

            foreach(string nn1 in n1)
            {
                foreach(string nn2 in n2)
                {
                    if(len > nn1.Length) 
                        len = nn1.Length;

                    string n = nn1.Substring(0, len);
                    if(nn2.Contains(n)) 
                        return true;
                }
            }

            return false;
        }

        
        /// <summary>Encode data to base64</summary>
        public static string EncodeToBase64(string data)
        {
            byte[] encData_byte = new byte[data.Length];
            encData_byte = System.Text.Encoding.UTF8.GetBytes(data);
            string encodedData = Convert.ToBase64String(encData_byte);
            return encodedData;
        }

        /// <summary>Decode data from base64</summary>
        public static string DecodeFrom64(string encodedData)
        {
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            System.Text.Decoder utf8Decode = encoder.GetDecoder();
            byte[] todecode_byte = Convert.FromBase64String(encodedData);
            int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
            char[] decoded_char = new char[charCount];
            utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
            string result = new String(decoded_char);
            return result;
        }

        public static string ToOrdinalSpanish(this string numberString)
        {
            int number;
            if (!int.TryParse(numberString, out number))
            {
                // Handle invalid input
                return "Inválido";
            }

            if (number <= 0)
            {
                // Handle non-positive numbers
                return numberString;
            }

            switch (number)
            {
                case 1:
                    return "primero";
                case 2:
                    return "segundo";
                case 3:
                    return "tercero";
                case 4:
                    return "cuarto";
                case 5:
                    return "quinto";
                case 6:
                    return "sexto";
                case 7:
                    return "séptimo";
                case 8:
                    return "octavo";
                case 9:
                    return "noveno";
                case 10:
                    return "decimo";
                default:
                    return numberString; // For numbers above 10, just return the input string
            }
        }


        public static string ConvertTextToHtml(this string text)
        {
            // Reemplaza caracteres especiales de HTML
            string html = System.Net.WebUtility.HtmlEncode(text);

            // Reemplaza saltos de línea con etiquetas <br>
            html = html.Replace("\r\n", "<br>");

            return html;
        }

    }
}
