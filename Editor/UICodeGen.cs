using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.Callbacks;
using System.IO;

public class UICodeGen : Editor
{
    const string Template = "using UnityEngine.UI;\n\rusing System.Collections.Generic;\nusing UnityEngine;\npublic class {0}:MonoBehaviour{{\n{1}\n}}";
    public static string NewClassName;
    [MenuItem("GameObject/UICodeGen/Panel")]
    static void Gen()
    {
        if (Selection.objects.Length > 0)
        {
            GameObject o = Selection.objects[0] as GameObject;
            Debug.Log(o.name);
            Panel(o);
        }
    }
    [MenuItem("GameObject/UIAutoConnect")]
    static void AutoGen()
    {
        Debug.Log("reload scripts");
        if (Selection.objects.Length > 0)
        {
            GameObject o = Selection.objects[0] as GameObject;
          var udp=  o.GetComponent(NewClassName);
            if (udp == null)
            {
                var t = System.Type.GetType(NewClassName);
                if (t == null)
                    Debug.LogError("tttt");
                o.AddComponent(t);
            }
            //udp.Button_Connect = o.transform.Find("Button_Connect").GetComponent<Button>();
        }
    }

    public struct VariableInfo
    {
        public string TypeName;
        public string Name;

    }
    public static List<VariableInfo> VariableCollection = new List<VariableInfo>();
    static void Panel(GameObject obj)
    {
        Transform t = obj.transform;
        var count = t.childCount;
        VariableCollection.Clear();
        for (int i = 0; i < count; i++)
        {
            var child = t.GetChild(i);
            if (child.GetComponent<Button>() != null)
            {
                if (IsValidGameObjectName(child.name))
                {
                    Debug.Log(child.name);
                    VariableCollection.Add(new VariableInfo() { TypeName = "Button",Name=child.name });
                }
            }
        }
       string code=ToCode(t.name);
        File.WriteAllText(Application.dataPath + "/Scripts/" + t.name + ".cs", code);
        NewClassName = t.name;
        AssetDatabase.Refresh();
    }

    static string ToCode(string className)
    {
        string code = null;
        foreach (var s in VariableCollection)
        {
            code +="public " +s.TypeName + " " + s.Name+";\n\r";
        }
        return string.Format(Template, className, code);
    }
    static bool IsValidGameObjectName(string name)
    {

        if (!char.IsLetter(name[0]))
        {
            return false;
        }
        foreach (char c in name)
        {
            if (c == '_')
                continue;
            if (!char.IsLetterOrDigit(c))
            {
                return false;
            }
        }
        return true;

    }
}
