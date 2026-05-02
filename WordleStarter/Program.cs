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

    internal class Program
    {
        static readonly CharDictionary knownLetters = new();
        static readonly CharSet grey = new();
        static readonly Yellow yellow = new();
        static readonly Green green = new();
        static Dictionary<string, CharDictionary>? possibleLetters;
        static readonly string[][] possible = new string[6][];

        static void Main(string[] args)
        {
            List<string> all = GetAll();
            possible[0] = File.ReadAllLines("possible.txt");
            possible[1] = new string[possible[0].Length];
            possible[2] = new string[possible[0].Length];
            possible[3] = new string[possible[0].Length];
            possible[4] = new string[possible[0].Length];
            possible[5] = new string[possible[0].Length];
            possibleLetters = new Dictionary<string, CharDictionary>(possible.Length);
            for (int i = 0; i < possible[0].Length; i++)
                possibleLetters[possible[0][i]] = new(possible[0][i]);
            Stopwatch totalTime = Stopwatch.StartNew();
            for (int j = 0; j < all.Count; j++)
            {
                string word = all[j];
                double succes = 0;
                double avr = 0;
                Stopwatch wordTime = Stopwatch.StartNew();
                for (int i = 0; i < possible[0].Length; i++)
                {
                    string correct = possible[0][i];
                    Console.SetCursorPosition(0, 0);
                    double elapsedSecondsWord = wordTime.Elapsed.TotalSeconds;
                    double elapsedSecondsTotal = totalTime.Elapsed.TotalSeconds;
                    TimeSpan eta = TimeSpan.FromSeconds((elapsedSecondsTotal / (((double)(i + 1) / possible[0].Length) + j) * all.Count) - elapsedSecondsTotal);
                    Console.WriteLine($"Trying starter \"{word}\" (Testing word: \"{correct}\"). Progress: {i * 100 / possible[0].Length}% (ETA Starter: {TimeSpan.FromSeconds((elapsedSecondsWord / (i + 1) * possible[0].Length) - elapsedSecondsWord):hh\\:mm\\:ss} Total: {eta.Days}d {eta:hh\\:mm\\:ss})       ");
                    (float a, float b) = RateStarter(word, possible[0].Length, correct, possibleLetters![correct], 1);
                    succes += a;
                    avr += b;
                }
                succes /= possible[0].Length;
                avr /= possible[0].Length;
                string write = $"Rating for {word}: Succes rate {succes} Average depth {avr} completed in {wordTime.Elapsed.TotalSeconds} seconds";
                if (write.Length < 114)
                    write += new string(' ', 114 - write.Length);
                Console.WriteLine(write);
                File.AppendAllText("ratings.csv", $"{word};{succes};{avr};{wordTime.Elapsed.TotalSeconds}\n");
            }
        }

        private static (float, float) RateStarter(string newWord, int possibleCount, string correctWord, CharDictionary correctLetters, int depth)
        {
            if (depth == 6)
                return (0, depth);
            knownLetters.Clear();
            grey.Clear();
            yellow.Clear();
            green.Clear();
            for (int i = 0; i < newWord.Length; i++)
            {
                char c = newWord[i];
                if (correctLetters.Contains(c))
                {
                    knownLetters.Add(c);
                    if (correctWord[i] == c)
                        green.Add(c, i);
                    else
                        yellow.Add(c, i);
                }
                else
                    grey.Add(c);
            }
            int count = 0;
            int prev = depth - 1;
            for (int i = 0; i < possibleCount; i++)
            {
                string word = possible[prev][i];
                if (word == correctWord || knownLetters.MatchWord(possibleLetters![word], correctLetters) || grey.MatchWord(word) || yellow.MatchWord(word) || green.MatchWord(word))
                    continue;
                possible[depth][count++] = word;
            }
            float succes = 1;
            float avr = depth;
            for (int i = 0; i < count; i++)
            {
                (float a, float b) = RateStarter(possible[depth][i], count, correctWord, correctLetters, depth + 1);
                succes += a;
                avr += b;
            }
            count++;
            return (succes / count, avr / count);
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