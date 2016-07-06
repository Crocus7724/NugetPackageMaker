namespace NugetPackageMaker

open MonoDevelop.Components.Commands
open MonoDevelop.Ide
open MonoDevelop.Core.Execution
open System.IO
open System.Threading.Tasks
open System.Diagnostics
open System.Linq
open MonoDevelop.Projects
open MonoDevelop.Components
open MonoDevelop.Components.Extensions

type MakePackageHandler() = 
  inherit CommandHandler()
  
  override self.Update(info : CommandInfo) = 
    info.Enabled <- IdeApp.Workbench |> function 
                    | null -> false
                    | x -> 
                      x.ActiveDocument |> function 
                      | null -> false
                      | x -> 
                        x.Project |> function 
                        | null -> false
                        | x -> 
                          x.IsFileInProject([| x.BaseDirectory.FullPath.ToString()
                                               x.Name + ".nuspec" |]
                                            |> Path.Combine)
  
  override self.Run() = 
    //上に表示するやつ取得
    use progress = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor()
    do progress.BeginTask(5)
    let project = IdeApp.Workbench.ActiveDocument.Project
    
    let nuspec = 
      project.GetProjectFile([| project.BaseDirectory.FullPath.ToString()
                                project.Name + ".nuspec" |]
                             |> Path.Combine)
    //実行前にnuspecファイル保存
    do project.SaveAsync(progress, nuspec.FilePath) |> ignore
    //nuspecからNuget Package作成
    let (output, error) = self.ExecuteNugetCommand
    do progress.Log.WriteLine(output.ToString())
    do progress.ReportError(error)
  
  member self.ExecuteNugetCommand = 
    let dialog = new SelectFileDialog("保存先", FileChooserAction.SelectFolder, SelectMultiple = false)
    if dialog.Run() then 
      let message = self.RunProcess(dialog.SelectedFile.FullPath.ToString())
      message
    else ("", "処理を中断しました。")
  
  member self.RunProcess path = 
    let project = IdeApp.Workbench.ActiveDocument.Project
    new ProcessStartInfo(WorkingDirectory = project.BaseDirectory.FullPath.ToString(), FileName = "nuget", 
                         UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, 
                         Arguments = "pack " + ([| project.BaseDirectory.FullPath.ToString()
                                                   project.Name + ".nuspec" |]
                                                |> Path.Combine) + " -OutputDirectory " + path + " -Verbosity detail")
    |> Process.Start
    |> fun p -> (p.StandardOutput.ReadToEnd(), p.StandardError.ReadToEnd())