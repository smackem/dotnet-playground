using System;
using System.Activities;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompileMachine.Lexing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.CompileMachine
{
    [TestClass]
    public class LexerTest
    {
        static readonly string Source =
            "hello world (and others) + all the rest \"and even you\"\r\n" +
            "this text includes += operators -= +-/ and \"strings\" even \"\" empty ones\r\n" +
            "numbers are 123 also present 456 789and \"long looooonngggg stringssss\tsssss\"\r\n" +
            "lets do math f(x)=sin(x)/cos(x) * (911 - (4 + 711)) / sqrt(3393923 + 4423123)\t\r\n" +
            "those operators with more than 00000001 characters    += -= == /= *=\r\r\n\n";

        Func<string, string[]> _lex;

        [TestInitialize]
        public void Init()
        {
            _lex = LexWithStateMachine;
        }

        [TestMethod]
        public void TestCharwiseLexing()
        {
            _lex = LexCharwise;
            TestLexWord();
            TestLexNumber();
            TestLexOperator();
            TestLexString();
            TestLexEmpty();
            TestLexWithWhitespace();
            TestLexWordBorders();
            TestInvalidChars();
            TestUnclosedString();
        }

        [TestMethod]
        public void TestRegexLexing()
        {
            _lex = LexRegex;
            TestLexWord();
            TestLexNumber();
            TestLexOperator();
            TestLexString();
            TestLexEmpty();
            TestLexWithWhitespace();
            TestLexWordBorders();
            TestInvalidChars();
            TestUnclosedString();
        }

        [TestMethod]
        public void TestLexWord()
        {
            CollectionAssert.AreEqual(new[] { "abc" }, _lex("abc"));
            CollectionAssert.AreEqual(new[] { "a1", "a2", "a3", "XYZ" }, _lex("a1 a2 a3 XYZ"));
        }

        [TestMethod]
        public void TestLexNumber()
        {
            CollectionAssert.AreEqual(new[] { "123" }, _lex("123"));
            CollectionAssert.AreEqual(new[] { "1", "2", "3", "456789" }, _lex("1 2 3 456789"));
        }

        [TestMethod]
        public void TestLexOperator()
        {
            CollectionAssert.AreEqual(new[] { "+", "+=", "-=", "/", "/=", "(", ")" }, _lex("+ += -= / /= ( )"));
            CollectionAssert.AreEqual(new[] { "+=", "+", "-=", "-", "/=", "/" }, _lex("+=+-=-/=/"));
            CollectionAssert.AreEqual(new[] { "-", "-=", "*", "*=", "==", "(", ")" }, _lex("- -= **= ==()"));
        }

        [TestMethod]
        public void TestLexString()
        {
            CollectionAssert.AreEqual(new[] { "\"a b c 1 2 3 + - / xyz\"" }, _lex("\"a b c 1 2 3 + - / xyz\""));
            CollectionAssert.AreEqual(new[] { "\"\"" }, _lex("\"\""));
            CollectionAssert.AreEqual(new[] { "\"\"", "\"\"" }, _lex("\"\"\"\""));
        }

        [TestMethod]
        public void TestLexEmpty()
        {
            var empty = Array.Empty<string>();
            CollectionAssert.AreEqual(empty, _lex(""));
            CollectionAssert.AreEqual(empty, _lex(" \r\n\n \t  "));
        }

        [TestMethod]
        public void TestLexWithWhitespace()
        {
            CollectionAssert.AreEqual(new[] { "a", "b", "c", "1", "2", "3", "+" }, _lex("\t\r \na b  c\r1\n2\t\t\t  3\t\r\n +\t"));
        }

        [TestMethod]
        public void TestLexWordBorders()
        {
            CollectionAssert.AreEqual(new[] { "123", "abc" }, _lex("123abc"));
            CollectionAssert.AreEqual(new[] { "1", "+", "a", "-", "22", "b", "\"s \"", "x3", "/", "4" }, _lex("1+a-22b\"s \"x3/4"));
        }

        [TestMethod]
        public void TestInvalidChars()
        {
            foreach (var ch in @"\!@#$%^&?|~`")
                Assert.IsNull(_lex(ch.ToString()));
        }

        [TestMethod]
        public void TestUnclosedString()
        {
            Assert.IsNull(_lex("\"unclosed string"));
        }

        [TestMethod]
        public void TestCompareLexMethods()
        {
            var lexemes1 = LexRegex(Source);
            var lexemes2 = LexCharwise(Source);
            var lexemes3 = LexWithStateMachine(Source);

            Assert.IsNotNull(lexemes1);
            CollectionAssert.AreEqual(lexemes1, lexemes2);
            CollectionAssert.AreEqual(lexemes2, lexemes3);
        }

        string[] LexRegex(string source)
        {
            return RegexLexer.Lex(source)?.ToArray();
        }

        string[] LexCharwise(string source)
        {
            return CharwiseLexer.Lex(source)?.ToArray();
        }

        string[] LexWithStateMachine(string source)
        {
            var lexer = new Lexer();

            using (var charReader = CharReader.FromTextReader(new StringReader(source)))
            {
                var arguments = new Dictionary<string, object>
                {
                    { nameof(Lexer.Reader), charReader }
                };

                var result = WorkflowInvoker.Invoke(lexer, arguments);
                var lexemes = (IReadOnlyList<string>)result[nameof(Lexer.Lexemes)];
                return lexemes?.ToArray();
            }
        }
    }
}
