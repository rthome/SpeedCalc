using SpeedCalc.Core.Runtime;

using System.IO;

using Xunit;

namespace SpeedCalc.Tests.Core.Runtime
{
    public class ScriptExecutionTests
    {
        static void CompilerErrors(string source)
        {
            var chunk = new Chunk();
            Assert.False(Parser.Compile(source, chunk));
        }

        static void RunScript(string source)
        {
            var chunk = new Chunk();
            Assert.True(Parser.Compile(source, chunk));

            var vm = new VirtualMachine();
            vm.Interpret(chunk);
        }

        static string RunScriptAndCaptureOutput(string source)
        {
            var chunk = new Chunk();
            Assert.True(Parser.Compile(source, chunk));

            var vm = new VirtualMachine();
            var writer = new StringWriter();

            vm.SetStdOut(writer);
            vm.Interpret(chunk);

            return writer.ToString().TrimEnd('\r', '\n');
        }

        static void RunScriptAndExpect(string expectedResult, string source) => Assert.Equal(expectedResult, RunScriptAndCaptureOutput(source));

        [Fact]
        public void RunsSingleNumberExpr()
        {
            RunScriptAndExpect("1024", "print 1024;");
        }

        [Fact]
        public void RunsSingleBoolExprs()
        {
            RunScriptAndExpect("true", "print true;");
            RunScriptAndExpect("false", "print false;");
        }

        [Fact]
        public void RunsAddExpr()
        {
            RunScriptAndExpect("11", "print 10 + 1;");
            RunScriptAndExpect("2.0", "print 0.5 + 1.5;");
        }

        [Fact]
        public void RunsTwoAddExprs()
        {
            RunScriptAndExpect("6", "print 1+2+3;");
        }

        [Fact]
        public void RunsMultipleAddExprs()
        {
            RunScriptAndExpect("81", "print 1+2+3+4+5+6+7+8+9+8+7+6+5+4+3+2+1;");
        }

        [Fact]
        public void RunsGroupedAddAndMulExpr()
        {
            RunScriptAndExpect("11", "print (2+3)+(2*3);");
        }

        [Fact]
        public void RunsSubtractExpr()
        {
            RunScriptAndExpect("6999", "print 7000 - 1;");
        }

        [Fact]
        public void RunsDivisionExpr()
        {
            RunScriptAndExpect("5", "print 10/2;");
        }

        [Fact]
        public void RunsExponentiationExpr()
        {
            RunScriptAndExpect("25", "print 5**2;");
        }

        [Fact]
        public void RunsPrintStmt()
        {
            var output = RunScriptAndCaptureOutput("print 1000;");
            Assert.Equal("1000", output);
        }

        [Fact]
        public void RunsGlobalNumberStmt()
        {
            RunScript("var GlobalVariable = 1000;");
        }

        [Fact]
        public void RunsGlobalBoolStmt()
        {
            RunScript("var GlobalBoolean = true;");
        }

        [Fact]
        public void RunsMultipleGlobalNumberStmts()
        {
            RunScript("var GlobalVariable = 1000; var AnotherGlobal = false;");
        }

        [Fact]
        public void RunsGlobalLookupStmt()
        {
            RunScriptAndExpect("1", "var SomeGlobal = 1; print SomeGlobal;");
        }

        [Fact]
        public void RunsGlobalAssignmentStmts()
        {
            RunScriptAndExpect("3", "var A = 1; var B = 2; var A = A + B; print A;");
        }

        [Fact]
        public void RunsDeclareLocal()
        {
            RunScript("{ var a = 1; }");
        }

        [Fact]
        public void RunsPrintLocal()
        {
            RunScriptAndExpect("1", "{ var a = 1; print a; }");
        }

        [Fact]
        public void RunsAddLocals()
        {
            RunScriptAndExpect("3", "{ var a = 1; var b = 2; print a + b; }");
        }

        [Fact]
        public void ErrorsOnVariableRedefinitionInSameScope()
        {
            CompilerErrors("{ var a = 1; var a = 100; }");
        }

