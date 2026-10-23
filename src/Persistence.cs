using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
namespace Upcourt  {
    public static class Persistence  {
        public static readonly string DefaultDirectoryPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Upcourt");
        public static string DirectoryPath=DefaultDirectoryPath;
        // Compatibility only: locate careers created by the first release.
        public static string LegacyDirectoryPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Upocourt");
        public static string SavePath  {
            get {
                return Path.Combine(DirectoryPath,"career.xml");
            }
        }
        public static string LoadNotice="";
        public static void Validate(SaveData d) {
            if(d==null||d.Version!=1||d.Settings==null||d.CustomPlays==null||d.CustomPlays.Count>40)throw new InvalidDataException("Unsupported save version or invalid playbook.");
            ControlBindings.Ensure(d.Settings);
            if(d.Settings.Keys!=null&&d.Settings.Keys.Length==15){var keys=new Settings().Keys;Array.Copy(d.Settings.Keys,keys,15);d.Settings.Keys=keys;}
            if(d.Settings.Keys==null||d.Settings.Keys.Length!=Settings.Actions.Length)throw new InvalidDataException("Invalid control bindings.");
            if(d.Settings.QuarterSeconds<30||d.Settings.QuarterSeconds>600)throw new InvalidDataException("Invalid period length.");
            League l=d.League;
            if(l!=null) {
                if(l.Clubs.Count!=12||l.UserTeam<0||l.UserTeam>=12||l.Players.Select(p=>p.Id).Distinct().Count()!=l.Players.Count||l.Clubs.Where((c,i)=>c.Id!=i).Any()||l.Clubs.Any(c=>c.Lineup==null||c.Lineup.Length!=5)||l.Players.Any(p=>p.Team < -1||p.Team>11||p.Position<0||p.Position>4)||l.Games.Any(g=>g.Home<0||g.Home>=12||g.Away<0||g.Away>=12||g.Home==g.Away||g.Played&&g.HomeScore==g.AwayScore))throw new InvalidDataException("Invalid league identities or fixtures.");
            }
            if(d.Match!=null&&(d.Match.Version!=1||d.Match.Roster==null||d.Match.Owner>=d.Match.Roster.Count||d.Match.Controlled>=d.Match.Roster.Count||d.Match.UserSide<0||d.Match.UserSide>1))throw new InvalidDataException("Invalid live match.");
        }
        public static void Save(SaveData data) {
            Validate(data);
            Directory.CreateDirectory(DirectoryPath);
            string temp=SavePath+".tmp";
            var xml=new XmlSerializer(typeof(SaveData));
            using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)) {
                xml.Serialize(stream,data);
                stream.Flush(true);
            }
            if(File.Exists(SavePath))File.Replace(temp,SavePath,SavePath+".bak");
            else File.Move(temp,SavePath);
        }
        public static SaveData Load() {
            LoadNotice="";
            var paths=new System.Collections.Generic.List<string>{SavePath,SavePath+".bak"};
            if(DirectoryPath==DefaultDirectoryPath&&!File.Exists(SavePath)&&!File.Exists(SavePath+".bak")) {
                paths.Add(Path.Combine(LegacyDirectoryPath,"career.xml"));
                paths.Add(Path.Combine(LegacyDirectoryPath,"career.xml.bak"));
            }
            foreach(string path in paths) {
                if(!File.Exists(path))continue;
                try {
                    SaveData d;
                    using(var stream=File.OpenRead(path))d=(SaveData)new XmlSerializer(typeof(SaveData)).Deserialize(stream);
                    Validate(d);
                    if(path.EndsWith(".bak"))LoadNotice="Recovered backup save. The original file was unreadable.";
                    if(Path.GetDirectoryName(path)!=DirectoryPath)LoadNotice="Recovered your existing career. Future saves use the Upcourt folder.";
                    return d;
                }
                catch(Exception e) {
                    LoadNotice="Save could not load: "+e.Message;
                }
            }
            return new SaveData();
        }
    }
}
