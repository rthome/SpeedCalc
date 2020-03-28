using SpeedCalc.Core.Runtime;

using Xunit;

namespace SpeedCalc.Tests.Core.Runtime
{
    public class ScriptExecutionTests
    {
        Value RunScriptAndPop(string source)
        {
            var chunk = new Chunk();
            Assert.True(Parser.Compile(source, chunk));

            var vm = new VirtualMachine();
            vm.Interpret(chunk);

            return vm.Pop();
        }

        [Fact]
        public void RunsSingleNumberExpr()
        {
            var result = RunScriptAndPop("1024");
            Assert.Equal(1024M, result.AsNumber());
        }

        [Fact]
        public void RunsSingleBoolExprs()
        {
            var trueResult = RunScriptAndPop("true");
            Assert.True(trueResult.AsBool());

            var falseResult = RunScriptAndPop("false");
            Assert.False(falseResult.AsBool());
        }

        [Fact]
        public void RunsAddExpr()
        {
            var result0 = RunScriptAndPop("10 + 1");
            Assert.Equal(11M, result0.AsNumber());

            var result1 = RunScriptAndPop("0.5 + 1.5");
            Assert.Equal(2M, result1.AsNumber());
        }

        [Fact]
        public void RunsTwoAddExprs()
        {
            var result = RunScriptAndPop("1+2+3");
            Assert.Equal(6M, result.AsNumber());
        }

        [Fact]
        public void RunsMultipleAddExprs()
        {
            var result = RunScriptAndPop("1+2+3+4+5+6+7+8+9+8+7+6+5+4+3+2+1");
            Assert.Equal(81M, result.AsNumber());
        }
    }
}
