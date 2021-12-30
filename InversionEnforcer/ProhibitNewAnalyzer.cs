using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[assembly: InternalsVisibleTo("InversionEnforcer.Tests")]
namespace InversionEnforcer
{

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ProhibitNewAnalyzer : DiagnosticAnalyzer
	{
		private Configuration? _configuration;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "No need")]
		internal static readonly DiagnosticDescriptor ConfigurationRule =
			new("DI0001", "Configuration error",
				"You should either use included_namespaces or excluded_namespaces",
				"Analyzers",
				DiagnosticSeverity.Error, isEnabledByDefault: true);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "No need")]
		internal static readonly DiagnosticDescriptor NoNewOperatorsRule =
			new ("DI0002", "New operator",
				"Prefer using dependency inversion to new operator for the type {0}.{1}",
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
			if (_configuration == null)
			{
				var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
				_configuration = new Configuration(context, options);
			}

			var node = (ObjectCreationExpressionSyntax) context.Node;
			var type = context.SemanticModel.GetSymbolInfo(node.Type).Symbol;
			if (type != null)
			{
				var ns = GetNamespace(type);
				if (!_configuration.Validate(ns, type.Name, context.Compilation.AssemblyName))
				{
					context.ReportDiagnostic(Diagnostic.Create(NoNewOperatorsRule, context.Node.GetLocation(),
						ns, type.Name));
				}
			}
		}

		private string GetNamespace(ISymbol type)
		{
			var ns = type.ContainingNamespace;
			var sb = new StringBuilder();
			while (!string.IsNullOrEmpty(ns.Name))
			{
				sb.Insert(0, ".").Insert(0, ns.Name);
				ns = ns.ContainingNamespace;
			}

			return sb.Remove(sb.Length - 1, 1).ToString();
		}
	}
}
