using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = InversionEnforcer.Tests.CSharpVerifier<InversionEnforcer.ProhibitNewAnalyzer>;

namespace InversionEnforcer.Tests
{
	public class ProhibitNewAnalyzerTests
	{
		[Fact]
		public async Task When_there_is_no_new_operator_Should_succeed()
		{
			var test =
@"class Test
{
	public void Method() { System.Console.WriteLine(); }
}";

			await Verify.VerifyAnalyzerAsync(test);
		}

		[Fact]
		public async Task When_there_is_new_operator_Should_fail()
		{
			var test =
@"class Test
{
	public void Method() { System.Console.WriteLine(new object()); }
}";

			await Verify.VerifyAnalyzerAsync(test, DiagnosticResult
				.CompilerError(ProhibitNewAnalyzer.NoNewOperatorsRule.Id)
				.WithSpan(3, 50, 3, 62));
		}
	}

	class Test
	{
		public void Method() { System.Console.WriteLine(new object()); }
	}
}
