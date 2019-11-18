using System;
using System.Collections.Generic;
using System.Linq;
using GammaJul.ReSharper.ForTea.Psi.Directives;
using GammaJul.ReSharper.ForTea.Tree;
using JetBrains.Annotations;
using JetBrains.Util;

namespace GammaJul.ReSharper.ForTea.Psi {
	
	/// <summary>Contains data for T4 file: which files are included and which assemblies are referenced.</summary>
	/// <remarks>This class is immutable and thus thread safe.</remarks>
	internal sealed class T4FileData {

		[NotNull] private readonly DirectiveInfoManager _directiveInfoManager;
		[NotNull] [ItemNotNull] private readonly JetHashSet<string> _referencedAssemblies = new JetHashSet<string>(StringComparer.OrdinalIgnoreCase);
		[NotNull] [ItemNotNull] private readonly JetHashSet<string> _macros = new JetHashSet<string>(StringComparer.OrdinalIgnoreCase);

		private void HandleDirectives([NotNull] IT4DirectiveOwner directiveOwner) {
			foreach (IT4Directive directive in directiveOwner.GetDirectives()) {
				if (directive.IsSpecificDirective(_directiveInfoManager.Assembly))
					HandleAssemblyDirective(directive);
				else if (directive.IsSpecificDirective(_directiveInfoManager.Include))
					HandleIncludeDirective(directive);
			}
		}

		/// <summary>Handles an assembly directive.</summary>
		/// <param name="directive">The directive containing a potential assembly reference.</param>
		private void HandleAssemblyDirective([NotNull] IT4Directive directive) {
			string assemblyNameOrFile = directive.GetAttributeValue(_directiveInfoManager.Assembly.NameAttribute.Name);
			if (assemblyNameOrFile == null || (assemblyNameOrFile = assemblyNameOrFile.Trim()).Length == 0) {

				// Handle <#@ assembly name="" completion="someassembly" #>, which is a ForTea-specific way
				// to get completion for an implicit assembly (for example, added by a custom directive).
				assemblyNameOrFile = directive.GetAttributeValue("completion");
				if (assemblyNameOrFile == null || (assemblyNameOrFile = assemblyNameOrFile.Trim()).Length == 0)
					return;
			}

			assemblyNameOrFile = Environment.ExpandEnvironmentVariables(assemblyNameOrFile);
			
			VsBuildMacroHelper.GetMacros(assemblyNameOrFile, _macros);
			_referencedAssemblies.Add(assemblyNameOrFile);
		}

		/// <summary>Handles an include directive.</summary>
		/// <param name="directive">The directive containing a potential macro.</param>
		private void HandleIncludeDirective([NotNull] IT4Directive directive)
			=> VsBuildMacroHelper.GetMacros(directive.GetAttributeValue(_directiveInfoManager.Include.FileAttribute.Name), _macros);

		/// <summary>Computes a difference between this data and another one.</summary>
		/// <param name="oldData">The old data.</param>
		/// <returns>
		/// An instance of <see cref="T4FileDataDiff"/> containing the difference between the two data,
		/// or <c>null</c> if there are no differences.
		/// </returns>
		[CanBeNull]
		public T4FileDataDiff DiffWith([CanBeNull] T4FileData oldData) {

			if (oldData == null) {
				if (_referencedAssemblies.Count == 0 && _macros.Count == 0)
					return null;
				return new T4FileDataDiff(_referencedAssemblies, EmptyList<string>.InstanceList, _macros);
			}

			string[] addedMacros = _macros.Except(oldData._macros).ToArray();
			oldData._referencedAssemblies.Compare(_referencedAssemblies, out JetHashSet<string> addedAssemblies, out JetHashSet<string> removedAssemblies);
			
			if (addedMacros.Length == 0 && addedAssemblies.Count == 0 && removedAssemblies.Count == 0)
				return null;

			return new T4FileDataDiff(addedAssemblies, removedAssemblies, addedMacros);
		}

		/// <summary>Initializes a new instance of the <see cref="T4FileData"/> class.</summary>
		/// <param name="t4File">The T4 file that will be scanned for data.</param>
		/// <param name="directiveInfoManager">An instance of <see cref="DirectiveInfoManager"/>.</param>
		public T4FileData([NotNull] IT4File t4File, [NotNull] DirectiveInfoManager directiveInfoManager) {
			_directiveInfoManager = directiveInfoManager;
			
			HandleDirectives(t4File);
			foreach (IT4Include include in t4File.GetRecursiveIncludes())
				HandleDirectives(include);
		}

	}

}