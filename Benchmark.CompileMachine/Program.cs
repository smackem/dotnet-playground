﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CompileMachine.Lexing;
using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark.CompileMachine
{
    [MemoryDiagnoser]
    public class CompileMachineBenchmarks
    {
        static readonly string Source =
            "hello world (and others) + all the rest \"and even you\"\r\n" +
            "this text includes += operators -= +-/ and \"strings\" even \"\" empty ones\r\n" +
            "numbers are 123 also present 456 789and \"long looooonngggg stringssss\tsssss\"\r\n" +
            "lets do math f(x)=sin(x)/cos(x) * (911 - (4 + 711)) / sqrt(3393923 + 4423123)\t\r\n" +
            "those operators with more than 00000001 characters    += -= == /= *=\r\r\n\n";

        [Benchmark]
        public void Regex()
        {
            if (RegexLexer.Lex(Source) == null)
                throw new Exception("Lexing error");
        }

        [Benchmark]
        public void Charwise()
        {
            if (CharwiseLexer.Lex(Source) == null)
                throw new Exception("Lexing error");
        }

        [Benchmark]
        public void StateMachine()
        {
            var lexer = new Lexer();

            using (var charReader = CharReader.FromTextReader(new StringReader(Source)))
            {
                var arguments = new Dictionary<string, object>
                {
                    { nameof(Lexer.Reader), charReader }
                };

                var result = WorkflowInvoker.Invoke(lexer, arguments);
                var lexemes = (IReadOnlyList<string>)result[nameof(Lexer.Lexemes)];
                if (lexemes == null)
                    throw new Exception("Lexing error");
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<CompileMachineBenchmarks>();
        }
    }
}