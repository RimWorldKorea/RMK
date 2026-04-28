// Powered by Rimworld Mod Korean and follows its copyright policy.

using System.Buffers;
using System.Diagnostics;
using System.Xml.Linq;
using LoadFoldersBuilder;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// [터미널 빌드 커맨드]
// Rider 문제인지 MSBuild 문제인지 버튼으로 하면 Main 진입점을 못찾음
// dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded -p:ExcludeAllSymbols=true -o "../../out"

{
    Console.WriteLine("Powered by Rimworld Mod Korean\n::LoadFoldersBuilder::\n");
    
    if (Statics.IsPathValid is not true)
    {
        Console.WriteLine("\n\e[93m구성 환경이 올바르지 않습니다.\nLoadFoldersBuilder 실행 파일이 LoadFolders를 생성할 올바른 모드 폴더 아래에 있는지 확인하세요.\x1b[0m");
        StopProgram();
    }

    if (Statics.ReadSupportedVersions(Statics.RootPath!) is not true)
        StopProgram();
    
    Console.WriteLine("\n빌드를 시작하려면 '-build' 명령어를 입력하세요.");

    while (true)
    {
        switch (Console.ReadLine())
        {
            case "-build": goto StartBuild;
            case "-migrate": goto StartMigration; // 초기 파일 생성
            default: ClearLastLine(); break;
        }
    }
    
    StartBuild: // 하지마루요
    Stopwatch Timer = Stopwatch.StartNew();
    
    // 파일 및 폴더 구조 단위에서 유효성을 검사합니다.
    string[] ValidPath = Statics.FindAndValidatePaths(Statics.TargetPath!);
    if (ValidPath.Length is 0)
    {
        Console.WriteLine("\n\e[93m유효한 경로에 위치한 LoadFolders.Build.yaml 파일을 하나도 찾을 수 없습니다.\x1b[0m");
        StopProgram();
    }
    
    // LoadFolders.Build.yaml을 불러들여 BuildRule 타입으로 변환합니다.
    IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();
    List<BuildRule> BuildQueue = new List<BuildRule>(ValidPath.Length);
    for (int i = 0; i < ValidPath.Length; i++)
    {
        if (Statics.BuildYamlDeserialize(ref Deserializer, ValidPath[i]) is {} FriedFish)
            BuildQueue.Add(FriedFish);
    }

    // BuildRules의 의존 경로 설정을 완성합니다.
    BuildRules FilteredRules = new BuildRules(BuildQueue);
    FilteredRules.CreateDependencyGraph(Statics.TargetPath!);
    Console.WriteLine("\n규칙 의존성 초기화 완료.");
    
    // 로드 구문을 작성하기 위한 최종 형태를 완성합니다.
    LoadRack MainRack = new LoadRack(Statics.BuildVersions!);
    MainRack.Initialize(FilteredRules);

    if (MainRack.GenerateXDocument() is { } CompleteXML)
    {
        string SavePath = Path.Combine(Statics.RootPath!, "loadFolders.xml");
        CompleteXML.Save(SavePath, SaveOptions.None);
        Console.WriteLine("\nloadFolders.xml 파일의 갱신이 완료되었습니다.");
        
        Timer.Stop();
        Console.WriteLine("\n총 작업시간 {0}초", Timer.Elapsed.TotalSeconds);
    }
    else
    {
        Console.WriteLine("\n\e[93mXML 구조를 형성하는 중 문제가 발생했습니다.\x1b[0m");
        StopProgram();
    }
    
    StopProgram();
    StartMigration: // 구글 시트에서 LFB로 이주용
    
    Console.WriteLine("\nLoadFolders.Build.yaml 파일로 변환할 tsv 형식의 파일 경로를 입력하십시오.");
    var InputPath = Console.ReadLine();
    while (true)
    {
        if (Path.Exists(InputPath)) break;
        Console.WriteLine("\n입력한 파일 경로가 유효하지 않습니다.");
        if (InputPath is "-cancel") StopProgram();
    }
    
    string TSVFile = File.ReadAllText(InputPath);
    var TSVLines = TSVFile.Split(Environment.NewLine, StringSplitOptions.None);
    
    //var PIDAndLoadPath = new Dictionary<string, string>();
    var YamlSerializer = new SerializerBuilder().
        WithNamingConvention(NullNamingConvention.Instance).
        ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve).
        Build();
    
    var YamlBoxes = new Dictionary<string, BuildRuleYamlStructure>();
    foreach (var Line in TSVLines)
    {
        // [0] Directory Designator
        // [1] Mod Name
        // [2] WorkshopID
        // [3] Version Designator
        // [4] PacakgeID
        string[] Cut = Line.Split('\t', StringSplitOptions.None);
        string[] Versions = Cut[3].Split(',');
        
        //TODO 구글 시트용 버전 지정자 규칙 반영 필요함
        // 그런데 그게 필요한 양이 그렇게 많지 않고 한 번만 돌리면 되니 수동으로 하자

        string LoadPathBase = Path.Combine("Data", Cut[0], Cut[1] + " - " + Cut[2]);

        // if (Versions.Length == 1 && Versions.First() == String.Empty)
        foreach (var Version in Versions)
        {
            string FilePath = LoadPathBase;
            
            var YamlBox = new BuildRuleYamlStructure();
            YamlBox.BuildRule.Binding.PackageID = new [] { Cut[4] };
            YamlBox.BuildRule.Binding.Dependency = RuleDependency.Independent;
            if (Version != String.Empty)
            {
                YamlBox.BuildRule.Version.Default = Version;
                FilePath = Path.Combine(FilePath, Version);
            }
            FilePath = Path.Combine(FilePath, Statics.BuildYamlFileName);

            try
            {
                if (Version.AsSpan().ContainsAny("()[]")) throw new Exception();
                File.WriteAllText(FilePath, YamlSerializer.Serialize(YamlBox));
            }
            catch
            {
                Console.WriteLine("\n{0} 생성에 실패했습니다.", FilePath);
            }
        }
    }
    
    Console.WriteLine("\n초기 데이터 변환이 완료되었습니다.");
    StopProgram();
    
    Console.WriteLine("\n\e[93m이 문구를 보았다면 희망을 버려라.\x1b[0m");
    StopProgram();
}

void StopProgram()
{
    Console.WriteLine("\n창을 닫거나 엔터키를 눌러 프로그램을 종료하십시오.");
    Console.ReadLine();
    Environment.Exit(0);
}

void ClearLastLine()
{
    if (Console.CursorTop > 0)
    {
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        Console.Write(new string(' ', Console.BufferWidth)); 
        Console.SetCursorPosition(0, Console.CursorTop);
    }
}