namespace Shitcord.WebScrapper;

/// <summary>
/// Represents an HTML tag which may include attributes in the form of:
/// <code>&lt;tagname prop1="val1" prop2="val2"&gt; </code>
/// </summary>
public class Tag{
    public string Name{ get; }
    public int StartOffset{ get; internal set; } //offset at which the given tag begins
    public int EndOffset{ get; internal set; } = -1;
    public List<(string, string)> Attributes{ get; }

    public bool ContainsAttribute((string, string) pair){
        if (Attributes.Count == 0){
            return false;
        }
        foreach (var key_value in Attributes){
            if (key_value == pair){
                return true;
            }
        }
        return false;
    }

    public bool Matches(Tag tag){
        if (Name != tag.Name){
            return false;
        }

        if (Attributes.Count != tag.Attributes.Count){
            return false;
        }
        
        for (int i = 0, len = Attributes.Count; i < len; i++){
            (string, string) thisPair = Attributes[i];
            (string, string) given = tag.Attributes[i];
            if (thisPair.Item1 == given.Item1 && thisPair.Item2 == given.Item2){
                continue;
            }
            return false;
        }
        return true;
    }
    public bool Matches(string tag, params (string, string)[] pairs){
        if (Name != tag){
            return false;
        }

        if (Attributes.Count != pairs.Length){
            return false;
        }
        
        for (int i = 0, len = Attributes.Count; i < len; i++){
            //attributes can be provided in a varying order
            if (!ContainsAttribute(pairs[i])){
                return false;
            }
        }
        return true;
    }
    
    public Tag(string name){
        StartOffset = 0;
        Name = name;
        Attributes = new List<(string, string)>();
    }
    public Tag(string name, List<(string, string)> attributes){
        StartOffset = 0;
        Name = name;
        Attributes = attributes;
    }

    public override string ToString(){
        return "Tag{name=" + Name + ", index=" + StartOffset + "}";
    }
}