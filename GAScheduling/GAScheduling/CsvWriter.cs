using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GAScheduling
{
    class CsvWriter
    {
        public static void WriteAll<T>(string path, List<string> header, List<T> items, Func<T, List<string>> map)
        {
            var utf8bom = new UTF8Encoding(true);
            using (var writer = new StreamWriter(path, false, utf8bom))
            {
                writer.WriteLine(FormatLine(header));

                foreach (var item in items)
                    writer.WriteLine(FormatLine(map(item)));

                writer.Flush();
                writer.Close();
            }
        }

        private static string FormatLine(List<string> list)
        {
            var builder = new StringBuilder();
            foreach (var str in list)
            {
                if (str.Contains(',') || str.Contains('\"') || str.Contains(' '))
                {
                    builder.Append('"');
                    builder.Append(str.Replace("\"", "\"\""));
                    builder.Append('"');
                }
                else
                    builder.Append(str);
                builder.Append(',');
            }

            return builder.ToString();
        }
    }
}
