namespace WordleStarter {
    using System.Diagnostics;

    class CharSet
    {
        private int Bitmap = 0;
        public readonly List<char> Chars = new(5);

        public void Add(char ch)
        {
            int i = 1 << (ch - 'a');
            if ((Bitmap & i) == 0)
            {
                Chars.Add(ch);
                Bitmap |= i;
            }
        }

        public void Clear()
        {
            Bitmap = 0;
            Chars.Clear();
        }

        public bool MatchWord(string word)
        {
            foreach (char ch in Chars)
                if (word.Contains(ch))
                    return true;
            return false;
        }
    }

    class CharDictionary
    {
        readonly byte[] Bytemap;
        readonly CharSet Chars;

        public CharDictionary()
        {
            Bytemap = new byte[128];
            Chars = new();
        }

        public CharDictionary(string str)
        {
            Bytemap = new byte[128];
            Chars = new();
            foreach (char ch in str)
                Add(ch);
        }

        public void Add(char ch)
        {
            if (Bytemap[ch] == 0)
                Chars.Add(ch);
            Bytemap[ch]++;
        }

        public void Clear()
        {
            for (byte i = 96; i < 128; i++)
                Bytemap[i] = 0;
            Chars.Clear();
        }

        public bool Contains(char ch) => Bytemap[ch] > 0;

        public bool MatchWord(CharDictionary letters, CharDictionary correct)
        {
            foreach (char ch in Chars.Chars)
                if (!letters.Contains(ch) || (letters.Bytemap[ch] < Bytemap[ch] && Bytemap[ch] <= correct.Bytemap[ch]) || (Bytemap[ch] > correct.Bytemap[ch] && letters.Bytemap[ch] > correct.Bytemap[ch]))
                    return true;
            return false;
        }
    }

    class Yellow
    {
        List<(char c, int i)> ChIList = new(5);

        public void Add(char c, int i) => ChIList.Add((c, i));

        public void Clear() => ChIList.Clear();

        public bool MatchWord(string word)
        {
            for (int j = 0; j < ChIList.Count; j++)
            {
                (char c, int i) y = ChIList[j];
                if (word[y.i] == y.c)
                    return true;
            }
            return false;
        }
    }

    class Green
    {
        List<(char c, int i)> ChIList = new(5);

        public void Add(char c, int i) => ChIList.Add((c, i));

        public void Clear() => ChIList.Clear();

        public bool MatchWord(string word)
        {
            for (int j = 0; j < ChIList.Count; j++)
            {
                (char c, int i) y = ChIList[j];
                if (word[y.i] != y.c)
                    return true;
            }
            return false;
        }
    }

    class SolverContext
    {
        private readonly CharDictionary KnownLetters = new();
        private readonly CharSet Grey = new();
        private readonly Yellow Yellow = new();
        private readonly Green Green = new();

        private readonly string[][] Possible;
        private readonly Dictionary<string, CharDictionary> PossibleLetters;

        public SolverContext(string[] possibleWords, Dictionary<string, CharDictionary> possibleLetters)
        {
            PossibleLetters = possibleLetters;
            Possible = new string[6][];
            Possible[0] = possibleWords;
            for (int i = 1; i < Possible.Length; i++)
                Possible[i] = new string[possibleWords.Length];
        }

        public (double Success, double AverageDepth) RateStarter(string newWord, int possibleCount, string correctWord, CharDictionary correctLetters, int depth)
        {
            if (depth == 6)
                return (0, depth);

            KnownLetters.Clear();
            Grey.Clear();
            Yellow.Clear();
            Green.Clear();
            for (int i = 0; i < newWord.Length; i++)
            {
                char c = newWord[i];
                if (correctLetters.Contains(c))
                {
                    KnownLetters.Add(c);
                    if (correctWord[i] == c)
                        Green.Add(c, i);
                    else
                        Yellow.Add(c, i);
                }
                else
                {
                    Grey.Add(c);
                }
            }

            int count = 0;
            int prev = depth - 1;
            for (int i = 0; i < possibleCount; i++)
            {
                string word = Possible[prev][i];
                if (word == correctWord || KnownLetters.MatchWord(PossibleLetters[word], correctLetters) || Grey.MatchWord(word) || Yellow.MatchWord(word) || Green.MatchWord(word))
                    continue;
                Possible[depth][count++] = word;
            }

            double success = 1;
            double avr = depth;
            for (int i = 0; i < count; i++)
            {
                (double a, double b) = RateStarter(Possible[depth][i], count, correctWord, correctLetters, depth + 1);
                success += a;
                avr += b;
            }

            count++;
            return (success / count, avr / count);
        }
    }

