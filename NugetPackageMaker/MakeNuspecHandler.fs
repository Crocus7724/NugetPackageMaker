namespace NugetPackageMaker

open MonoDevelop.Components.Commands
open MonoDevelop.Ide
open MonoDevelop.Core.Execution
open System.IO
open System.Threading.Tasks
open System.Diagnostics
open System.Linq
open System.Xml
open System.Xml.Linq
open MonoDevelop.Projects

type MakeNuspecHandler() = 
  inherit CommandHandler()
  
  override self.Update(info : CommandInfo) = 
    info.Enabled <- IdeApp.Workbench.ActiveDocument |> function 
                    | null -> false
                    | x -> x.Project <> null
  
  override self.Run() = 
    use progress = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor()
    let project = IdeApp.Workbench.ActiveDocument.Project
    do progress.BeginTask(5)
    //nuspecファイルのパス
    let path = 
      [| project.BaseDirectory.FullPath.ToString()
         project.Name + ".nuspec" |]
      |> Path.Combine
    //nuspecファイルがなければ作成
    if not (File.Exists(path)) then 
      let (output, error) = self.createNuspecFile (project.BaseDirectory.FullPath.ToString())
      do progress.Log.WriteLine(output.ToString())
      do progress.ReportError error
    //ステップ
    do progress.Step()
    //nuspecファイル読み込み
    XElement.Load(path) |> fun e -> 
      self.writeMetadata project (e.Element(XName.Get "metadata"))
      //ステップ!
      progress.Step()
      if not (self.writeFiles project e) then 
        progress.Log.WriteLine(([| "bin"
                                   "Release"
                                   project.Name + ".dll" |]
                                |> Path.Combine)
                               + "が見つかりませんでした。")
      e.Save(path)
    //ステップ!!ステップ!!
    progress.Step()
    self.openNuspecFile path progress
    //ステェェェェェェェッッッッッップ!!!!!!!
    progress.Step()
    //まあStepさせる意味特に無いけど
    progress.EndTask()
  
  member private self.createNuspecFile (path) = 
    new ProcessStartInfo(UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, 
                         WorkingDirectory = path, FileName = "nuget", Arguments = "spec")
    |> Process.Start
    |> fun p -> ( //エラーとOutputを返す
                  p.StandardOutput.ReadToEnd(), p.StandardError.ReadToEnd())
  
  member private self.writeMetadata (project : Project) (element : XElement) = 
    //値書き換え
    do element.SetElementValue(XName.Get "id", project.Name)
    do element.SetElementValue(XName.Get "version", project.Version)
    do element.SetElementValue(XName.Get "title", project.Name)
    do element.SetElementValue(XName.Get "authors", project.AuthorInformation.Name)
    do element.SetElementValue(XName.Get "owners", project.AuthorInformation.Company)
    do element.SetElementValue(XName.Get "description", project.Description)
    do element.SetElementValue(XName.Get "copyright", project.AuthorInformation.Copyright)
    //Projectファイルから
    project.Files
    //packages.configのファイルを探しだし
    |> Seq.tryFind (fun x -> x.ProjectVirtualPath.ToString() = "packages.config")
    |> function 
    //nullなら何もしない
    | None -> ()
    | p -> 
      //dependenciesエレメントがあったら削除
      if (element.Element(XName.Get "dependencies") <> null) then do element.Element(XName.Get "dependencies").Remove()
      XElement
        (XName.Get "dependencies", 
         //packages.configを読み込み、
         let package = XElement.Load(p.Value.FilePath.ToString())
         //packageエレメントを取得し、
         package.Elements(XName.Get "package") 
         //その情報を元に新たに<dependency id="hoge" version="foo"/>を作成し、
         |> Seq.map 
              (fun e -> 
              XElement
                (XName.Get "dependency", XAttribute(XName.Get "id", e.Attribute(XName.Get "id").Value), 
                 XAttribute(XName.Get "version", e.Attribute(XName.Get "version").Value))))
      //metadataに追加
      |> element.Add
  
  member private self.writeFiles (project : Project) (element : XElement) : bool = 
    let path = 
      Path.Combine([| "bin"
                      "Release"
                      project.Name + ".dll" |])
    Debug.WriteLine(Path.Combine([| project.BaseDirectory.ToString()
                                    path |]))
    if File.Exists(Path.Combine([| project.BaseDirectory.ToString()
                                   path |]))
    then 
      //もともとfilesがあったら削除
      if (element.Element(XName.Get("files")) <> null) then do element.Element(XName.Get "files").Remove()
      do element.Add
           (XElement
              (XName.Get "files", 
               XElement(XName.Get "file", XAttribute(XName.Get "src", path), XAttribute(XName.Get "target", "lib"))))
      true
    else false
  
  member private self.openNuspecFile path progress = 
    let project = IdeApp.Workbench.ActiveDocument.Project
    //nuspecをXamarin Studioで開く
    let nuspec = new ProjectFile(path, "None")
    //nuspecファイルがなければプロジェクトに追加(仮
    if not (project.IsFileInProject(path)) then project.AddFile(nuspec)
    //Project.SaveAsyncしないとプロジェクトに追加されないとか聞いてない
    project.SaveAsync(progress) |> ignore
    //タブを開く
    do IdeApp.Workbench.OpenDocument(nuspec.FilePath, project, true) |> ignore