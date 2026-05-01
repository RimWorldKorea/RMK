// Powered by Rimworld Mod Korean and follows its copyright policy.

using System.Text.RegularExpressions;

namespace LoadFoldersBuilder;

/** 폴더 로드 구문을 만드는 1차 재료입니다.
 *  LoadFolders.Build.yaml 파일과 일대일 대응합니다.
 */
public class BuildRule
{
    /** 림월드의 로드 대상이 되는 폴더 경로입니다. 이 룰을 생성한 Build.yaml 파일의 위치이기도 합니다. */
    public readonly string LoadPath;
    
    public readonly string[] PackageID;
    public readonly BindingMode Mode;
    public readonly RuleDependency Dependency;

    /** Dependency가 종속인 경우 가장 가까운 상위 독립 규칙을 의미합니다. */
    public BuildRule? Parent;

    public readonly string[]? After;
    public readonly string[]? Before;
    
    /** [Nullable] 기본 지정 버전입니다. 가능한 경우 Boundary로 확장합니다. */
    public readonly Version? Default;
    /** [Nullable] Default 버전이 존재하는 경우 이 경계까지 확장합니다. */
    public readonly Version? LeftBoundary;
    /** [Nullable] Default 버전이 존재하는 경우 이 경계까지 확장합니다. */
    public readonly Version? RightBoundary;
    /** Default와 Boundary 상태를 저장합니다. */
    public readonly BoundaryConditions BoundaryCondition;
    public readonly Version[]? Designate;
    public readonly Version[]? Ban;

    public readonly string? WorkshopID = null;
    public readonly string? ModName = null;

    // null인 경우 미확인, false인 경우 유효하지 않음, true인 경우 유효 -> 사용 가능
    public bool? IsValid = null;

    /** BuildRuleYamlStructure 객체를 받아서 BuildRule로 정리합니다. */
    public BuildRule(ref BuildRuleYamlStructure Yaml, string YamlPath)
    {
        LoadPath = YamlPath;
        
        PackageID = Yaml.BuildRule.Binding.PackageID;
        
        Mode = Yaml.BuildRule.Binding.Mode;
        if (PackageID.Length is 1) Mode = BindingMode.Any; // 요소가 1개면 의미가 없음. 텍스트나 절약.
        
        Dependency = Yaml.BuildRule.Binding.Dependency;

        After = Yaml.BuildRule.Order.After;
        Before = Yaml.BuildRule.Order.Before;


        string DefaultTemp = Yaml.BuildRule.Version.Default;
        string LeftBoundaryTemp = Yaml.BuildRule.Version.LeftBoundary;
        string RightBoundaryTemp = Yaml.BuildRule.Version.RightBoundary;

        if (!String.IsNullOrEmpty(DefaultTemp)) Default = new Version(DefaultTemp);
        
        if (!String.IsNullOrEmpty(LeftBoundaryTemp)) LeftBoundary = new Version(LeftBoundaryTemp);
        if (!String.IsNullOrEmpty(RightBoundaryTemp)) RightBoundary = new Version(RightBoundaryTemp);
        
        if (LeftBoundary is not null) BoundaryCondition |= BoundaryConditions.LeftBounded;
        if (RightBoundary is not null) BoundaryCondition |= BoundaryConditions.RightBounded;
        
        if (Yaml.BuildRule.Version.Designate is { } DesignateTemp)
            if (DesignateTemp.Length > 0)
                Designate = Array.ConvertAll(DesignateTemp, Version.Parse);

        
        if (Yaml.BuildRule.Version.Ban is { } BanTemp)
            if (BanTemp.Length > 0)
                Ban = Array.ConvertAll(BanTemp, Version.Parse);

        WorkshopID = Yaml.Metadata.WorkshopID;
        ModName = Yaml.Metadata.ModName;
        
        CheckValidity();
    }

