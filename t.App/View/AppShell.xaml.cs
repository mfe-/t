namespace t.App.View;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
	}
	internal ShellContent ShellContent
	{
		get { return _shellContent; }
	}
}