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
        byte[] Bytemap;
        CharSet Chars;

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

        public bool Contains(char ch) => Chars.Chars.Contains(ch);

        public bool MatchWord(string word, CharDictionary correct)
        {
            CharDictionary letters = new(word);
            foreach (char ch in Chars.Chars)
                if (!letters.Contains(ch) || (letters.Bytemap[ch] < Bytemap[ch] && Bytemap[ch] <= correct.Bytemap[ch]) || (Bytemap[ch] > correct.Bytemap[ch] && letters.Bytemap[ch] > correct.Bytemap[ch]))
                    return true;
            return false;
        }
    }

    internal class Program
    {
        CharDictionary knownLetters = new();
        CharSet grey = new();
        List<(char c, int i)> yellow = new(5);
        List<(char c, int i)> green = new(5);

        static void Main(string[] args)
        {
            Console.WriteLine("Locating files");
            string[] all = File.ReadAllLines("all.txt");
            List<string> possible = [.. File.ReadAllLines("possible.txt")];
            Console.WriteLine("Calculating Best Wordle Starter...");
            List<(HashSet<int> id, float rating)> ratings = [];
            Stopwatch totalTime = Stopwatch.StartNew();
            for (int j = 0; j < all.Length; j++)
            {
                string word = all[j];
                int mask = 0;

                bool skip = false;
                foreach (char c in word)
                {
                    int bit = 1 << (c - 'a');

                    if ((mask & bit) != 0)
                    {
                        skip = true;
                        break;
                    }

                    mask |= bit;
                }
                if (skip)
                    continue;

                double rating = 0;
                Stopwatch wordTime = Stopwatch.StartNew();
                for (int i = 1; i < possible.Count; i++)
                {
                    string correct = possible[i];
                    Dictionary<char, int> correctLetters = [];
                    foreach (char c in correct)
                        correctLetters[c] = correctLetters.TryGetValue(c, out int value) ? ++value : 1;
                    
                    Console.SetCursorPosition(0, 2);
                    double elapsedSecondsWord = wordTime.Elapsed.TotalSeconds;
                    double elapsedSecondsTotal = totalTime.Elapsed.TotalSeconds;
                    TimeSpan eta = TimeSpan.FromSeconds((elapsedSecondsTotal / (((double)i / possible.Count) + j) * all.Length) - elapsedSecondsTotal);
                    Console.WriteLine($"Trying starter \"{word}\" (Testing word: \"{correct}\"). Progress: {i * 100 / possible.Count}% (ETA Starter: {TimeSpan.FromSeconds((elapsedSecondsWord / i * possible.Count) - elapsedSecondsWord):hh\\:mm\\:ss} Total: {eta.Days}d {eta:hh\\:mm\\:ss})       ");
                    rating += RateStarter([word], word, all, (correct, correctLetters, [.. possible[..i], .. possible[(i + 1)..]]), ref ratings, 0);
                }
                Console.WriteLine($"Rating for {word}: {rating / possible.Count}");
            }
        }

        private static float RateStarter(string[] words, string newWord, string[] all, (string word, Dictionary<char, int> letters, List<string> possible) correct, ref List<(HashSet<int> id, float rating)> ratings, byte depth)
        {
            if (depth == 6)
                return 0;
            Dictionary<char, int> knownLetters = [];
            HashSet<char> grey = [];
            HashSet<(char c, int i)> yellow = [];
            HashSet<(char c, int i)> green = [];
            for (int i = 0; i < newWord.Length; i++)
            {
                char c = newWord[i];
                if (correct.letters.ContainsKey(c))
                {
                    knownLetters[c] = knownLetters.TryGetValue(c, out int value) ? ++value : 1;
                    if (correct.word[i] == c)
                        green.Add((c, i));
                    else
                        yellow.Add((c, i));
                }
                else
                {
                    grey.Add(c);
                }
            }

            int total = 0;
            float succes = 1;
            while (total < correct.possible.Count)
            {
                string word = correct.possible[total];
                bool skip = false;
                Dictionary<char, int> letters = [];
                foreach (char c in word)
                    letters[c] = letters.TryGetValue(c, out int value) ? ++value : 1;
                foreach ((char c, int a) in knownLetters)
                {
                    if (!letters.TryGetValue(c, out int value) || (value < a && knownLetters[c] <= correct.letters[c]) || (knownLetters[c] > correct.letters[c] && value > correct.letters[c]))
                    {
                        skip = true;
                        break;
                    }
                }
                if (!skip)
                {
                    foreach (char c in grey)
                    {
                        if (word.Contains(c))
                        {
                            skip = true;
                            break;
                        }
                    }
                }
                if (!skip)
                {
                    foreach ((char c, int i) in yellow)
                    {
                        if (word[i] == c)
                        {
                            skip = true;
                            break;
                        }
                    }
                }
                if (!skip)
                {
                    foreach ((char c, int i) in green)
                    {
                        if (word[i] != c)
                        {
                            skip = true;
                            break;
                        }
                    }
                }
                if (skip)
                {
                    correct.possible.RemoveAt(total);
                    continue;
                }
                
                total++;
            }
            foreach (string word in correct.possible)
            {
                succes += RateStarter([.. words, word], word, all, (correct.word, correct.letters, [.. correct.possible]), ref ratings, (byte)(depth + 1));
            }
            //if (depth == 0)
            //{
            //    HashSet<int> input = [];
            //    foreach (string word in words)
            //        input.Add(all.BinarySearch(word, StringComparer.OrdinalIgnoreCase));
            //    ratings.Add((id: input, rating: succes / total));
            //}
            if (succes > total + 1)
                Debugger.Break();
            return succes / (total + 1);
        }
    }
}