    /** 이 빌드 요청 정보가 실제로 의미있는지 확인합니다.
     *  명확하게 상충되는 요청이 있는 경우 무효처리 합니다.
     *  단순히 의도가 명확하지 않거나 불필요한 경우 알림만 띄우는 쪽으로 해결합니다.
     *  Dependency와 같이 다른 객체를 참조해야 정의가 완료되는 속성은 여기서 검증하지 않습니다.
     */
    private void CheckValidity()
    {
        if (PackageID.Length is 0)
        {
            Console.WriteLine("\e[91m{0}의 PackageID 설정이 없습니다.\x1b[0m", LoadPath);
            IsValid = false;
            return;
        }
        
        if ((After is not null && Before is not null) && After.Intersect(Before).Any())
        {
            Console.WriteLine("\e[91m{0}의 After와 Before 속성에 중복된 값이 있습니다.\x1b[0m", LoadPath);
            IsValid = false;
            return;
        }

        if (Default is not null && LeftBoundary is not null && Default < LeftBoundary)
        {
            Console.WriteLine("\e[91m{0}의 Default 버전이 LeftBoundary보다 앞에 있습니다.\x1b[0m", LoadPath);
            IsValid = false;
            return;
        }
        
        if (Default is not null && RightBoundary is not null && Default > RightBoundary)
        {
            Console.WriteLine("\e[91m{0}의 Default 버전이 RightBoundary보다 뒤에 있습니다.\x1b[0m", LoadPath);
            IsValid = false;
            return;
        }

        /* 허용 => 기본 버전을 명확하게 설정할 수 없는 경우가 있기 때문
        if ((LeftBoundary is not null || RightBoundary is not null) && Default is null)
        {
            Console.WriteLine("\n\e[91m{0}에 Boundary 버전이 설정되어 있으나 Default 버전이 없습니다.\x1b[0m", LoadPath);
            IsValid = false;
            return;
        }*/

        /* 허용 => 특정 버전에 종속되지 않는 '기본' 로드 폴더를 설정하고 싶을 수 있기 때문 - 이 경우 로드 우선권이 없게 된다.
        if (Default is null && Designate is null)
        {
            Console.WriteLine("\n\e[91m{0}의 Default 버전과 Designate 버전이 모두 존재하지 않습니다.\x1b[0m", LoadPath);
            IsValid = false;
            return;
        }*/

        if (Designate is not null && Ban is not null && Designate.Intersect(Ban).Any())
        {
            Console.WriteLine("\e[91m{0}의 Designate 버전과 Ban 버전에 중복이 존재합니다.\x1b[0m", LoadPath);
            IsValid = false;
            return;
        }

        if (Default is not null && Ban is not null && Ban.Contains(Default))
        {
            Console.WriteLine("\e[91m{0}의 Ban 버전에 Default 버전이 존재합니다.\x1b[0m", LoadPath);
            IsValid = false;
            return;
        }

        if (Mode is BindingMode.None && PackageID.Length > 1)
        {
            Console.WriteLine("\e[91m{0}의 다중 PackageID 조건에 BindingMode 설정이 존재하지 않습니다.\x1b[0m", LoadPath);
            IsValid = false;
            return;
        }
        
        IsValid = true;
    }
}

/** BuildRule은 다른 BuildRule을 참조해야 의미가 완성되는 속성이 있기 때문에
 *  BuildRules에서 범위를 설정하고 해당 사항을 검증합니다.
 */
public class BuildRules
{
    public readonly OrderedDictionary<string, BuildRule> Rules;

    public BuildRules(IEnumerable<BuildRule> InputRules)
    {
        Rules = new OrderedDictionary<string, BuildRule>();
        foreach (var Rule in InputRules) Rules.Add(Rule.LoadPath, Rule);
    }

    /** BuildRule을 정의하는 Build.yaml 파일의 위치를 통해 검색합니다. */
    public BuildRule? FindWithLocation(string Location)
    {
        if (!Rules.TryGetValue(Location, out BuildRule? YesIAm)) YesIAm = null;
        return YesIAm;
    }
    
