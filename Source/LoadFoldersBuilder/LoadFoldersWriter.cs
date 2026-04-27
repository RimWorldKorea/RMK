using System.Xml.Linq;

namespace LoadFoldersBuilder;

/** 모든 로드 정보를 링크시키는 주 작업대입니다.
 *  여기에 저장된 정보를 사용하여 LoadFolders를 직접적으로 작성합니다.
 *  행이 모드(InferredModTarget)이고, 열이 버전(Version)인 거대한 수납장을 생각하세요.
 *  
 */
public class LoadRack
{
    private readonly Version[] VersionWindow;
    /** Key는 PackageID 조합과 BindingMode로 구성된 해시입니다. */
    private readonly OrderedDictionary<int, InferredModTarget> Targets;

    public bool IsInitialized { get; private set; } = false;
    
    public LoadRack(IEnumerable<Version> LoadVersions)
    {
        VersionWindow = LoadVersions.ToArray() ?? throw new NullReferenceException();
        Targets = new OrderedDictionary<int, InferredModTarget>();
    }
    
    /** BuildRules로부터 실제 로드 구문과 대응되는 InferredModTarget을 작성합니다.
     * 
     */
    public void Initialize(BuildRules InputRules)
    {
        foreach (var Rule in InputRules.Rules.Values)
        {
            string FilePath = Rule.LoadPath + Statics.BuildYamlFileName; // 그냥 로그용
            
            // 여기에서 필요한 패키지ID 조합과 BindingMode를 계산합니다.
            // PackageID 조합은 바인딩 설정에 따라 여러 개 나올 수 있으나, 바인딩 모드는 하나로 결정됩니다.
            List<string[]> PIDCombinations = new List<string[]>();
            BindingMode DeterminedMode = default;
            
            switch (Rule.Dependency)
            {
                case RuleDependency.Independent:
                {
                    DeterminedMode = Rule.Mode;
                    PIDCombinations.Add(Rule.PackageID);
                    break;
                }

                case RuleDependency.Dependent:
                {
                    string[] ParentPackageID = Rule.Parent!.PackageID;
                    BindingMode ParentBindingMode = Rule.Parent.Mode;
                    
                    switch ((int)ParentBindingMode << 2 | (int)Rule.Mode) // 2+2자리 비트플래그
                    {
                        case (int)BindingMode.All << 2 | (int)BindingMode.All:
                        {
                            DeterminedMode = BindingMode.All;
                            PIDCombinations.Add([..ParentPackageID, ..Rule.PackageID]);
                            break;
                        }

                        case (int)BindingMode.Any << 2 | (int)BindingMode.Any:
                        {
                            DeterminedMode = BindingMode.All;
                            foreach (var ParentPID in ParentPackageID)
                                foreach (var ChildPID in Rule.PackageID)
                                    PIDCombinations.Add([ParentPID, ChildPID]);
                            break;
                        }

                        case (int)BindingMode.All << 2 | (int)BindingMode.Any:
                        {
                            DeterminedMode = BindingMode.All;
                            foreach (var ChildPID in Rule.PackageID)
                                PIDCombinations.Add([..ParentPackageID, ChildPID]);
                            break;             
                        }

                        case (int)BindingMode.Any << 2 | (int)BindingMode.All:
                        {
                            DeterminedMode = BindingMode.All;
                            foreach (var ParentPID in ParentPackageID)
                                PIDCombinations.Add([ParentPID, ..Rule.PackageID]);
                            break;             
                        }
                    }
                    break;
                }
            }
            
            /* 대응하는 InferredModTarget이 이미 있는지 검색하고, 없으면 신규 생성합니다.
               그 후 세부 속성을 채워넣습니다. */
            foreach (var Combination in PIDCombinations)
            {
                int BlendedHash = Statics.GetBlendedHash(Combination) ^ DeterminedMode.GetHashCode();
                
                // 기존 타깃이 있는 경우 그걸 가져오고, 아니라면 새로 만들어서 등록합니다.
                if (!Targets.TryGetValue(BlendedHash, out InferredModTarget? Target))
                {
                    Target = new InferredModTarget(VersionWindow, Combination, DeterminedMode);
                    Targets.Add(BlendedHash, Target);
                }
                
                foreach (var Hole in Target.Socket)
                {
                    var Version = Hole.Key;
                    var VersionRule = Hole.Value;

                    // Ban 버전이면 스킵
                    if (Rule.Ban.Contains(Version)) continue;

                    // 이 버전이 Designate 버전인 경우
                    if (Rule.Designate.Contains(Version))
                    {
                        if (VersionRule.IsLocked(out object? RawLockedBy))
                        {
                            BuildRule? LockedBy = RawLockedBy as BuildRule;
                            string HisName = LockedBy!.LoadPath;
                            Console.WriteLine("\n\e[91m{0}의 Designate 설정이 {1}(와)과 충돌합니다.\x1b[0m",
                                FilePath, HisName + Statics.BuildYamlFileName);
                        }
                        else
                        {
                            VersionRule.OverrideAndLock(Rule);
                        }
                        continue;
                    }

                    // 이 버전이 Defalt 버전인 경우
                    if (Version == Rule.Default)
                    {
                        if (VersionRule.IsLocked(out object? RawLockedBy))
                        {
                            BuildRule? LockedBy = RawLockedBy as BuildRule;
                            string HisName = LockedBy!.LoadPath;
                            Console.WriteLine("\n\e[91m{0}의 Default 설정이 {1}(와)과 충돌합니다.\x1b[0m",
                                FilePath, HisName + Statics.BuildYamlFileName);
                        }
                        else
                        {
                            VersionRule.OverrideAndLock(Rule);
                        }
                        continue;
                    }
                    
                    // 이 버전이 Boundary 조건을 만족할 경우
                    switch (Rule.BoundaryCondition)
                    {
                        case BoundaryConditions.None:// 경계 조건이 없으면 가능한 채웁니다.
                            VersionRule.Override(Rule);
                            break;
                        case BoundaryConditions.LeftBounded:
                            if (Version >= Rule.LeftBoundary)
                                VersionRule.Override(Rule);
                            break;
                        case BoundaryConditions.RightBounded:
                            if (Version <= Rule.RightBoundary)
                                VersionRule.Override(Rule);
                            break;
                        case BoundaryConditions.BothBounded:
                            if (Version < Rule.LeftBoundary || Version > Rule.RightBoundary)
                                VersionRule.Override(Rule);
                            break;
                    }
                }
            }
        }
        
        // 로드 정보가 쓰여지지 않은 버전을 제거합니다.
        foreach (var Target in Targets.Values)
        {
            for (int i = Target.Socket.Count - 1; i >= 0; i--)
            { 
                var Hole = Target.Socket.GetAt(i);
                
                if (Hole.Value.LoadPath is null) Target.Socket.RemoveAt(i);
            }
        }
        
        IsInitialized = true;
    }
    
