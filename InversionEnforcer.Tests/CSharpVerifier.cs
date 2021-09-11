using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace InversionEnforcer.Tests
{
	public class CSharpVerifier<TAnalyzer>
		where TAnalyzer : DiagnosticAnalyzer, new()
	{
		public static DiagnosticResult Diagnostic()
			=> CSharpCodeFixVerifier<TAnalyzer, EmptyCodeFixProvider, XUnitVerifier>.Diagnostic();

		public static DiagnosticResult Diagnostic(string diagnosticId)
			=> CSharpCodeFixVerifier<TAnalyzer, EmptyCodeFixProvider, XUnitVerifier>.Diagnostic(diagnosticId);

		public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
			=> new DiagnosticResult(descriptor);

		public static DiagnosticResult CompilerError(string errorIdentifier)
			=> new DiagnosticResult(errorIdentifier, DiagnosticSeverity.Error);

		public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
		{
			var test = new CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, XUnitVerifier> { TestCode = source };
			test.ExpectedDiagnostics.AddRange(expected);
			return test.RunAsync();
		}

		public static Task VerifyCodeFixAsync(string source, string fixedSource)
			=> VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

		public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
			=> VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

		public static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
		{
			// Roslyn fixers always use \r\n for newlines, regardless of OS environment settings, so we normalize
			// the source as it typically comes from multi-line strings with varying newlines.
			if (Environment.NewLine != "\r\n")
			{
				source = source.Replace(Environment.NewLine, "\r\n");
				fixedSource = fixedSource.Replace(Environment.NewLine, "\r\n");
			}

			var test = new CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, XUnitVerifier>
			{
				TestCode = source,
				FixedCode = fixedSource
			};

			test.ExpectedDiagnostics.AddRange(expected);
			return test.RunAsync();
		}
	}
}