        [Fact]
        public void RunsShadowedLocalPrint()
        {
            RunScriptAndExpect("true", "{ var a = 1; { var a = true; print a; } }");
        }

        [Fact]
        public void RunsLocalShadowingGlobalPrint()
        {
            RunScriptAndExpect("false", "var global = 1234; { var global = false; print global; }");
        }

        [Fact]
        public void RunsLocalAssignedToGlobalPrint()
        {
            RunScriptAndExpect("100", "var global = true; { var local = 100; global = local; print global; }");
        }

        [Fact]
        public void RunsGlobalAssignedToLocalPrint()
        {
            RunScriptAndExpect("100", "var global = 100; { var local = global; print local; }");
        }

        [Fact]
        public void RunsAssignmentToSelf()
        {
            RunScriptAndExpect("100", "{ var a = 100; a = a; print a; }");
        }

        [Fact]
        public void ErrorsOnAssignmentFromOuterScope()
        {
            CompilerErrors("{ var a = 100; { var a = a; } }");
        }

        [Fact]
        public void ErrorsOnDefiningLocalWithItself()
        {
            CompilerErrors("{ var a = a; }");
        }

        [Fact]
        public void RunsAdditionAssignmentOperator()
        {
            RunScriptAndExpect("1", "var a = 0; a += 1; print a;");
        }

        [Fact]
        public void RunsSubtractionAssignmentOperator()
        {
            RunScriptAndExpect("2", "var a = 3; a -= 1; print a;");
        }

        [Fact]
        public void RunsMultiplicationAssignmentOperator()
        {
            RunScriptAndExpect("20", "var a = 5; a *= 4; print a;");
        }

        [Fact]
        public void RunsDivisionAssignmentOperator()
        {
            RunScriptAndExpect("2", "var a = 10; a /= 5; print a;");
        }

        [Fact]
        public void RunsExponentiationAssignmentOperator()
        {
            RunScriptAndExpect("9", "var a = 3; a **= 2; print a;");
        }

        [Fact]
        public void ErrorsOnArithmeticAssignmentOperatorInDefinition()
        {
            CompilerErrors("var a += 1;");
            CompilerErrors("var a -= 1;");
            CompilerErrors("var a *= 1;");
            CompilerErrors("var a /= 1;");
            CompilerErrors("var a **= 1;");
        }

        [Fact]
        public void RunsIf()
        {
            RunScriptAndExpect("true", "if true: print true;");
        }

        [Fact]
        public void RunsIfWithComparisonCond()
        {
            RunScriptAndExpect("1", "if 1+1 > 1: print 1;");
        }

        [Fact]
        public void RunsIfWithEqualityCond()
        {
            RunScriptAndExpect("1", "if 2*2*2 == 8: print 1;");
        }

        [Fact]
        public void RunsIfDoesNotRunWhenCondIsFalse()
        {
            RunScriptAndExpect("2", "if false: print 1; print 2;");
        }

        [Fact]
        public void RunsIfWithBlock()
        {
            RunScriptAndExpect("1", "if true: { var a = 1; print a; }");
        }

        [Fact]
        public void RunsIfWithElse()
        {
            RunScriptAndExpect("true", "if true: print true; else: print false;");
        }

        [Fact]
        public void RunsElse()
        {
            RunScriptAndExpect("false", "if false: print true; else: print false;");
        }

        [Fact]
        public void RunsElseWithBlock()
        {
            RunScriptAndExpect("2", "if false: { var a = 1; print a; } else: { var a = 2; print a; }");
        }

        [Fact]
        public void RunsAnd()
        {
            RunScriptAndExpect("false", "print false and false;");
            RunScriptAndExpect("false", "print true and false;");
            RunScriptAndExpect("false", "print false and true;");
            RunScriptAndExpect("true", "print true and true;");
        }

