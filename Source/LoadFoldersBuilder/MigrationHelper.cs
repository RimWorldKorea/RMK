// Powered by Rimworld Mod Korean and follows its copyright policy.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;

namespace LoadFoldersBuilder;

/** 구글 시트 데이터를 LoadFolders.Build.yaml로 변환하기 위한 도구입니다. */
public static class MigrationHelper
{
    /** 구글 시트 RMK DB로 부터 생성한 TSV 파일을 읽고 LoadFolders.Build.yaml을 생성합니다.*/
    public static void MigrateFromTSV(string InputPath)
    {
        string TSVFile = File.ReadAllText(InputPath);
        var TSVLines = TSVFile.Split(Environment.NewLine, StringSplitOptions.None);

        var YamlSerializer = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .WithEventEmitter(Next => new FlowSequenceEmitter(Next))
            .WithDefaultScalarStyle(ScalarStyle.DoubleQuoted)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
            .Build();

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

            // 구글 시트용 버전 지정자 규칙 반영 필요함
            // 그런데 그게 필요한 양이 그렇게 많지 않고 한 번만 돌리면 되니 수동으로 하자

            string LoadPathBase = Path.Combine("Data", Cut[0], Cut[1] + " - " + Cut[2]);

            foreach (var Version in Versions)
            {
                string FilePath = LoadPathBase;

                var YamlBox = new BuildRuleYamlStructure();

                YamlBox.Metadata.WorkshopID = Cut[2];
                YamlBox.Metadata.ModName = Cut[1];

                YamlBox.BuildRule.Binding.PackageID = new[] { Cut[4] };
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
    }
}

/** Flow 스타일로 포맷 => [ ... ] */
public class FlowSequenceEmitter : ChainedEventEmitter
{
    public FlowSequenceEmitter(IEventEmitter PostEmitter) : base(PostEmitter) { }
    
    public override void Emit(SequenceStartEventInfo EventInfo, IEmitter Emitter)
    {
        EventInfo.Style = SequenceStyle.Flow;
        base.Emit(EventInfo, Emitter);
    }
}