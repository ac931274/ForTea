using GammaJul.ReSharper.ForTea.Psi.Directives;
using GammaJul.ReSharper.ForTea.Tree;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace GammaJul.ReSharper.ForTea.Psi {

	/// <summary>Cache holding <see cref="T4FileData"/> for each T4 file.</summary>
	[PsiComponent]
	public sealed class T4FileDataCache {

		[NotNull] private readonly WeakToStrongDictionary<IPsiSourceFile, T4FileData> _fileDataBySourceFile = new WeakToStrongDictionary<IPsiSourceFile, T4FileData>();
		[NotNull] private readonly DirectiveInfoManager _directiveInfoManager;

		[NotNull]
		public Signal<Pair<IPsiSourceFile, T4FileDataDiff>> FileDataChanged { get; }

		/// <summary>Called when a PSI file is created.</summary>
		/// <param name="file">The file that was created.</param>
		private void OnPsiFileChanged([CanBeNull] IFile file) {
			if (file is IT4File t4File)
				CreateOrUpdateData(t4File);
		}

		/// <summary>Called when a PSI element changes.</summary>
		/// <param name="treeNode">The tree node that changed.</param>
		/// <param name="psiChangedElementType">The type of the PSI change.</param>
		private void OnPsiChanged(ITreeNode treeNode, PsiChangedElementType psiChangedElementType) {
			if (treeNode != null && psiChangedElementType == PsiChangedElementType.SourceContentsChanged)
				OnPsiFileChanged(treeNode.GetContainingFile());
		}

		private void CreateOrUpdateData([NotNull] IT4File t4File) {
			IPsiSourceFile sourceFile = t4File.GetSourceFile();
			if (sourceFile == null || !sourceFile.LanguageType.Is<T4ProjectFileType>())
				return;

			var newData = new T4FileData(t4File, _directiveInfoManager);
			T4FileData existingData;
			lock (_fileDataBySourceFile) {
				_fileDataBySourceFile.TryGetValue(sourceFile, out existingData);
				_fileDataBySourceFile[sourceFile] = newData;
			}

			T4FileDataDiff diff = newData.DiffWith(existingData);
			if (diff != null)
				FileDataChanged.Fire(Pair.Of(sourceFile, diff));
		}

		/// <summary>Initializes a new instance of the <see cref="T4FileDataCache"/> class.</summary>
		/// <param name="lifetime">The lifetime of this class.</param>
		/// <param name="psiFiles">The PSI manager.</param>
		/// <param name="directiveInfoManager">An instance of <see cref="DirectiveInfoManager"/>.</param>
		public T4FileDataCache(Lifetime lifetime, [NotNull] PsiFiles psiFiles, [NotNull] DirectiveInfoManager directiveInfoManager) {
			_directiveInfoManager = directiveInfoManager;
			FileDataChanged = new Signal<Pair<IPsiSourceFile, T4FileDataDiff>>(lifetime, "T4FileDataCache.FileDataChanged");

			lifetime.Bracket(
				() => psiFiles.PsiFileCreated += OnPsiFileChanged,
				() => psiFiles.PsiFileCreated -= OnPsiFileChanged
			);
			lifetime.Bracket(
				() => psiFiles.AfterPsiChanged += OnPsiChanged,
				() => psiFiles.AfterPsiChanged -= OnPsiChanged
			);
			lifetime.OnTermination(_fileDataBySourceFile);
		}

	}

}