    /** 초기화된 InferredModTarget으로부터 실제 로드 구문을 생성합니다. */
    public XDocument? GenerateXDocument()
    {
        if (!IsInitialized)
        {
            Console.WriteLine("\n\e[93mLoadRack이 Initialize 되지 않은 상태에서 XDocument를 생성하려 합니다.\x1b[0m");
            return null;
        }
        
        var FinalTable = new OrderedDictionary<Version, List<SingleVersionRule>>();
        foreach(var Version in VersionWindow)
            FinalTable.Add(Version, new List<SingleVersionRule>());
        
        // 모든 규칙을 버전별로 분류
        foreach (var Target in Targets.Values)
            foreach (var Hole in Target.Socket)
                FinalTable[Hole.Key].Add(Hole.Value);

        // 이걸 위상 정렬(Topological Sorting)이라고 부른대
        foreach (var Pair in FinalTable)
        {
            var VersionRules =  Pair.Value;
            
            // 정렬을 위한 보조 배열 생성 및 초기화
            var Graph = new OrderedDictionary<SingleVersionRule, List<SingleVersionRule>>();
            var InDegree = new OrderedDictionary<SingleVersionRule, int>();
            
            foreach (var VersionRule in VersionRules)
            {
                Graph[VersionRule] = new List<SingleVersionRule>();
                InDegree[VersionRule] = 0;
            }
            
            // 모든 순서 규칙을 A -> B 형태로 치환하여 그래프 생성
            for (int i = 0; i < VersionRules.Count; i++)
            {
                for (int j = 0; j < VersionRules.Count; j++)
                {
                    if (i == j) continue;

                    var A = VersionRules[i];
                    var B = VersionRules[j];

                    // 모든 순서를 B After A로 치환
                    if ((B.LoadAfter is not null && A.PackageIDs.Overlaps(B.LoadAfter)) ||
                        (A.LoadBefore is not null && B.PackageIDs.Overlaps(A.LoadBefore)))
                    {
                        Graph[A].Add(B);
                        InDegree[B]++;
                    }
                }
            }
            
            // 정렬 시작!
            var SortedList = new List<SingleVersionRule>();
            var Queue = new Queue<SingleVersionRule>();
            
            // 차수가 0인 것 부터 큐에 등록
            foreach (var VersionRule in VersionRules)
            {
                if (InDegree[VersionRule] == 0)
                    Queue.Enqueue(VersionRule);
            }
            
            while (Queue.Count > 0)
            {
                // 큐에서 빼서 출력 리스트에 등록
                var Current = Queue.Dequeue();
                SortedList.Add(Current);

                // 뺀거 뒤에 와야했던 애들 차수 까주고, 0이면 큐에 넣어줌
                foreach (var Dependent in Graph[Current])
                {
                    InDegree[Dependent]--;
                    if (InDegree[Dependent] == 0) Queue.Enqueue(Dependent);
                }
            }// 정렬 완료!

            FinalTable[Pair.Key] = SortedList;
        }
        
        XDocument Document = new XDocument(new XElement("loadFolders"));
        
        foreach (var Pair in FinalTable)
        {
            var SortedList = Pair.Value;

            // <1.0>, <1.1>, ... 등
            XElement VersionNode = new XElement("v" + Pair.Key);
            
            foreach (var VersionRule in SortedList)
                VersionNode.Add(VersionRule.ToXElement());
            
            Document.Root!.Add(VersionNode);
        }

        return Document;
    }
}