    class WorkerState
    {
        public readonly SolverContext Context;
        public double SuccessSum;
        public double DepthSum;

        public WorkerState(SolverContext context)
        {
            Context = context;
        }
    }

    internal class Program
    {
        private static readonly object ConsoleLock = new();

        static void Main(string[] args)
        {
            List<string> all = GetAll();

            string[] possibleWords = File.ReadAllLines("possible.txt");
            int possibleCount = possibleWords.Length;

            var possibleLetters = new Dictionary<string, CharDictionary>(possibleCount);
            for (int i = 0; i < possibleCount; i++)
                possibleLetters[possibleWords[i]] = new CharDictionary(possibleWords[i]);

            Stopwatch totalTime = Stopwatch.StartNew();
            for (int j = 0; j < all.Count; j++)
            {
                string starter = all[j];

                double successTotal = 0;
                double depthTotal = 0;

                int completed = 0;
                int totalPairs = possibleCount;

                Stopwatch starterWatch = Stopwatch.StartNew();

                using CancellationTokenSource cts = new();

                Task statusTask = Task.Run(() =>
                {
                    int width = Console.IsOutputRedirected ? 180 : Math.Max(120, Console.WindowWidth - 1);

                    while (!cts.Token.IsCancellationRequested)
                    {
                        int done = Volatile.Read(ref completed);

                        string testingWord = possibleWords[Math.Min(done, totalPairs - 1)];
                        string starterEta = done > 0 ? TimeSpan.FromSeconds(starterWatch.Elapsed.TotalSeconds / done * (totalPairs - done)).ToString(@"hh\:mm\:ss") : "--:--:--";

                        int processedPairs = j * totalPairs + done;
                        int allPairs = all.Count * totalPairs;
                        TimeSpan totalEta = processedPairs > 0 ? TimeSpan.FromSeconds(totalTime.Elapsed.TotalSeconds / processedPairs * (allPairs - processedPairs)) : TimeSpan.Zero;

                        string line = $"Trying starter \"{starter}\" Progress: {done * 100 / totalPairs}% (ETA Starter: {starterEta} Total: {totalEta.Days}d {totalEta:hh\\:mm\\:ss})";

                        if (line.Length > width)
                            line = line[..width];

                        lock (ConsoleLock)
                        {
                            Console.SetCursorPosition(0, 0);
                            Console.Write("\r" + line.PadRight(width));
                        }

                        if (cts.Token.WaitHandle.WaitOne(250))
                            break;
                    }
                });

                object sumLock = new();
                Parallel.For(0, possibleCount, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, () => new WorkerState(new SolverContext(possibleWords, possibleLetters)),
                    (i, _, worker) =>
                    {
                        string correct = possibleWords[i];
                        (double a, double b) = worker.Context.RateStarter(starter, possibleCount, correct, possibleLetters[correct], 1);

                        worker.SuccessSum += a;
                        worker.DepthSum += b;

                        Interlocked.Increment(ref completed);
                        return worker;
                    },
                    worker =>
                    {
                        lock (sumLock)
                        {
                            successTotal += worker.SuccessSum;
                            depthTotal += worker.DepthSum;
                        }
                    });

                cts.Cancel();
                try { statusTask.Wait(); } catch { }

                lock (ConsoleLock)
                {
                    Console.WriteLine();
                }

                double success = successTotal / possibleCount;
                double avr = depthTotal / possibleCount;

                string write = $"Rating for {starter}: Succes rate {success} Average depth {avr} completed in {starterWatch.Elapsed.TotalSeconds} seconds";
                if (write.Length < 114)
                    write += new string(' ', 114 - write.Length);

                Console.Write(write);
                File.AppendAllText("ratings.csv", $"{starter};{success};{avr};{starterWatch.Elapsed.TotalSeconds}\n");
            }
        }

        private static List<string> GetAll()
        {
            if (!File.Exists("ratings.csv"))
                File.WriteAllText("ratings.csv", "word;success_rate;average_depth;time(s)\n");
            HashSet<string> filter = [.. File.ReadLines("ratings.csv").Skip(1).Select(line => line.Split(';')[0])];
            string[] allFile = File.ReadAllLines("all.txt");
            List<string> all = [];
            foreach (string word in allFile)
            {
                if (filter.Contains(word))
                    continue;
                int mask = 0;
                bool good = true;
                foreach (char c in word)
                {
                    int bit = 1 << (c - 'a');
                    if ((mask & bit) != 0)
                    {
                        good = false;
                        break;
                    }
                    mask |= bit;
                }
                if (good)
                    all.Add(word);
            }
            return all;
        }
    }
}