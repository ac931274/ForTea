using System;
using System.Linq;
using GammaJul.ReSharper.ForTea.Psi.Directives;
using GammaJul.ReSharper.ForTea.Tree;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.Utils;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel.Model2.Assemblies.Interfaces;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace GammaJul.ReSharper.ForTea.Psi {

	/// <summary>Module referencer that adds an assembly directive to a T4 file.</summary>
	[ModuleReferencer(Priority = -10)]
	public class T4ModuleReferencer : IModuleReferencer {

		[NotNull] private readonly T4Environment _environment;
		[NotNull] private readonly DirectiveInfoManager _directiveInfoManager;

		private bool CanReferenceModule([CanBeNull] IPsiModule module, [CanBeNull] IPsiModule moduleToReference)
			=> module is T4PsiModule t4PsiModule
			&& t4PsiModule.IsValid()
			&& moduleToReference != null
			&& moduleToReference.ContainingProjectModule is IAssembly assembly
			&& _environment.TargetFrameworkId.IsReferenceAllowed(assembly.TargetFrameworkId);

		public bool CanReferenceModule(IPsiModule module, IPsiModule moduleToReference, IPresentableItem presentation)
			=> CanReferenceModule(module, moduleToReference);

		public bool ReferenceModule(IPsiModule module, IPsiModule moduleToReference)
			=> ReferenceModuleImpl(module, moduleToReference, null);

		public bool ReferenceModuleWithType(IPsiModule module, ITypeElement typeToReference)
			=> ReferenceModuleImpl(module, typeToReference.Module, typeToReference.GetContainingNamespace().QualifiedName);

		private bool ReferenceModuleImpl([NotNull] IPsiModule module, [NotNull] IPsiModule moduleToReference, [CanBeNull] string ns) {
			if (!CanReferenceModule(module, moduleToReference))
				return false;

			var t4PsiModule = (T4PsiModule) module;
			var assembly = (IAssembly) moduleToReference.ContainingProjectModule;
			Assertion.AssertNotNull(assembly, "assembly != null");

			if (!(t4PsiModule.SourceFile.GetTheOnlyPsiFile(T4Language.Instance) is IT4File t4File))
				return false;

			Action action = () => {

				// add assembly directive
				t4File.AddDirective(_directiveInfoManager.Assembly.CreateDirective(assembly.FullAssemblyName), _directiveInfoManager);

				// add import directive if necessary
				if (!String.IsNullOrEmpty(ns)
				&& !t4File.GetDirectives(_directiveInfoManager.Import).Any(d => String.Equals(ns, d.GetAttributeValue(_directiveInfoManager.Import.NamespaceAttribute.Name), StringComparison.Ordinal)))
					t4File.AddDirective(_directiveInfoManager.Import.CreateDirective(ns), _directiveInfoManager);

			};

			return ExecuteTransaction(module, action);
		}

		private static bool ExecuteTransaction([NotNull] IPsiModule module, [NotNull] Action action) {
			IPsiTransactions transactions = module.GetPsiServices().Transactions;
			if (transactions.Current != null) {
				action();
				return true;
			}
			return transactions.Execute("T4 Assembly Reference", action).Succeded;
		}

		public T4ModuleReferencer([NotNull] T4Environment environment, [NotNull] DirectiveInfoManager directiveInfoManager) {
			_environment = environment;
			_directiveInfoManager = directiveInfoManager;
		}

	}

}