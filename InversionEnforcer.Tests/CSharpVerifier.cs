using System;
using System.Collections.Generic;
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
			return VerifyAnalyzerAsync(source, new Dictionary<string, string>(), expected);
		}

		public static Task VerifyAnalyzerAsync(string source, Dictionary<string, string> options, params DiagnosticResult[] expected)
		{
			var test = new Test(options) { TestCode = source };
			test.ExpectedDiagnostics.AddRange(expected);
			return test.RunAsync();
		}

		public static Task VerifyCodeFixAsync(string source, string fixedSource)
			=> VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

		public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
			=> VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

		public static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
		{
			return VerifyCodeFixAsync(source, new Dictionary<string, string>(), expected, fixedSource);
		}

		public static Task VerifyCodeFixAsync(string source, Dictionary<string, string> options, DiagnosticResult[] expected, string fixedSource)
		{
			// Roslyn fixers always use \r\n for newlines, regardless of OS environment settings, so we normalize
			// the source as it typically comes from multi-line strings with varying newlines.
			if (Environment.NewLine != "\r\n")
			{
				source = source.Replace(Environment.NewLine, "\r\n");
				fixedSource = fixedSource.Replace(Environment.NewLine, "\r\n");
			}

			var test = new Test(new Dictionary<string, string>())
			{
				TestCode = source,
				FixedCode = fixedSource
			};

			test.ExpectedDiagnostics.AddRange(expected);
			return test.RunAsync();
		}

		private class Test : CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, XUnitVerifier>
		{
			private readonly Dictionary<string, string> _options;
			public Test(Dictionary<string, string> options) => _options = options;

			protected override AnalyzerOptions GetAnalyzerOptions(Project project)
			{
				var options = base.GetAnalyzerOptions(project);
				return new AnalyzerOptions(options.AdditionalFiles, new CustomAnalyzerConfigOptionsProvider(_options));
			}
		}

		private class CustomAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
		{
			private readonly Dictionary<string, string> _options;
			public CustomAnalyzerConfigOptionsProvider(Dictionary<string, string> options) => _options = options;

			public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new CustomAnalyzerConfigOptions(_options);

			public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new CustomAnalyzerConfigOptions(new Dictionary<string, string>());

			public override AnalyzerConfigOptions GlobalOptions { get; }
				= new CustomAnalyzerConfigOptions(new Dictionary<string, string>());
		}

		private class CustomAnalyzerConfigOptions : AnalyzerConfigOptions
		{
			private readonly Dictionary<string, string> _options;
			public CustomAnalyzerConfigOptions(Dictionary<string, string> options) => _options = options;
			public override bool TryGetValue(string key, out string value) => _options.TryGetValue(key, out value!);
		}
	}
}
