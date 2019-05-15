using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GAScheduling
{
    public class CsvReader
    {
        public static List<T> ReadAll<T>(string path, Func<List<string>, T> map)
        {
            var list = new List<T>();

            using (var reader = new StreamReader(path))
            {
                if (reader.EndOfStream)
                    throw new Exception("Invalid Csv File: " + path);
                // ignore the header
                var _ = ParseLine(reader.ReadLine());

                while (!reader.EndOfStream)
                {
                    var item = ParseLine(reader.ReadLine());
                    list.Add(map(item));
                }
            }

            return list;
        }

        private static List<string> ParseLine(string line)
        {
            var list = new List<string>();
            int begin = 0;

            for (int i = 0; i< line.Length; ++i)
            {
                if (line[i] == ',')
                {
                    list.Add(line.Substring(begin, i - begin));
                    begin = i + 1;
                    continue;
                }
                if (line[i] == '"')
                {
                    begin += 1;
                    while (++i < line.Length)
                    {
                        if (line[i] == '"')
                        {
                            if (i + 1 == line.Length || line[i+1] != '"')
                            {
                                list.Add(line.Substring(begin, i - begin).Replace("\"\"", "\""));
                                i = i + 1;
                                begin = i + 1;
                                break;
                            }
                            else
                            {
                                i = i + 1;
                                continue;
                            }
                        }
                    }
                }
            }

            if (begin < line.Length)
            {
                list.Add(line.Substring(begin, line.Length - begin));
            }

            return list;
        }
    }
}
