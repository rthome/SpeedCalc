﻿using SpeedCalc.Core.Runtime;

using System;
using System.IO;
using System.Text;

using Xunit;

namespace SpeedCalc.Tests.Core.Runtime
{
    public class ScriptExecutionTests
    {
        sealed class DebugTextWriter : StreamWriter
        {
            sealed class DebugStream : Stream
            {
                static readonly NotSupportedException NotSupported = new();

                public override bool CanRead => false;
                public override bool CanSeek => false;
                public override bool CanWrite => true;

                public override void Flush() => System.Diagnostics.Debug.Flush();

                public override long Length => throw NotSupported;
                public override int Read(byte[] buffer, int offset, int count) => throw NotSupported;
                public override long Seek(long offset, SeekOrigin origin) => throw NotSupported;
                public override void SetLength(long value) => throw NotSupported;
                public override long Position { get => throw NotSupported; set => throw NotSupported; }
                
                public override void Write(byte[] buffer, int offset, int count) => System.Diagnostics.Debug.Write(Encoding.Unicode.GetString(buffer, offset, count));
                
            }

            public DebugTextWriter()
                : base(new DebugStream(), Encoding.Unicode, 1024)
            {
                AutoFlush = true;
            }
        }

        static void CompilerErrors(string source, TextWriter output = null)
        {
            output ??= new DebugTextWriter();
            var vm = new VirtualMachine(output, output);
            Assert.Equal(InterpretResult.CompileError, vm.Interpret(source));
        }

        static void RuntimeErrors(string source, TextWriter output = null)
        {
            output ??= new DebugTextWriter();
            var vm = new VirtualMachine(output, output);
            Assert.Equal(InterpretResult.RuntimeError, vm.Interpret(source));
        }

        static void RunScript(string source, TextWriter output = null)
        {
            output ??= new DebugTextWriter();
            var vm = new VirtualMachine(output, output);
            Assert.Equal(InterpretResult.Success, vm.Interpret(source));
        }

