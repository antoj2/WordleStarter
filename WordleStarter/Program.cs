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

        static void Main(string[] args)
        {
            Console.WriteLine("Locating files");
            List<string> all = GetAll();
            List<string> possible = [.. File.ReadAllLines("possible.txt")];
            possibleLetters = new Dictionary<string, CharDictionary>(possible.Count);
            for (int i = 0; i < possible.Count; i++)
                possibleLetters[possible[i]] = new(possible[i]);
            Console.WriteLine("Calculating Best Wordle Starter...");
            Stopwatch totalTime = Stopwatch.StartNew();
            Dictionary<string, double> ratings = new(all.Count);
            for (int j = 0; j < all.Count; j++)
            {
                string word = all[j];
                double rating = 0;
                Stopwatch wordTime = Stopwatch.StartNew();
                for (int i = 1; i < possible.Count; i++)
                {
                    string correct = possible[i];
                    Console.SetCursorPosition(0, 2);
                    double elapsedSecondsWord = wordTime.Elapsed.TotalSeconds;
                    double elapsedSecondsTotal = totalTime.Elapsed.TotalSeconds;
                    TimeSpan eta = TimeSpan.FromSeconds((elapsedSecondsTotal / (((double)i / possible.Count) + j) * all.Count) - elapsedSecondsTotal);
                    Console.WriteLine($"Trying starter \"{word}\" (Testing word: \"{correct}\"). Progress: {i * 100 / possible.Count}% (ETA Starter: {TimeSpan.FromSeconds((elapsedSecondsWord / i * possible.Count) - elapsedSecondsWord):hh\\:mm\\:ss} Total: {eta.Days}d {eta:hh\\:mm\\:ss})       ");
                    rating += RateStarter(word, (correct, possibleLetters![correct], [.. possible[..i], .. possible[(i + 1)..]]), 0);
                }
                ratings[word] = rating / possible.Count;
                string write = $"Rating for {word}: {ratings[word]} completed in {wordTime.Elapsed.TotalSeconds} seconds";
                if (write.Length < 70)
                    write += new string(' ', 70 - write.Length);
                Console.WriteLine(write);
            }
        }

        private static float RateStarter(string newWord, (string word, CharDictionary letters, List<string> possible) correct, int depth)
        {
            if (depth == 6)
                return 0;
            knownLetters.Clear();
            grey.Clear();
            yellow.Clear();
            green.Clear();
            for (int i = 0; i < newWord.Length; i++)
            {
                char c = newWord[i];
                if (correct.letters.Contains(c))
                {
                    knownLetters.Add(c);
                    if (correct.word[i] == c)
                        green.Add(c, i);
                    else
                        yellow.Add(c, i);
                }
                else
                    grey.Add(c);
            }
            float succes = 1;
            for (int i = 0; i < correct.possible.Count;)
            {
                string word = correct.possible[i];
                if (knownLetters.MatchWord(possibleLetters![word], correct.letters) || grey.MatchWord(word) || yellow.MatchWord(word) || green.MatchWord(word))
                    correct.possible.RemoveAt(i);
                else
                    i++;
            }
            foreach (string word in correct.possible)
                succes += RateStarter(word, (correct.word, correct.letters, [.. correct.possible]), depth + 1);
            return succes / (correct.possible.Count + 1);
        }

        private static List<string> GetAll()
        {
            string[] allFile = File.ReadAllLines("all.txt");
            List<string> all = [];
            foreach (string word in allFile)
            {
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