/** BuildRule에 적힌 PackageID와 BindingMode의 조합을 Languages 폴더가 가리킬 개별적인 대상으로 간주합니다.
 *  예를 들어, 모드 [A]-Any, [A]-All, [B], [A, B], ... 조합은 모두 다른 타깃입니다.
 *  InferredModTarget의 SingleersionRule은 LoadFolders 구문의 1개 로드 구문과 일대일 대응합니다.
 *  즉, IfModActive(All) ... /> 한 줄을 표현합니다.
 *  BuildRule의 의존성 설정에 따라 하나의 Build.yaml 파일이 여러개의 InferredModTarget 생성을 유발할 수 있습니다.
 */
public class InferredModTarget : IEquatable<InferredModTarget>
{
    /** 해당 타깃을 구성하는 PackageID의 조합입니다. */
    public readonly HashSet<string> PackageIDs;
    
    public readonly BindingMode Mode;
    
    /** 이 모드 타깃이 로드될 때 각각의 버전에서 로드할 폴더 경로를 지정하게 됩니다.
     *  각 요소는 About.xml에 기재된 로드 버전의 오름차순 정렬을 키로 가집니다.
     */
    public readonly OrderedDictionary<Version, SingleVersionRule> Socket;

    /** 저장된 PackageID가 순서에 상관없이 같고, BindingMode또한 같다면 동일한 타깃으로 간주합니다.
     *  HashSet 자체의 해시는 내용물과 무관하기 때문에 사용하면 안됩니다.
     */
    public override int GetHashCode()
    {
        return Statics.GetBlendedHash(PackageIDs) ^ Mode.GetHashCode();
    }

