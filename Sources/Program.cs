namespace SrtCombiner
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            string inputA = args[0];
            string inputB = args[1];
            string output = args[2];
            string pathToResult = await MergeAsync(inputA, inputB, output);
            Console.WriteLine("Done! Your file is here: {0}", pathToResult);
        }

        private static async Task<string> MergeAsync(string inputA, string inputB, string output)
        {
            IEnumerable<SrtLine> linesA = ParseFile(inputA);
            IEnumerable<SrtLine> linesB = ParseFile(inputB);
            IEnumerable<SrtLine> merged = Merge(linesA, linesB);
            FileInfo fileInfo = new(output);
            using FileStream result = fileInfo.OpenWrite();
            using StreamWriter writer = new(result);
            foreach (SrtLine line in merged)
            {
                await writer.WriteLineAsync(line.ToString());
            }
            return fileInfo.FullName;
        }

        private static IEnumerable<SrtLine> Merge(IEnumerable<SrtLine> linesA, IEnumerable<SrtLine> linesB)
        {
            List<SrtLine> result = new();
            HashSet<SrtLine> b = new(linesB);
            foreach (var item in linesA)
            {
                var found = b.FirstOrDefault(x => x.From == item.From);
                if (found != null)
                {
                    item.AppendText(found.Text);
                    b.Remove(found);
                }
                result.Add(item);
            }
            result.AddRange(b);
            int counter = 1;
            var ordered = result.OrderBy(x => x.From);
            foreach (var item in ordered)
            {
                item.Position = counter;
                counter++;
            }
            return ordered;
        }

        private static IEnumerable<SrtLine> ParseFile(string inputFilePath)
        {
            string content = File.ReadAllText(inputFilePath);
            string[] parts = content.Replace("\r", string.Empty).Split("\n\n");
            var result = parts
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new SrtLine(x))
                .OrderBy(x => x.Position);
            return result;
        }

        public class SrtLine
        {
            public int Position { get; set; }
            public TimeSpan From { get; set; }
            public TimeSpan To { get; set; }
            public string Text { get; set;}
            public string Time => TimeToString();

            private const string timeSplitter = " --> ";

            public SrtLine(string lineData)
            {
                string data = lineData.Trim().Replace("\r", string.Empty);
                string[] lines = data.Split('\n');
                Position = int.Parse(lines[0]);
                string[] times = lines[1].Split(timeSplitter);
                From = TimeSpan.Parse(times[0]);
                To = TimeSpan.Parse(times[1]);
                var text = lines.Skip(2);
                Text = string.Join(Environment.NewLine, text);
            }

            private string TimeToString()
            {
                return $"{From.Hours}:{From.Minutes}:{From.Seconds},{From.Milliseconds.ToString().PadLeft(3, '0')}" +
                    $"{timeSplitter}{To.Hours}:{To.Minutes}:{To.Seconds},{To.Milliseconds.ToString().PadLeft(3, '0')}";
            }

            public override string ToString()
            {
                System.Text.StringBuilder sb = new();
                sb.AppendLine(Position.ToString());
                sb.AppendLine(Time);
                sb.AppendLine(Text);
                return sb.ToString();
            }

            public void AppendText(string text)
            {
                Text += Environment.NewLine;
                Text += text;
                Text = Text.Trim();
            }
        }
    }
}