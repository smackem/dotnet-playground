using BenchmarkDotNet.Attributes;
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

    [MemoryDiagnoser]
    public class BitCountBenchmarks
    {
        static readonly int[] BitCountsPerNibble = new[]
        {
            0, //0
            1, //1
            1, //2
            2, //3
            1, //4
            2, //5
            2, //6
            3, //7
            1, //8
            2, //9
            2, //10
            3, //11
            2, //12
            3, //13
            3, //14
            4, //15
        };

        static readonly int[] BitCountsPerByte;

        static BitCountBenchmarks()
        {
            BitCountsPerByte = new int[256];
            var b = new BitCountBenchmarks();

            for (int i = 0; i < 256; i++)
                BitCountsPerByte[i] = b.BitCount_NaiveBitwise(i);
        }

        int BitCount_NaiveBitwise(int n)
        {
            var count = 0;
            var u = (uint)n;

            while (u > 0)
            {
                count += (int)(u & 1);
                u >>= 1;
            }

            return count;
        }

        int BitCount_Nibblewise(int n)
        {
            var count = 0;
            var u = (uint)n;

            while (u > 0)
            {
                count += BitCountsPerNibble[u & 0xf];
                u >>= 4;
            }

            return count;
        }

        int BitCount_Bytewise(int n)
        {
            var count = 0;
            var u = (uint)n;

            while (u > 0)
            {
                count += BitCountsPerByte[u & 0xff];
                u >>= 8;
            }

            return count;
        }

        int BitCount_BytewiseNoLoop(int n)
        {
            return BitCountsPerByte[(n & 0x000000ff) >> 0]
                 + BitCountsPerByte[(n & 0x0000ff00) >> 8]
                 + BitCountsPerByte[(n & 0x00ff0000) >> 16]
                 + BitCountsPerByte[(n & 0xff000000) >> 24];
        }

        int BitCount_Swar(int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        [Benchmark]
        public void NaiveBitwise()
        {
            if (BitCount_NaiveBitwise(0) != 0) throw new Exception();
            if (BitCount_NaiveBitwise(1) != 1) throw new Exception();
            if (BitCount_NaiveBitwise(0xf) != 4) throw new Exception();
            if (BitCount_NaiveBitwise(0xff) != 8) throw new Exception();
            if (BitCount_NaiveBitwise(0xffff) != 16) throw new Exception();
            if (BitCount_NaiveBitwise(0xffffff) != 24) throw new Exception();
            if (BitCount_NaiveBitwise(unchecked((int)0xfffffffe)) != 31) throw new Exception();
        }

        [Benchmark]
        public void Nibblewise()
        {
            if (BitCount_Nibblewise(0) != 0) throw new Exception();
            if (BitCount_Nibblewise(1) != 1) throw new Exception();
            if (BitCount_Nibblewise(0xf) != 4) throw new Exception();
            if (BitCount_Nibblewise(0xff) != 8) throw new Exception();
            if (BitCount_Nibblewise(0xffff) != 16) throw new Exception();
            if (BitCount_Nibblewise(0xffffff) != 24) throw new Exception();
            if (BitCount_Nibblewise(unchecked((int)0xfffffffe)) != 31) throw new Exception();
        }

        [Benchmark]
        public void Bytewise()
        {
            if (BitCount_Bytewise(0) != 0) throw new Exception();
            if (BitCount_Bytewise(1) != 1) throw new Exception();
            if (BitCount_Bytewise(0xf) != 4) throw new Exception();
            if (BitCount_Bytewise(0xff) != 8) throw new Exception();
            if (BitCount_Bytewise(0xffff) != 16) throw new Exception();
            if (BitCount_Bytewise(0xffffff) != 24) throw new Exception();
            if (BitCount_Bytewise(unchecked((int)0xfffffffe)) != 31) throw new Exception();
        }

        [Benchmark]
        public void Bytewise_NoLoop()
        {
            if (BitCount_BytewiseNoLoop(0) != 0) throw new Exception();
            if (BitCount_BytewiseNoLoop(1) != 1) throw new Exception();
            if (BitCount_BytewiseNoLoop(0xf) != 4) throw new Exception();
            if (BitCount_BytewiseNoLoop(0xff) != 8) throw new Exception();
            if (BitCount_BytewiseNoLoop(0xffff) != 16) throw new Exception();
            if (BitCount_BytewiseNoLoop(0xffffff) != 24) throw new Exception();
            if (BitCount_BytewiseNoLoop(unchecked((int)0xfffffffe)) != 31) throw new Exception();
        }

        [Benchmark]
        public void Swar()
        {
            if (BitCount_Swar(0) != 0) throw new Exception();
            if (BitCount_Swar(1) != 1) throw new Exception();
            if (BitCount_Swar(0xf) != 4) throw new Exception();
            if (BitCount_Swar(0xff) != 8) throw new Exception();
            if (BitCount_Swar(0xffff) != 16) throw new Exception();
            if (BitCount_Swar(0xffffff) != 24) throw new Exception();
            if (BitCount_Swar(unchecked((int)0xfffffffe)) != 31) throw new Exception();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<CompileMachineBenchmarks>();
            BenchmarkRunner.Run<BitCountBenchmarks>();
        }
    }
}
