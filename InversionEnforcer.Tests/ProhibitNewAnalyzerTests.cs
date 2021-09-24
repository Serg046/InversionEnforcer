﻿using System.Collections.Generic;
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

		[Fact]
		public async Task When_included_namespace_Should_not_fail()
		{
			var test =
@"class Test
{
	public void Method() { System.Console.WriteLine(new object()); }
}";

			await Verify.VerifyAnalyzerAsync(
				test,
				new Dictionary<string, string> { { "dotnet_diagnostic.DI0001.included_namespaces", "System2"} });
		}

		[Fact]
		public async Task When_excluded_namespace_Should_not_fail()
		{
			var test =
@"class Test
{
	public void Method() { System.Console.WriteLine(new object()); }
}";

			await Verify.VerifyAnalyzerAsync(
				test,
				new Dictionary<string, string> { { "dotnet_diagnostic.DI0001.excluded_namespaces", "System" } });
		}

		[Fact]
		public async Task When_excluded_type_Should_not_fail()
		{
			var test =
@"class Test
{
	public void Method() { System.Console.WriteLine(new object()); }
}";

			await Verify.VerifyAnalyzerAsync(
				test,
				new Dictionary<string, string>
				{
					{ "dotnet_diagnostic.DI0001.included_namespaces", "System" },
					{ "dotnet_diagnostic.DI0001.excluded_types", "System.Object" }
				});
		}

		[Fact]
		public async Task When_excluded_assembly_Should_not_fail()
		{
			var test =
@"class Test
{
	public void Method() { System.Console.WriteLine(new object()); }
}";

			await Verify.VerifyAnalyzerAsync(
				test,
				new Dictionary<string, string>
				{
					{ "dotnet_diagnostic.DI0001.excluded_assemblies", "TestProject" }
				});
		}
	}
}
