using SpeedCalc.Core.Runtime;
using System;
using System.IO;
using Xunit;

namespace SpeedCalc.Tests.Core.Runtime
{
    public class ScriptExecutionTests
    {
        void CompilesScript(string source)
        {
            var chunk = new Chunk();
            Assert.True(Parser.Compile(source, chunk));
        }

        void CompilerErrors(string source)
        {
            var chunk = new Chunk();
            Assert.False(Parser.Compile(source, chunk));
        }

        void RunScript(string source)
        {
            var chunk = new Chunk();
            Assert.True(Parser.Compile(source, chunk));

            var vm = new VirtualMachine();
            vm.Interpret(chunk);
        }

        string RunScriptAndCaptureOutput(string source)
        {
            var chunk = new Chunk();
            Assert.True(Parser.Compile(source, chunk));


            var vm = new VirtualMachine();
            var writer = new StringWriter();

            vm.SetStdOut(writer);
            vm.Interpret(chunk);

            return writer.ToString().TrimEnd('\r', '\n');
        }

        [Fact]
        public void RunsSingleNumberExpr()
        {
            var result = RunScriptAndCaptureOutput("print 1024;");
            Assert.Equal("1024", result);
        }

        [Fact]
        public void RunsSingleBoolExprs()
        {
            var trueResult = RunScriptAndCaptureOutput("print true;");
            Assert.Equal("true", trueResult);

            var falseResult = RunScriptAndCaptureOutput("print false;");
            Assert.Equal("false", falseResult);
        }

        [Fact]
        public void RunsAddExpr()
        {
            var result0 = RunScriptAndCaptureOutput("print 10 + 1;");
            Assert.Equal("11", result0);

            var result1 = RunScriptAndCaptureOutput("print 0.5 + 1.5;");
            Assert.Equal("2", result1);
        }

        [Fact]
        public void RunsTwoAddExprs()
        {
            var result = RunScriptAndCaptureOutput("print 1+2+3;");
            Assert.Equal("6", result);
        }

        [Fact]
        public void RunsMultipleAddExprs()
        {
            var result = RunScriptAndCaptureOutput("print 1+2+3+4+5+6+7+8+9+8+7+6+5+4+3+2+1;");
            Assert.Equal("81", result);
        }

        [Fact]
        public void RunsGroupedAddAndMulExpr()
        {
            var result = RunScriptAndCaptureOutput("print (2+3)+(2*3);");
            Assert.Equal("11", result);
        }

        [Fact]
        public void RunsSubtractExpr()
        {
            var result = RunScriptAndCaptureOutput("print 7000 - 1;");
            Assert.Equal("6999", result);
        }

        [Fact]
        public void RunsDivisionExpr()
        {
            var result = RunScriptAndCaptureOutput("print 10/2;");
            Assert.Equal("5", result);
        }

        [Fact]
        public void RunsExponentiationExpr()
        {
            var result = RunScriptAndCaptureOutput("print 5**2;");
            Assert.Equal("25", result);
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
            var result = RunScriptAndCaptureOutput("var SomeGlobal = 1; print SomeGlobal;");
            Assert.Equal("1", result);
        }
        
        [Fact]
        public void RunsGlobalAssignmentStmts()
        {
            var result = RunScriptAndCaptureOutput("var A = 1; var B = 2; var A = A + B; print A;");
            Assert.Equal("3", result);
        }

        [Fact]
        public void RunsDeclareLocal()
        {
            RunScript("{ var a = 1; }");
        }

        [Fact]
        public void RunsPrintLocal()
        {
            var result = RunScriptAndCaptureOutput("{ var a = 1; print a; }");
            Assert.Equal("1", result);
        }

        [Fact]
        public void RunsAddLocals()
        {
            var result = RunScriptAndCaptureOutput("{ var a = 1; var b = 2; print a + b; }");
            Assert.Equal("3", result);
        }

        [Fact]
        public void ErrorsOnVariableRedefinitionInSameScope()
        {
            CompilerErrors("{ var a = 1; var a = 100; }");
        }

        [Fact]
        public void RunsShadowedLocalPrint()
        {
            var result = RunScriptAndCaptureOutput("{ var a = 1; { var a = true; print a; } }");
            Assert.Equal("true", result);
        }

        [Fact]
        public void RunsLocalShadowingGlobalPrint()
        {
            var result = RunScriptAndCaptureOutput("var global = 1234; { var global = false; print global; }");
            Assert.Equal("false", result);
        }
    }
}
