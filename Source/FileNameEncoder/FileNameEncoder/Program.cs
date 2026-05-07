using System.Text.RegularExpressions;
using System.Xml.Linq;
using RimworldExtractorInternal;

namespace FileNameEncoder;

// dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded -p:ExcludeAllSymbols=true -o "../../out"

static class Program
{
    static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("파일 이름을 세탁할 폴더 경로를 입력해주세요.");
            if (Console.ReadLine() is not { } UserInput)
            {
                Console.WriteLine("경로가 유효하지 않습니다.");
                continue;
            }
        
            FileNameEncoder(UserInput);
        }
    }
    
    public static void FileNameEncoder(string DirectoryPath)
    {
        DirectoryInfo Directory = new DirectoryInfo(DirectoryPath.Trim().Trim('\"'));
        if (!Directory.Exists)
        {
            Console.WriteLine("경로가 유효하지 않습니다.");
            return;
        }

        string TranslationFolderName;
        Regex FolderNameChecker = new Regex(@"^\d+\.\d+$", RegexOptions.Compiled);
        if (FolderNameChecker.IsMatch(Directory.Name) && Directory.Parent is not null)
            TranslationFolderName = Directory.Parent.Name;
        else
            TranslationFolderName = Directory.Name;

        var DefInjectedDirectory =
            Directory.EnumerateDirectories("DefInjected", SearchOption.AllDirectories).FirstOrDefault();
        var KeyedDirectory =
            Directory.EnumerateDirectories("Keyed", SearchOption.AllDirectories).FirstOrDefault();
        var PatchesDirectory =
            Directory.EnumerateDirectories("Patches", SearchOption.AllDirectories).FirstOrDefault();
        
        var DefInjectedFiles = DefInjectedDirectory?.EnumerateFiles("*", SearchOption.AllDirectories);
        var KeyedFiles = KeyedDirectory?.EnumerateFiles("*", SearchOption.AllDirectories);
        var PatchFiles = PatchesDirectory?.EnumerateFiles("*", SearchOption.AllDirectories);

        if (DefInjectedFiles is not null && DefInjectedFiles.Any())
        {
            Dictionary<string, List<string>> Sort = new Dictionary<string, List<string>>();
            foreach (var File in DefInjectedFiles)
            {
                var ClassName = Path.GetRelativePath(DefInjectedDirectory!.FullName, File.DirectoryName!);
                if (!Sort.TryGetValue(ClassName, out var ClassList))
                    Sort.Add(ClassName, new List<string>());
                
                Sort[ClassName].Add(File.FullName);
            }

            foreach (var XMLFiles in Sort)
            {
                if (XMLFiles.Value.Count > 1)
                {
                    foreach (var XMLFile in XMLFiles.Value)
                    {
                        XDocument XDoc = XDocument.Load(XMLFile);
                        var NodName = XDoc.Root.Elements().FirstOrDefault();
                        var NewFileName = Utils.GenerateFileName(TranslationFolderName, XMLFiles.Key, NodName.Name.LocalName) + ".xml";
                        var NewPath = Path.Combine(DefInjectedDirectory!.FullName, XMLFiles.Key, NewFileName);
                        File.Move(XMLFile, NewPath);
                    }
                    
                }
                else
                {
                    var NewFileName = Utils.GenerateFileName(TranslationFolderName, XMLFiles.Key) + ".xml";
                    var NewPath = Path.Combine(DefInjectedDirectory!.FullName, XMLFiles.Key, NewFileName);
                    File.Move(XMLFiles.Value.First(), NewPath);
                }
                
            }
        }

        if (KeyedFiles is not null && KeyedFiles.Any())
        {
            var NewFileName = Utils.GenerateFileName(TranslationFolderName, "Keyed") + ".xml";
            var NewPath = Path.Combine(KeyedDirectory!.FullName, NewFileName);
            foreach (var XMLFile in KeyedFiles)
            {
                File.Move(XMLFile.FullName, NewPath);
            }
        }

        if (PatchFiles is not null && PatchFiles.Any())
        {
            var NewFileName = Utils.GenerateFileName(TranslationFolderName, "Patches") + ".xml";
            var NewPath = Path.Combine(PatchesDirectory!.FullName, NewFileName);
            foreach (var XMLFile in PatchFiles)
            {
                File.Move(XMLFile.FullName, NewPath);
            }
        }
    }
}