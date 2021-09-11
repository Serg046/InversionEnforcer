using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

[assembly: InternalsVisibleTo("InversionEnforcer.Tests")]
namespace InversionEnforcer
{

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ProhibitNewAnalyzer : DiagnosticAnalyzer
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "No need")]
		internal static readonly DiagnosticDescriptor NoNewOperatorsRule =
			new ("InvEn0001", "New operator",
				"Prefer using dependency inversion to new operator",
				"Analyzers",
				DiagnosticSeverity.Error, isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
			= ImmutableArray.Create(NoNewOperatorsRule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
		}

		private void AnalyzeNode(SyntaxNodeAnalysisContext context)
		{
			context.ReportDiagnostic(Diagnostic.Create(NoNewOperatorsRule, context.Node.GetLocation()));
		}
	}
}
