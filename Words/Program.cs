using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using System.Globalization;
using System.Text;

namespace Words
{
    class Program
    {
        readonly string _dictionaryFileName;
        readonly CultureInfo _culture;
        readonly Encoding _encoding;

        Program(string dictionaryFileName, CultureInfo culture, Encoding encoding)
        {
            _dictionaryFileName = dictionaryFileName
                ?? throw new ArgumentNullException(nameof(dictionaryFileName));
            _culture = culture
                ?? throw new ArgumentNullException(nameof(culture));
            _encoding = encoding
                ?? throw new ArgumentNullException(nameof(encoding));
        }

        static int Main(string[] args)
        {
            var cli = new CommandLineApplication();
            var fileNameArg = cli.Argument("DictionaryFileName", "Name of the file that contains the dictionary");
            var cultureOption = cli.Option("-c | --culture", "The locale to use. Default: system locale.", CommandOptionType.SingleValue);
            var encodingOption = cli.Option("-e | --encoding", "The file encoding. Default: UTF-8.", CommandOptionType.SingleValue);
            cli.HelpOption("-? | --help");
            cli.Name = "Words";
            cli.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(fileNameArg.Value))
                {
                    cli.ShowHelp();
                    return 0;
                }

                try
                {
                    var culture = cultureOption.HasValue()
                        ? CultureInfo.GetCultureInfo(cultureOption.Value())
                        : CultureInfo.CurrentCulture;
                    var encoding = encodingOption.HasValue()
                        ? Encoding.GetEncoding(encodingOption.Value())
                        : Encoding.UTF8;
                    var program = new Program(fileNameArg.Value, culture, encoding);
                    program.Run();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                    return 1;
                }

                return 0;
            });

            return cli.Execute(args);
        }

        void Run()
        {
            Console.Write($"Reading Dictionary from '{_dictionaryFileName}'");
            var dict = ReadDictionary(_ => Console.Write("."));
            Console.WriteLine();

            while (true)
            {
                Console.Write("Enter Characters: ");
                var chars = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(chars))
                    break;

                var normalized = CanonicalizeDistinct(chars);
                if (dict.TryGetValue(normalized, out var list))
                {
                    foreach (var word in list)
                        Console.WriteLine(word);
                }
                else
                {
                    Console.WriteLine("No Matches!");
                }
            }
        }

        IDictionary<string, IList<string>> ReadDictionary(Action<int> progressCallback)
        {
            var dict = new Dictionary<string, IList<string>>();

            foreach (var word in EnumerateWords(progressCallback))
            {
                var normalized = CanonicalizeDistinct(word);
                if (dict.TryGetValue(normalized, out var list) == false)
                {
                    list = new List<string>();
                    dict.Add(normalized, list);
                }
                list.Add(word);
            }

            return dict;
        }

        string CanonicalizeDistinct(string s)
        {
            var chars = s.Select(ch => char.ToLower(ch, _culture))
                .Distinct()
                .OrderBy(ch => ch)
                .ToArray();
            return new string(chars);
        }

        string Canonicalize(string s)
        {
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                chars[i] = Char.ToLower(chars[i], _culture);
            Array.Sort(chars);
            return new string(chars);
        }

        IEnumerable<string> EnumerateWords(Action<int> progressCallback)
        {
            using (var stream = File.OpenRead(_dictionaryFileName))
            using (var reader = new StreamReader(stream, _encoding))
            {
                string line;
                var percent = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;

                    var p = (int)(stream.Position * 100 / stream.Length);
                    if (p != percent)
                    {
                        progressCallback(p);
                        percent = p;
                    }
                }
            }
        }
    }
}