        [Fact]
        public void RunsNestedAnd()
        {
            RunScriptAndExpect("false", "print true and true and false;");
            RunScriptAndExpect("false", "print false and true and false;");
            RunScriptAndExpect("true", "print true and true and true;");
        }

        [Fact]
        public void RunsOr()
        {
            RunScriptAndExpect("false", "print false or false;");
            RunScriptAndExpect("true", "print true or false;");
            RunScriptAndExpect("true", "print false or true;");
            RunScriptAndExpect("true", "print true or true;");
        }

        [Fact]
        public void RunsNestedOr()
        {
            RunScriptAndExpect("true", "print true or true or false;");
            RunScriptAndExpect("true", "print false or true or false;");
            RunScriptAndExpect("true", "print true or true or true;");
            RunScriptAndExpect("false", "print false or false or false;");
        }

        [Fact]
        public void RunsAndWithOr() // 'and' has higher precedence
        {
            RunScriptAndExpect("false", "print false or true and false;"); 
            RunScriptAndExpect("true", "print 1 and true or false;");
        }

        [Fact]
        public void RunsIfWithTrueAnd()
        {
            RunScriptAndExpect("true", "if 1 and 2: print true; else: print false;");
        }

        [Fact]
        public void RunsIfWithFalseAnd()
        {
            RunScriptAndExpect("false", "if true and false: print true; else: print false;");
            RunScriptAndExpect("false", "if false and true: print true; else: print false;");
        }

        [Fact]
        public void RunsIfWithAndVariables()
        {
            RunScriptAndExpect("true", "var c1 = 1000; var c2 = true; if c1 and c2: print true; else: print false;");
        }

        [Fact]
        public void RunsWhileLoopWithAddition()
        {
            const string Script = @"var s = 0;
                                    while s < 10:
                                        s += 1;
                                    print s;";

            RunScriptAndExpect("10", Script);
        }

        [Fact]
        public void RunsWhileLoopWithModifyingBoolCondition()
        {
            const string Script = @"var s = false;
                                    while !s:
                                        s = true;
                                    print s;";

            RunScriptAndExpect("true", Script);
        }

        [Fact]
        public void RunsWhileLoopWithRepeatedPrint()
        {
            const string Script = @"var s = 10;
                                    while s > 0:
                                    {
                                        s -= 1;
                                        print 1;
                                    }";

            RunScriptAndExpect("1\r\n1\r\n1\r\n1\r\n1\r\n1\r\n1\r\n1\r\n1\r\n1", Script);
        }

        [Fact]
        public void RunsWhileWithIfInBody()
        {
            const string Script = @"var s = 10;
                                    while s > 0:
                                    {
                                        s -= 1;
                                        if s == 3:
                                            print 333;
                                    }";

            RunScriptAndExpect("333", Script);
        }

        [Fact]
        public void RunsSimpleForLoop()
        {
            const string Script = @"for var i = true; i;: { print i; i = false; }";

            RunScriptAndExpect("true", Script);
        }

        [Fact]
        public void RunsIncremeningForLoop()
        {
            const string Script = @"for var i = 0; i < 5; i += 1: { print i; }";

            RunScriptAndExpect("0\r\n1\r\n2\r\n3\r\n4", Script);
        }

        [Fact]
        public void RunsForLoopWithPredefinedVariable()
        {
            const string Script = @"var i = 0; for ; i < 5; i += 1: { } print i;";

            RunScriptAndExpect("5", Script);
        }

        [Fact]
        public void RunsForLoopWithAssignmentToGlobal()
        {
            const string Script = @"var i = 0;
                                    for var c = 0; c < 10; c += 1: { i += c; } 
                                    print i;";

            RunScriptAndExpect("45", Script);
        }

        [Fact]
        public void RunsNestedForLoop()
        {
            const string Script = @"var c = 0;
                                    for var i = 0; i < 3; i += 1: {
                                        for var j = 0; j < 3; j += 1: {
                                            c = c + 1;
                                        }
                                    } 
                                    print c;";

            RunScriptAndExpect("9", Script);
        }
    }
}
