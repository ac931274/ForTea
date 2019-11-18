using GammaJul.ReSharper.ForTea.Psi;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace GammaJul.ReSharper.ForTea.Daemon {

	[DaemonStage(StagesBefore = new[] { typeof(CSharpErrorStage) })]
	public class T4CSharpErrorStage : CSharpDaemonStageBase {

		protected override bool IsSupported(IPsiSourceFile sourceFile)
			=> base.IsSupported(sourceFile) && sourceFile != null && sourceFile.IsLanguageSupported<T4Language>();

		protected override IDaemonStageProcess CreateProcess(
			IDaemonProcess process,
			IContextBoundSettingsStore settings,
			DaemonProcessKind processKind,
			ICSharpFile file
		)
			=> new T4CSharpErrorProcess(process, settings, file);

	}

}