using System.Collections.Immutable;
using System.Linq;
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
				DiagnosticSeverity.Warning, isEnabledByDefault: true);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "No need")]
		internal static readonly DiagnosticDescriptor TooManyDependenciesRule =
			new("DI0003", "Too many dependencies",
				"The constructor of the type {0} has {1} dependencies which is more than allowed",
				"Analyzers",
				DiagnosticSeverity.Warning, isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
			= ImmutableArray.Create(ConfigurationRule, NoNewOperatorsRule, TooManyDependenciesRule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeObjectCreationNode, SyntaxKind.ObjectCreationExpression);
			context.RegisterSyntaxNodeAction(AnalyzeTypeDeclarationNode, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordDeclaration);
		}

		private void AnalyzeTypeDeclarationNode(SyntaxNodeAnalysisContext context)
		{
			if (_configuration == null)
			{
				var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
				_configuration = new Configuration(context, options);
			}

			foreach (var ctor in context.Node.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
			{
				if (ctor.ParameterList.Parameters.Count > _configuration.AllowedNumberOfDependencies)
				{
					var typeDeclaration = (TypeDeclarationSyntax) context.Node;
					context.ReportDiagnostic(Diagnostic.Create(TooManyDependenciesRule, ctor.ParameterList.GetLocation(), typeDeclaration.Identifier, ctor.ParameterList.Parameters.Count));
				}
			}
		}

		private void AnalyzeObjectCreationNode(SyntaxNodeAnalysisContext context)
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
				var location = context.Node.GetLocation();
				if (!_configuration.Validate(location.SourceTree?.FilePath, ns, type, context.Compilation.AssemblyName))
				{
					context.ReportDiagnostic(Diagnostic.Create(NoNewOperatorsRule, location, ns, type.Name));
				}
			}
		}

		private string GetNamespace(ISymbol symbol)
		{
			var sb = new StringBuilder();
			while (GetPath(symbol, out symbol))
			{
				sb.Insert(0, ".").Insert(0, symbol.Name);
			}

			return sb.Length > 0 ? sb.Remove(sb.Length - 1, 1).ToString() : string.Empty;
		}

		private bool GetPath(ISymbol inputSymbol, out ISymbol symbol)
		{
			if (inputSymbol.ContainingType != null)
			{
				symbol = inputSymbol.ContainingType;
				return true;
			}

			if (!string.IsNullOrEmpty(inputSymbol.ContainingNamespace.Name))
			{
				symbol = inputSymbol.ContainingNamespace;
				return true;
			}

			symbol = null!;
			return false;
		}
	}
}
