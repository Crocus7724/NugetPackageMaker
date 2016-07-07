# NugetPackageMaker

Xamarin StudioでNuget Packageを簡単に作成する拡張機能です。(Mac限定<s>WindowsはVisual Studioがあるから</s>)

## 開発環境(言語)
* Mac OSX 10.11.5
* Xamarin Studio 6.0.1
* MonoDevelop.Addins 0.3.3
* F# 4.0

##　使い方
編集->Make Nuspecファイルを押し、Nuget Packageの情報となる`.nuspec`ファイルを作成します。

![image 1](https://github.com/Crocus7724/NugetPackageMaker/blob/master/%E3%82%B9%E3%82%AF%E3%83%AA%E3%83%BC%E3%83%B3%E3%82%B7%E3%83%A7%E3%83%83%E3%83%88%202016-07-07%2011.26.52.png)

![image 2](https://github.com/Crocus7724/NugetPackageMaker/blob/master/%E3%82%B9%E3%82%AF%E3%83%AA%E3%83%BC%E3%83%B3%E3%82%B7%E3%83%A7%E3%83%83%E3%83%88%202016-07-07%2011.43.35.png)

そうするとMake Packageが有効になるので、Make Packageを押し、保存先のフォルダを選択すると`.nupkg`ファイルが作成され、完了です。
![image 3](https://github.com/Crocus7724/NugetPackageMaker/blob/master/%E3%82%B9%E3%82%AF%E3%83%AA%E3%83%BC%E3%83%B3%E3%82%B7%E3%83%A7%E3%83%83%E3%83%88%202016-07-07%2011.44.18.png)

## 中の動き
`Make Nuspec`を押すと`MakeNuspecHandler.fs`が実行され、`Make Package`を押すと`MakePackageHandler.fs`が実行されます。
.nuspecファイル作成と.nupkgファイル作成は.Net Frameworkの`system.Diagnostics.Process`クラスを使い、ターミナルのコマンドをぶっ叩いています。

`Make Nuspec`では`Process`クラスを使い.nuspecファイルを作成したあと、プロジェクト情報(アセンブリ情報)とpackages.configファイルを使用し、.nuspecを書き換えています。

## 注意点
* プロジェクトを作成したあと、特にアセンブリ情報を編集しないでいると.nuspecファイルの`description`エレメントの中身が空っぽになっています。そのまま実行しても    
<br />Description is required<br />  
となり、.nupkgファイルの作成に失敗します。  
![error](https://github.com/Crocus7724/NugetPackageMaker/blob/master/%E3%82%B9%E3%82%AF%E3%83%AA%E3%83%BC%E3%83%B3%E3%82%B7%E3%83%A7%E3%83%83%E3%83%88%202016-07-07%2012.02.20.png)

なので適当な言葉で埋めておいてください。  


* id,version,title,authors,owners,description,copyright,dependencies,filesエレメントはMake Nuspecをするたびに中身がリセットします。なるべくアセンブリ情報を編集するか、都度書きなおしてください。

## Licence
This software is released under the MIT License.