    /** 주어진 빌드룰 모음을 참조하여 빌드룰의 의존 설정을 완성합니다. */
    public void CreateDependencyGraph(string TopSearchPath)
    {
        List<string> PendingDelete = new List<string>();
        
        foreach (var Pair in Rules)
        {
            var Rule = Pair.Value;
            
            // Dependent만 확인하면 됨
            if (Rule.Dependency is not RuleDependency.Dependent) continue;
            
            // 이 루프의 목적: Dependent 룰의 상위에 제대로 Independent 룰이 있니?
            DirectoryInfo CurrentFolder = new DirectoryInfo(Rule.LoadPath);
            while(true)
            {
                // 상위 폴더가 null이 아니면 그걸 Current로 지정하고 null이면 실패 루트로
                if (CurrentFolder.Parent is not { } CurrentParent) goto CannotFindParent;
                
                // 이미 최상위 경로까지 왔으면 실패 루트로
                if (CurrentParent.FullName == TopSearchPath) goto CannotFindParent;
                
                // 주어진 Range에서 검색
                if (FindWithLocation(CurrentParent.FullName) is { } RuleCaught &&
                    RuleCaught.Dependency is RuleDependency.Independent)
                {
                    // Independent 모드인 빌드룰이 있다면 해당 룰을 부모룰로 등록
                    Rule.Parent = RuleCaught;
                    break;
                }

                // 못찾았으면 다음 루프로 넘어가
                CurrentFolder = CurrentParent;
                continue;
                
                CannotFindParent:
                Rule.IsValid = false;
                PendingDelete.Add(Pair.Key);
                Console.WriteLine("\n\e[91m{0}(은)는 Dependent로 설정되어 있으나 상위 경로에 Independent 규칙 파일이 없습니다.\x1b[0m",
                    Rule.LoadPath + Statics.BuildYamlFileName);
                break;
            }
        }

        // 무효한 룰을 제거합니다.
        foreach (var Key in PendingDelete) Rules.Remove(Key);
    }

    /** 이 BuildRules의 현재 BuildRule에 기록된 메타데이터로 모드 목록을 작성합니다.
     *  공시용 데이터를 출력하기 위해 사용합니다.
     */
    public string ExportModList()
    {
        var ExportList = new OrderedDictionary<int, string>();
        
        // 문자열이 d.d 형식인지 확인하는 정규식 패턴
        Regex FolderNameChecker = new Regex(@"^\d+\.\d+$", RegexOptions.Compiled);
        
        foreach (var Pair in Rules)
        {
            var Rule = Pair.Value;
            if (Rule.ModName is not { } ModName) continue;

            string RelativeLocation = Path.GetRelativePath(Statics.RootPath!, Pair.Key);
            
            string TopLocation = Path.GetFileName(RelativeLocation);
            int TrimIndex = RelativeLocation.LastIndexOf(TopLocation) - 1;
            if(FolderNameChecker.IsMatch(TopLocation) && TrimIndex is not -1)
               RelativeLocation = RelativeLocation.Remove(TrimIndex);
            
            TopLocation = Path.GetFileName(RelativeLocation);
            TrimIndex = RelativeLocation.LastIndexOf(TopLocation) - 1;
            RelativeLocation = RelativeLocation.Remove(TrimIndex).Replace("\\", "/");
            
            int hash = 0;
            unchecked
            {
                hash = ModName.GetHashCode() * 31 + Rule.WorkshopID?.GetHashCode() ?? ModName.GetHashCode();
            }

            string TextLine = $"{Rule.WorkshopID ?? "No ID"}\t{ModName}\t{RelativeLocation}\t{Rule.PackageID.First()}";
            ExportList.TryAdd(hash, TextLine);
        }

        return string.Join(Environment.NewLine, ExportList.Values);
    }
}

public enum BindingMode
{
    None = 0,
    All = 1,
    Any = 2
}

public enum RuleDependency
{
    None = 0,
    Independent = 1,
    Dependent = 2
}

[Flags]
public enum BoundaryConditions
{
    None = 0,
    LeftBounded = 0b_10,
    RightBounded = 0b_01,
    BothBounded = 0b_11
}