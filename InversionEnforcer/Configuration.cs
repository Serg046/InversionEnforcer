using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace InversionEnforcer
{
	internal class Configuration
	{
		private readonly string[]? _includedNamespaces;
		private readonly string[]? _excludedNamespaces;
		private readonly string[]? _excludedTypes;

		public Configuration(SyntaxNodeAnalysisContext context, AnalyzerConfigOptions config)
		{
			if (config.TryGetValue("dotnet_diagnostic.DI0001.included_namespaces", out var includedNamespaces))
			{
				_includedNamespaces = includedNamespaces.Split(',');
			}

			if (config.TryGetValue("dotnet_diagnostic.DI0001.excluded_namespaces", out var excludedNamespaces))
			{
				_excludedNamespaces = excludedNamespaces.Split(',');
			}

			if (config.TryGetValue("dotnet_diagnostic.DI0001.excluded_types", out var excludedTypes))
			{
				_excludedTypes = excludedTypes.Split(',');
			}

			if (_includedNamespaces != null && _excludedNamespaces != null)
			{
				context.ReportDiagnostic(Diagnostic.Create(ProhibitNewAnalyzer.ConfigurationRule, Location.None));
			}
		}

		public bool Validate(ISymbol type)
		{
			if (_includedNamespaces != null)
			{
				foreach (var ns in _includedNamespaces)
				{
					if (type.ContainingNamespace.Name.StartsWith(ns, StringComparison.InvariantCultureIgnoreCase))
					{
						return ValidateIncludedType(type);
					}
				}

				return true;
			}
			else if (_excludedNamespaces != null)
			{
				foreach (var ns in _excludedNamespaces)
				{
					if (type.ContainingNamespace.Name.StartsWith(ns, StringComparison.InvariantCultureIgnoreCase))
					{
						return true;
					}
				}
			}

			return ValidateIncludedType(type);
		}

		private bool ValidateIncludedType(ISymbol type)
		{
			if (_excludedTypes != null)
			{
				var typeName = type.ContainingNamespace + "." + type.Name;
				foreach (var excludedType in _excludedTypes)
				{
					if (excludedType.Equals(typeName, StringComparison.InvariantCultureIgnoreCase))
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
