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
		private readonly string[]? _excludedAssemblies;
		private readonly string[]? _excludedFiles;
		private readonly bool _excludePrivateTypes;

		public Configuration(SyntaxNodeAnalysisContext context, AnalyzerConfigOptions config)
		{
			if (config.TryGetValue("dotnet_diagnostic.DI0002.included_namespaces", out var includedNamespaces))
			{
				_includedNamespaces = includedNamespaces.Split(',');
			}

			if (config.TryGetValue("dotnet_diagnostic.DI0002.excluded_namespaces", out var excludedNamespaces))
			{
				_excludedNamespaces = excludedNamespaces.Split(',');
			}

			if (_includedNamespaces != null && _excludedNamespaces != null)
			{
				context.ReportDiagnostic(Diagnostic.Create(ProhibitNewAnalyzer.ConfigurationRule, Location.None));
			}

			if (config.TryGetValue("dotnet_diagnostic.DI0002.excluded_types", out var excludedTypes))
			{
				_excludedTypes = excludedTypes.Split(',');
			}

			if (config.TryGetValue("dotnet_diagnostic.DI0002.excluded_assemblies", out var excludedAssemblies))
			{
				_excludedAssemblies = excludedAssemblies.Split(',');
			}

			if (config.TryGetValue("dotnet_diagnostic.DI0002.excluded_files", out var excludedFiles))
			{
				_excludedFiles = excludedFiles.Split(',');
			}

			if (config.TryGetValue("dotnet_diagnostic.DI0002.exclude_private_types", out var excludePrivateTypes))
			{
				_excludePrivateTypes = bool.TryParse(excludePrivateTypes, out var ignore) && ignore;
			}
		}

		public bool Validate(string? filePath, string @namespace, ISymbol type, string? assemblyName)
		{
			if (filePath != null && _excludedFiles != null)
			{
				foreach (var excludedPath in _excludedFiles)
				{
					if (filePath.Equals(excludedPath, StringComparison.InvariantCultureIgnoreCase))
					{
						return true;
					}
				}
			}

			if (_excludedAssemblies != null)
			{
				foreach (var asm in _excludedAssemblies)
				{
					if (assemblyName == asm)
					{
						return true;
					}
				}
			}

			if (_includedNamespaces != null)
			{
				foreach (var ns in _includedNamespaces)
				{
					if (@namespace.StartsWith(ns, StringComparison.InvariantCultureIgnoreCase))
					{
						return ValidateIncludedType(@namespace, type);
					}
				}

				return true;
			}
			else if (_excludedNamespaces != null)
			{
				foreach (var ns in _excludedNamespaces)
				{
					if (@namespace.StartsWith(ns, StringComparison.InvariantCultureIgnoreCase))
					{
						return true;
					}
				}
			}

			return ValidateIncludedType(@namespace, type);
		}

		private bool ValidateIncludedType(string @namespace, ISymbol type)
		{
			if (_excludePrivateTypes && type.DeclaredAccessibility == Accessibility.Private)
			{
				return true;
			}

			if (_excludedTypes != null)
			{
				var typeName = @namespace + "." + type.Name;
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