        static string RunScriptAndCaptureOutput(string source)
        {
            var writer = new StringWriter();
            var vm = new VirtualMachine(writer, Console.Error);

            Assert.Equal(InterpretResult.Success, vm.Interpret(source));

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
        public void RunsGroupedExponentiationExpr()
        {
            RunScriptAndExpect("1000", "print (5+5)**3;");
        }

        [Fact]
        public void RunsModuloExpr()
        {
            RunScriptAndExpect("1", "print 5 mod 2;");
        }

        [Fact]
        public void RunsModuloWithMultiply()
        {
            RunScriptAndExpect("3", "print 5*3 mod 4;");
        }

        [Fact]
        public void RunsModuloWithExpAndAddition()
        {
            RunScriptAndExpect("5", "print 2 + 23 mod 2**2;");
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

        [Fact]
        public void RunsForLoopWithContinue()
        {
            const string Script = @"var c = 0;
                                    for var i = 0; i < 3; i += 1: {
                                        c += 1;
                                        continue;
                                        c += 1000;
                                    }
                                    print c;";

            RunScriptAndExpect("3", Script);
        }

        [Fact]
        public void RunsForLoopWithBreak()
        {
            const string Script = @"var c = 0;
                                    for var i = 0; i < 3; i += 1: {
                                        c += 1;
                                        break;
                                        c += 1000;
                                    }
                                    print c;";

            RunScriptAndExpect("1", Script);
        }

        [Fact]
        public void RunsWhileLoopWithContinue()
        {
            const string Script = @"var c = 0;
                                    while c < 3: {
                                        c += 1;
                                        continue;
                                        c += 1000;
                                    }
                                    print c;";

            RunScriptAndExpect("3", Script);
        }

        [Fact]
        public void RunsWhileLoopWithBreak()
        {
            const string Script = @"var c = 0;
                                    while c < 3: {
                                        c += 1;
                                        break;
                                        c += 1000;
                                    }
                                    print c;";

            RunScriptAndExpect("1", Script);
        }

        [Fact]
        public void RunsNestedWhileLoopWithContinue()
        {
            const string Script = @"var c = 0; 
                                    while c < 6: {
                                        var x = 0;
                                        while x < 3: {
                                            x += 1;
                                            continue;
                                            x += 1000;
                                        }
                                        c += x;
                                    }
                                    print c;";

            RunScriptAndExpect("6", Script);
        }

        [Fact]
        public void RunsNestedWhileLoopWithBreak()
        {
            const string Script = @"var c = 0; 
                                    while c < 2: {
                                        var x = 0;
                                        while x < 3: {
                                            x += 1;
                                            break;
                                            x += 1000;
                                        }
                                        c += x;
                                    }
                                    print c;";

            RunScriptAndExpect("2", Script);
        }

        [Fact]
        public void RunsNestedForLoopWithContinue()
        {
            const string Script = @"var c = 0; 
                                    for var i = 0; i < 2; i += 1: {
                                        var x = 0;
                                        for var j = 0; j < 3; j += 1: {
                                            x += 1;
                                            continue;
                                            x += 1000;
                                        }
                                        c += x;
                                    }
                                    print c;";

            RunScriptAndExpect("6", Script);
        }

        [Fact]
        public void RunsNestedForLoopWithBreak()
        {
            const string Script = @"var c = 0; 
                                    for var i = 0; i < 2; i += 1: {
                                        var x = 0;
                                        for var j = 0; j < 3; j += 1: {
                                            x += 1;
                                            break;
                                            x += 1000;
                                        }
                                        c += x;
                                    }
                                    print c;";

            RunScriptAndExpect("2", Script);
        }

        [Fact]
        public void ErrorsOnContinueOutsideOfALoop()
        {
            CompilerErrors("continue;");
            CompilerErrors("{ continue; }");
            CompilerErrors("if true: continue;");
        }

        [Fact]
        public void ErrorsOnBreakOutsideOfALoop()
        {
            CompilerErrors("break;");
            CompilerErrors("{ break; }");
            CompilerErrors("if true: break;");
        }

        [Fact]
        public void RunsEmptyFunctionDefinition()
        {
            RunScript("fn test() {}");
        }

        [Fact]
        public void RunsEmptyFunctionCall()
        {
            RunScript("fn test() {} test();");
        }

        [Fact]
        public void RunsEmptyScopedFunctionCalls()
        {
            RunScript("fn test() {} { test(); }");
            RunScript("{ fn test() {} test(); }");
            RunScript("{ fn test() {} { test(); } }");
        }

        [Fact]
        public void RunsPrintConstantInFunction()
        {
            RunScriptAndExpect("1", "fn test() { print 1; } test();");
        }

        [Fact]
        public void RunsPrintExpressionInFunction()
        {
            RunScriptAndExpect("9", "fn func() { print (1+2)*3; } func();");
            RunScriptAndExpect("9", "fn func() = (1+2)*3; print func();");
        }

        [Fact]
        public void RunsPrintResultOfExpressionInFunction()
        {
            RunScriptAndExpect("10", "fn add5and5() { return 5+5; } print add5and5();");
            RunScriptAndExpect("10", "fn add5and5() = 5+5; print add5and5();");
        }

        [Fact]
        public void RunPrintFromNestedFunctionCalls()
        {
            RunScriptAndExpect("1", "fn two() { print 1; } fn one() { two(); } one();");
            RunScriptAndExpect("2", "fn three() { print 2; } fn two() { three(); } fn one() { two(); } one();");
            RunScriptAndExpect("3", "fn four() { print 3; } fn three() { four(); } fn two() { three(); } fn one() { two(); } one();");

            RunScriptAndExpect("4", "fn three() = 4; fn two() { print three(); } fn one() = two(); one();");
            RunScriptAndExpect("5", "fn three() = 5; fn two() = three(); fn one() = two(); print one();");
        }

        [Fact]
        public void RunsNestedFunctionDefinitions()
        {
            RunScriptAndExpect("123", @"
                fn outer() {
                    fn inner() {
                        return 123;
                    }
                    return inner();
                }
                print outer();");

            RunScriptAndExpect("123", @"
                fn outer() {
                    fn inner() = 123;
                    return inner();
                }
                print outer();");

            RunScriptAndExpect("123", @"
                fn outer() {
                    fn inner() {
                        print 123;
                    }
                    inner();
                }
                outer();");
        }

        [Fact]
        public void RunsPrintingFunctionDefinition()
        {
            Assert.Contains("testFunc1", RunScriptAndCaptureOutput("fn testFunc1() {} print testFunc1;"));
            Assert.Contains("testFunc2", RunScriptAndCaptureOutput("fn testFunc2() = 1; print testFunc2;"));
            Assert.Contains("testFunc3", RunScriptAndCaptureOutput("fn testFunc3(a, b, c) {} print testFunc3;"));
            Assert.Contains("testFunc4", RunScriptAndCaptureOutput("fn testFunc4(a, b, c) = 1; print testFunc4;"));
        }

        [Fact]
        public void RunsFunctionsWithParameters()
        {
            RunScriptAndExpect("true", "fn printArg(arg) { print arg; } printArg(true);");
            RunScriptAndExpect("3", "fn add(a, b) { return a+b; } print add(1,2);");
            RunScriptAndExpect("3", "fn add(a, b) = a+b; print add(1,2);");
        }

        [Fact]
        public void RunsFucntionWithExpressionInParameters()
        {
            RunScriptAndExpect("15", "fn add(a, b, c) { return a+b+c; } print add(1+2,3,3*3);");
            RunScriptAndExpect("15", "fn add(a, b, c) = a+b+c; print add(1+2,3,3*3);");

            RunScriptAndExpect("8", "fn add(a, b, c) { return a+b+c; } print add(add(1,1,1),2,3);");
            RunScriptAndExpect("8", "fn add(a, b, c) = a+b+c; print add(add(1,1,1),2,3);");

            RunScriptAndExpect("20", "fn add(a, b, c) = a+b+c; print add(add(1,1,1),2,add(1,2,add(3,4,5)));");
        }

        [Fact]
        public void ErrorsOnCallToNonFunctionValue()
        {
            RuntimeErrors("var v = 1; v();");
            RuntimeErrors("var v = true; v();");
            RuntimeErrors("var a = 1; var b = a; b();");
        }

        [Fact]
        public void ErrorsOnCallWithWrongNumberOfParameters()
        {
            RuntimeErrors("fn a() {} a(1);");
            RuntimeErrors("fn a(x) {} a(1,2);");
            RuntimeErrors("fn a(x) {} a(1,2,3);");
            RuntimeErrors("fn a(x) {} a();");
            RuntimeErrors("fn a(x,y) {} a();");
            RuntimeErrors("fn a(x,y) {} a(true);");
            RuntimeErrors("fn a(x,y) {} a(true,false,true);");
        }

        [Fact]
        public void VMPrintsStackTraceOnRuntimeError()
        {
            var script = @"fn a() = b();
                           fn b() = c();
                           fn c() = c(1, 2, 3);
                           a();";

            var output = new StringWriter();
            RuntimeErrors(script, output);

            var result = output.ToString();
            Assert.Contains("fn a", result);
            Assert.Contains("fn b", result);
            Assert.Contains("fn c", result);
        }

        [Fact]
        public void VMAbortsUnrestrictedRecursionWithStackOverflow()
        {
            var output = new StringWriter();
            RuntimeErrors("fn a() { b(); } fn b() = a(); a();", output);

            var result = output.ToString();
            Assert.Contains("stack", result, StringComparison.InvariantCultureIgnoreCase);
            Assert.Contains("overflow", result, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void ErrorsOnTopLevelReturn()
        {
            CompilerErrors("return true;");
        }

        [Fact]
        public void CanCallBuiltinNatives()
        {
            var randomOutput = RunScriptAndCaptureOutput("print random();");
            var clockOutput = RunScriptAndCaptureOutput("print clock();");

            Assert.True(decimal.TryParse(randomOutput, out _));
            Assert.True(decimal.TryParse(clockOutput, out _));
        }

        [Fact]
        public void NativeCallsCheckArity()
        {
            RuntimeErrors("print random(1);");

            var vm = new VirtualMachine();

            static Value testFun(Value[] _) => Values.Bool(true);
            vm.DefineNativeFunction("test", 1, testFun);

            Assert.Equal(InterpretResult.RuntimeError, vm.Interpret("print test();"));
            Assert.Equal(InterpretResult.RuntimeError, vm.Interpret("print test(1,2);"));
            Assert.Equal(InterpretResult.Success, vm.Interpret("print test(1);"));
        }
    }
}
