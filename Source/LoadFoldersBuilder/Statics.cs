// Powered by Rimworld Mod Korean and follows its copyright policy.

using System.Xml.Linq;
using YamlDotNet.Serialization;

namespace LoadFoldersBuilder;

/** 작업에 필요한 여러 기본 정보와 파일을 수집하고, 사용 가능한 형태로 준비합니다. */
static class Statics
{
    //TODO 인스턴스 클래스로 바꿔서 프로그램을 한 번 켜두고 반복 실행하기 쉽게 만들기
    
    public static string? ExePath { get; private set; } // 실행 파일 경로
    public static string? RootPath { get; private set; } // 모드 최상위 루트 경로
    public static string? TargetPath { get; private set; } // 검색 대상 최상위 경로
    
    // 하나라도 null이면 false입니다.
    public static bool IsPathValid => (ExePath, RootPath, TargetPath) switch
    {
        (string a, string b, string c) => true, 
        _ => false
    };

    public const string BuildYamlFileName = "LoadFolders.Build.yaml";
    
    /** About.xml로부터 불러들인 정렬된 버전 목록입니다. */
    public static Version[]? BuildVersions { get; private set; }
    
    static Statics()
    {
        InitializeWithDefaults();
    }

    /** 각종 기본 경로를 설정합니다. */
    public static void InitializeWithDefaults()
    {
        ExePath = AppContext.BaseDirectory;
        RootPath = FindFolderAscending(ExePath, "About");
        TargetPath = RootPath is null ? null : Path.Combine(RootPath, "Data");
        
        Console.WriteLine();
        Console.WriteLine("실행 파일 경로-> {0}", ExePath);
        Console.WriteLine("모드 루트 경로-> {0}", RootPath is null ? "경로 없음" : RootPath);
        Console.WriteLine("색인 타겟 경로-> {0}", TargetPath is null ? "경로 없음" : TargetPath);
    }
    
    /** 지정한 폴더로부터 상위 폴더로 거슬러 올라가며 특정 이름을 가진 폴더가 위치한 경로를 찾습니다. */
    public static string? FindFolderAscending(string StartingPath, string TargetFolderName)
    {
        DirectoryInfo? CurrentFolder = new DirectoryInfo(StartingPath);
        
        while (CurrentFolder is not null)
        {
            DirectoryInfo[] Folders = CurrentFolder.GetDirectories(TargetFolderName, SearchOption.TopDirectoryOnly);
            
            if(Folders.Length is not 0) return CurrentFolder.FullName;

            CurrentFolder = CurrentFolder.Parent;
        }

        return null;
    }

    /** About.xml로부터 버전 정보를 읽습니다. */
    public static bool ReadSupportedVersions(string Location)
    {
        string AboutPath = Path.Combine(Location, "About\\About.xml");
        XDocument About;
        
        if (File.Exists(AboutPath)) About = XDocument.Load(AboutPath);
        else
        {
            Console.WriteLine("\n\e[93mAbout.xml 파일을 찾을 수 없습니다.\x1b[0m");
            BuildVersions = Array.Empty<Version>();
            return false;
        }
        
        BuildVersions = About.Root?.
            Element("supportedVersions")?.
            Elements("li").
            Select(x => new Version(x.Value)).
            OrderBy(x => x).
            ToArray() ?? Array.Empty<Version>();

        if (BuildVersions.Length is not 0)
        {
            Console.WriteLine("\nAbout.xml로부터 버전 정보를 읽었습니다.");
            foreach (Version Version in BuildVersions)
                Console.WriteLine("\t- " + Version);
        }
        else
        {
            Console.WriteLine("\n\e[93mAbout.xml의 버전 정보가 유효하지 않습니다.\x1b[0m");
            return false;
        }
        
        return true;
    }
    
