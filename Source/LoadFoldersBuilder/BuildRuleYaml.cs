namespace LoadFoldersBuilder;

/** 이 구조체들은 필드의 유효성을 보장하지 않습니다.
 *  단순히 Yaml 역직렬화 데이터를 잠시 받아두는 용도
 *  사용자(BuildRule) 쪽에서 알아서 처리
 */

public struct BuildRuleYamlStructure
{
    public YamlNode_BuildRule BuildRule;
}

public struct YamlNode_BuildRule
{
    public YamlNode_Binding Binding;
    public YamlNode_Order Order;
    public YamlNode_Version Version;
}

public struct YamlNode_Binding
{
    public string[] PackageID;
    public BindingMode Mode;
    public RuleDependency Dependency;
}

public struct YamlNode_Order
{
    public string[] After;
    public string[] Before;
}

public struct YamlNode_Version
{
    public string Default;
    public string LeftBoundary;
    public string RightBoundary;
    public string[] Designate;
    public string[] Ban;
}