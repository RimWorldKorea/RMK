// Powered by Rimworld Mod Korean and follows its copyright policy.

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
    Console.WriteLine("\n\e[32m빌드 시작\x1b[0m");
    TimeSpan TotalRunTime = TimeSpan.Zero;
    
    // 파일 및 폴더 구조 단위에서 유효성을 검사합니다.
    Stopwatch Stopwatch = Stopwatch.StartNew();
    string[] ValidPath = Statics.FindAndValidatePaths(Statics.TargetPath!);
    if (ValidPath.Length is 0)
    {
        Console.WriteLine("\e[93m유효한 경로에 위치한 LoadFolders.Build.yaml 파일을 하나도 찾을 수 없습니다.\x1b[0m");
        StopProgram();
    }
    
    Stopwatch.Stop(); TotalRunTime += Stopwatch.Elapsed;
    Console.WriteLine("\e[32m폴더 구조 및 필수 파일 확인 완료...{0:F3}s\x1b[0m", Stopwatch.Elapsed.TotalSeconds);
    
    // LoadFolders.Build.yaml을 불러들여 BuildRule 타입으로 변환합니다.
    Stopwatch.Restart();
    IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();
    List<BuildRule> BuildQueue = new List<BuildRule>(ValidPath.Length);
    for (int i = 0; i < ValidPath.Length; i++)
    {
        if (Statics.BuildYamlDeserialize(Deserializer, ValidPath[i]) is {} FriedFish && FriedFish.IsValid is true)
            BuildQueue.Add(FriedFish);
    }
    
    Stopwatch.Stop(); TotalRunTime += Stopwatch.Elapsed;
    Console.WriteLine("\e[32mLoadFolders.Build.yaml 읽기 완료...{0:F3}s\x1b[0m", Stopwatch.Elapsed.TotalSeconds);
    
    // BuildRules의 의존 경로 설정을 완성합니다.
    Stopwatch.Restart();
    BuildRules FilteredRules = new BuildRules(BuildQueue);
    FilteredRules.CreateDependencyGraph(Statics.TargetPath!);
    
    Stopwatch.Stop(); TotalRunTime += Stopwatch.Elapsed;
    Console.WriteLine("\e[32m로드 의존성 그래프 생성 완료...{0:F3}s\x1b[0m", Stopwatch.Elapsed.TotalSeconds);
    
    // 로드 구문을 작성하기 위한 최종 형태를 완성합니다.
    Stopwatch.Restart();
    LoadRack MainRack = new LoadRack(Statics.BuildVersions!);
    MainRack.Initialize(FilteredRules);

    Stopwatch.Stop(); TotalRunTime += Stopwatch.Elapsed;
    Console.WriteLine("\e[32m데이터 전처리 완료...{0:F3}s\x1b[0m", Stopwatch.Elapsed.TotalSeconds);

    if (MainRack.GenerateXDocument() is { } CompleteXML)
    {

        // LoadFolders.xml 파일을 작성합니다.
        Stopwatch.Restart();

        try
        {
            CompleteXML.Save(Path.Combine(Statics.RootPath!, "LoadFolders.xml"), SaveOptions.None);
        }
        catch (IOException e) when (e.HResult is unchecked((int)0x800704C8))
        {
            Console.WriteLine("\e[93m다른 프로그램이 LoadFolders.xml의 쓰기 권한을 점유하고 있습니다.\n해당 프로그램을 닫고 다시 시도하세요.\n문제가 해결되지 않는다면 작업 관리자에서 Windows 탐색기를 '다시 시작'해보세요.\x1b[0m");
            StopProgram();
        }
        
        Stopwatch.Stop(); TotalRunTime += Stopwatch.Elapsed;
        Console.WriteLine("\e[32mLoadFolders.xml 작성 완료...{0:F3}s\x1b[0m", Stopwatch.Elapsed.TotalSeconds);
        
        // 참고용으로 쓸 ModList.tsv 파일을 작성합니다.
        Stopwatch.Restart();
        File.WriteAllText(Path.Combine(Statics.RootPath!, "ModList.tsv"), FilteredRules.ExportModList());
        
        Stopwatch.Stop(); TotalRunTime += Stopwatch.Elapsed;
        Console.WriteLine("\e[32mModList.tsv 작성 완료...{0:F3}s\x1b[0m", Stopwatch.Elapsed.TotalSeconds);
        
        Console.WriteLine("\e[32m작업 완료\x1b[0m");
        Console.WriteLine("총 작업시간 {0:F3}s", TotalRunTime.TotalSeconds);
    }
    else
    {
        Console.WriteLine("\e[93mXML 구조를 형성하는 중 문제가 발생했습니다.\x1b[0m");
        StopProgram();
    }
    
    StopProgram();
    StartMigration: // 구글 시트에서 LFB로 이주용
    
    Console.WriteLine("\nLoadFolders.Build.yaml 파일로 변환할 tsv 형식의 파일 경로를 입력하십시오.");
    string TSVPath;
    while (true)
    {
        TSVPath = Console.ReadLine()?.Trim('\"') ?? String.Empty;
        if (Path.Exists(TSVPath)) break;
        
        Console.WriteLine("\n입력한 파일 경로가 유효하지 않습니다.");
        if (TSVPath is "-cancel") StopProgram();
    }
    Console.WriteLine("\n\e[32m데이터 변환 시작\x1b[0m\n");
    
    MigrationHelper.MigrateFromTSV(TSVPath);
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