    /** Languages 폴더 및 LoadFolders.Build.yaml 파일을 기준으로 LoadFolders 규칙을 작성할 후보 대상을 선별합니다. */
    public static string[] FindAndValidatePaths(string TargetFolderPath)
    {
        // Languages 및 Textures 폴더가 있는 경로 검색 (이미지 번역만 있는 경우가 있기 때문에 Textures도 포함)
        IEnumerable<string> SearchPattern = new[] { "Languages", "Textures" };
        IEnumerable<string> LanguagesFolders = SearchPattern.SelectMany(
            SearchFolder =>
                Directory.EnumerateDirectories(TargetFolderPath, SearchFolder, SearchOption.AllDirectories)
                ).Distinct(StringComparer.Ordinal);
        
        HashSet<string> FoldersContainsRequiredFolders =
            new HashSet<string>(LanguagesFolders.Select(x => new DirectoryInfo(x).Parent!.ToString()));
        
        // LoadFolders.Build.yaml이 있는 경로 검색
        IEnumerable<string> BuildYamlFiles =
            Directory.EnumerateFiles(TargetFolderPath, BuildYamlFileName, SearchOption.AllDirectories);
        HashSet<string> FoldersWithBuildYaml =
            new HashSet<string>(BuildYamlFiles.Select(x => new DirectoryInfo(x).Parent!.ToString()));
        
        // 차집함 검색
        HashSet<string> HasNoBuildYaml = FoldersContainsRequiredFolders.Except(FoldersWithBuildYaml).ToHashSet();
        HashSet<string> HasNoLanguagesOrTextures = FoldersWithBuildYaml.Except(FoldersContainsRequiredFolders).ToHashSet();

        if (HasNoBuildYaml.Count > 0)
        {
            Console.WriteLine("\n\e[93m다음 {0}개 폴더는 Languages 또는 Textures 폴더가 있지만 LoadFolders.Build.yaml 파일이 없습니다.\n빌드 대상에서 제외됩니다.\x1b[0m", HasNoBuildYaml.Count);
            foreach (var Folder in HasNoBuildYaml)
            {
                Console.WriteLine("\t" + Folder);
            }
        }
        
        if (HasNoLanguagesOrTextures.Count > 0)
        {
            Console.WriteLine("\n\e[93m다음 {0}개 폴더는 LoadFolders.Build.yaml 파일이 있지만 Languages 또는 Textures 폴더가 없습니다.\n하위 경로에 종속 폴더가 의도에 맞게 구성된 경우 무시하세요.\x1b[0m", HasNoLanguagesOrTextures.Count);
            foreach (var Folder in HasNoLanguagesOrTextures)
            {
                Console.WriteLine("\t" + Folder);
            }
        }

        if (FoldersWithBuildYaml.Count > 0)
            Console.WriteLine("\n{0}개의 적합한 빌드 대상 폴더를 찾았습니다.", FoldersWithBuildYaml.Count);
        
        return FoldersWithBuildYaml.Order().ToArray();
    }

    /** 미리 정의된 IDeserializer를 넘겨받아 BuildYamlLocation에 위치한 Build 파일을 역직렬화합니다. */
    public static BuildRule? BuildYamlDeserialize(IDeserializer Deserializer, string BuildYamlLocation)
    {
        string LoadPath = Path.Combine(BuildYamlLocation, BuildYamlFileName);
        string YamlText = File.ReadAllText(LoadPath);
        
        BuildRuleYamlStructure Fish;
        try
        {
            Fish = Deserializer.Deserialize<BuildRuleYamlStructure>(YamlText);
        }
        catch (Exception e)
        {
            string ErrorInfoMessage = String.Empty;
            if (e.InnerException is InvalidCastException)
                ErrorInfoMessage = " Yaml 파일에서 단일 요소 속성에 배열을 입력하거나, 다중 요소 속성에 단일 요소를 입력했을 수 있습니다.";
            
            ErrorInfoMessage += " => " + (e.InnerException?.Message ?? String.Empty);

            Console.WriteLine("\n\e[91m{0}은 유효하지 않습니다.{1}\x1b[0m", LoadPath, ErrorInfoMessage);
            return null;
        }
        
        BuildRule FriedFish = new BuildRule(ref Fish, BuildYamlLocation);

        return FriedFish;
    }
    
    public static int GetBlendedHash<T>(IEnumerable<T> Enumarable)
    {
        int hash = 0; // 초기값이 0이면 ^ 연산에서 첫 원소의 해시가 그대로 나온다
        foreach (T Element in Enumarable)
            hash ^= Element?.GetHashCode() ?? 0;

        return hash;
    }
}