    public bool Equals(InferredModTarget? other)
    {
        if (other is null || this.GetHashCode() != other.GetHashCode())
            return false;
        
        return this.PackageIDs.SetEquals(other.PackageIDs) && this.Mode.Equals(other.Mode);
    }
    
    public InferredModTarget(IEnumerable<Version> LoadVersions, IEnumerable<string> PackageIDSet, BindingMode BindingMode)
    {
        PackageIDs = PackageIDSet.ToHashSet();
        Mode = BindingMode;
        
        // LoadVersions을 오름차순으로 하여 소켓을 생성
        Socket = new OrderedDictionary<Version, SingleVersionRule>();
        foreach(var Version in LoadVersions.Order())
            Socket.Add(Version, new SingleVersionRule(this));
    }
}

/** 잠글 때 까지만 값을 수정할 수 있는 string 컨테이너입니다.
 *  한 번 잠그면 풀 수 없습니다.
 */
public class LockableString
{
    public string? Value
    {
        get;
        set
        {
            if (Lock is Lock.Mutable) field = value;
        }
    } = null;

    /** 한 번 잠그면 이후 수정 시도를 차단합니다. */
    public Lock Lock { get; private set; }
    /** 누가 잠궜나요? */
    public object? IsLockedBy { get; private set; }

    public bool LockThis(object Requester)
    {
        if (Lock is Lock.Immutable) return false;
        
        Lock = Lock.Immutable;
        IsLockedBy = Requester;
        return true;
    }

    public bool IsLocked(out object? LockedBy)
    {
        if (Lock is Lock.Immutable)
        {
            LockedBy = IsLockedBy!;
            return true;
        }
        else
        {
            LockedBy = null;
            return false;
        }
    }
}

/** LoadFolders.xml 구문과 일대일 대응하는 타입입니다.
 * 
 */
public class SingleVersionRule : LockableString
{
    /** 내부 Value에 대한 alias입니다. */
    public string? LoadPath
    {
        get => Value;
        set => Value = value;
    }

    public readonly HashSet<string> PackageIDs;
    public readonly BindingMode Mode;
    public string[]? LoadAfter;
    public string[]? LoadBefore;

    //TODO 이거 동작 구현하가
    /** 이 규칙이 Default 버전에 의해 설정되었는지 여부를 기록합니다.
     *  null이 아닌 경우 해당 버전의 Default 설정에 의해 로드 정보가 설정된 것입니다.
     *  null인 경우 다른 규칙에 의해 로드 정보가 설정된 것입니다.
     */
    public Version? OverridedBy = null;

    public SingleVersionRule(InferredModTarget ModTarget)
    {
        this.PackageIDs = ModTarget.PackageIDs;
        this.Mode = ModTarget.Mode;
    }
    
    public void Override(BuildRule Rule)
    {
        LoadPath = Rule.LoadPath;
        
        LoadAfter = Rule.After;
        LoadBefore = Rule.Before;
    }
    
    public void OverrideAndLock(BuildRule Rule)
    {
        Override(Rule);
        LockThis(Rule);
    }
    
    /** SingleVersionRule을 LoadFolders XML 형식에 사용되는 형태로 변환합니다. */
    public XElement ToXElement()
    {
        string LoadCondition;
        string PathForLoadFolders = LoadPath!.Substring(Statics.RootPath!.Length).Replace("\\","/");
        switch (Mode)
        {
            case BindingMode.All:
                LoadCondition = "IfModActiveAll";
                break;
            case BindingMode.Any:
                LoadCondition = "IfModActive";
                break;
            default:
                Console.WriteLine("문제가 있어");
                throw new Exception();
        }

        XElement Element = new XElement("li",
            new XAttribute(LoadCondition, string.Join(",", PackageIDs)),
            PathForLoadFolders);
            
        return Element;
    }
}

public enum Lock
{
    Mutable = 0, // 이거 또 수정해도 상관없어
    Immutable // 이건 절대 다시 수정하지 마
} 