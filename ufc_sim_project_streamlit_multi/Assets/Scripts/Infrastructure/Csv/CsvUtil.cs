using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UFC.Infrastructure.Csv
{
    public static class CsvUtil
    {
        public static List<Dictionary<string, string>> ReadCsvDicts(string path)
        {
            var rows = new List<Dictionary<string, string>>();
            if (!File.Exists(path))
            {
                return rows;
            }

            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0)
            {
                return rows;
            }

            var headers = ParseLine(lines[0]);
            for (int i = 1; i < lines.Length; i++)
            {
                var values = ParseLine(lines[i]);
                var row = new Dictionary<string, string>();
                for (int c = 0; c < headers.Count; c++)
                {
                    string value = c < values.Count ? values[c] : string.Empty;
                    row[headers[c]] = value;
                }
                rows.Add(row);
            }
            return rows;
        }

        public static void WriteCsvDicts(string path, List<Dictionary<string, string>> rows, List<string> columns = null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
            var sb = new StringBuilder();
            List<string> header = columns ?? (rows.Count > 0 ? new List<string>(rows[0].Keys) : new List<string>());
            sb.AppendLine(JoinLine(header));

            foreach (var row in rows)
            {
                var values = new List<string>();
                foreach (var col in header)
                {
                    row.TryGetValue(col, out var value);
                    values.Add(value ?? string.Empty);
                }
                sb.AppendLine(JoinLine(values));
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        public static Dictionary<string, string> ReadKv(string path)
        {
            var rows = ReadCsvDicts(path);
            var map = new Dictionary<string, string>();
            foreach (var row in rows)
            {
                if (row.TryGetValue("key", out var key))
                {
                    row.TryGetValue("value", out var value);
                    map[key] = value ?? string.Empty;
                }
            }
            return map;
        }

        public static void WriteKv(string path, Dictionary<string, string> kv)
        {
            var rows = new List<Dictionary<string, string>>();
            foreach (var pair in kv)
            {
                rows.Add(new Dictionary<string, string>
                {
                    {"key", pair.Key},
                    {"value", pair.Value ?? string.Empty}
                });
            }
            WriteCsvDicts(path, rows, new List<string> { "key", "value" });
        }

        private static List<string> ParseLine(string line)
        {
            var values = new List<string>();
            if (line == null)
            {
                values.Add(string.Empty);
                return values;
            }

            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            values.Add(sb.ToString());
            return values;
        }

        private static string JoinLine(List<string> values)
        {
            var escaped = new List<string>();
            foreach (var value in values)
            {
                escaped.Add(Escape(value));
            }
            return string.Join(",", escaped);
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
            string escaped = value.Replace("\"", "\"\"");
            return mustQuote ? $"\"{escaped}\"" : escaped;
        }
    }
}
